#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 플레이어 5종의 빙의 사망 연출에 필요한 Die/Soul 클립과 Runtime 구성을 검증합니다.
/// </summary>
public static class hys_PossessionDeathFlowValidator
{
    private const string LibraryPath = "Assets/Resources/hys_PossessionAnimationLibrary.asset";
    private const string RuntimePlayerPath =
        "Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady/HWJ_Runtime_Player_Soul.prefab";

    private readonly struct WeaponSpec
    {
        public WeaponSpec(string weapon, string controllerProperty, string soulProperty)
        {
            Weapon = weapon;
            ControllerProperty = controllerProperty;
            SoulProperty = soulProperty;
        }

        public string Weapon { get; }
        public string ControllerProperty { get; }
        public string SoulProperty { get; }
    }

    private static readonly WeaponSpec[] Weapons =
    {
        new WeaponSpec("Sword", "swordPlayerController", "swordSoulExitClip"),
        new WeaponSpec("Axe", "axePlayerController", "axeSoulExitClip"),
        new WeaponSpec("Bow", "bowPlayerController", "bowSoulExitClip"),
        new WeaponSpec("Lance", "lancePlayerController", "lanceSoulExitClip"),
        new WeaponSpec("Shield", "shieldPlayerController", "shieldSoulExitClip")
    };

    [MenuItem("Tools/hys/Animation/빙의 사망 연출 검증")]
    public static void Validate()
    {
        List<string> errors = CollectErrors();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException("[hys DeathFlow] " + string.Join(" | ", errors));
        }

        Debug.Log("[hys DeathFlow] VALIDATION_OK weapons=5, runtimePlayer=OK");
    }

    public static void ValidateFromCommandLine()
    {
        Validate();
    }

    private static List<string> CollectErrors()
    {
        List<string> errors = new List<string>();
        hys_PossessionAnimationLibrary library =
            AssetDatabase.LoadAssetAtPath<hys_PossessionAnimationLibrary>(LibraryPath);
        if (library == null)
        {
            errors.Add("빙의 애니메이션 라이브러리가 없습니다.");
            return errors;
        }

        SerializedObject serializedLibrary = new SerializedObject(library);
        foreach (WeaponSpec weapon in Weapons)
        {
            AnimatorController controller = ReadObject<AnimatorController>(
                serializedLibrary,
                weapon.ControllerProperty);
            AnimationClip soulClip = ReadObject<AnimationClip>(serializedLibrary, weapon.SoulProperty);

            ValidateClip(soulClip, weapon.Weapon + " Soul", errors);
            ValidateState(controller, $"hys_{weapon.Weapon}_Die", errors);
            ValidateState(controller, $"hys_{weapon.Weapon}_Soul", errors);
        }

        AnimationClip swordSoul = ReadObject<AnimationClip>(serializedLibrary, "swordSoulExitClip");
        if (swordSoul != null)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(swordSoul);
            int spriteFrames = bindings.Length > 0
                ? AnimationUtility.GetObjectReferenceCurve(swordSoul, bindings[0]).Length
                : 0;
            if (spriteFrames != 5)
            {
                errors.Add($"Sword Soul 프레임 수가 5가 아닙니다: {spriteFrames}");
            }
        }

        ValidateRuntimePlayer(errors);
        return errors;
    }

    private static T ReadObject<T>(SerializedObject serializedObject, string propertyName)
        where T : UnityEngine.Object
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        return property != null ? property.objectReferenceValue as T : null;
    }

    private static void ValidateClip(AnimationClip clip, string label, List<string> errors)
    {
        if (clip == null)
        {
            errors.Add(label + " 클립이 없습니다.");
        }
        else if (clip.empty || clip.length <= 0f)
        {
            errors.Add(label + " 클립이 비어 있습니다.");
        }
    }

    private static void ValidateState(
        AnimatorController controller,
        string stateName,
        List<string> errors)
    {
        if (controller == null)
        {
            errors.Add(stateName + " 컨트롤러가 없습니다.");
            return;
        }

        foreach (ChildAnimatorState state in controller.layers[0].stateMachine.states)
        {
            if (state.state != null && state.state.name == stateName && state.state.motion != null)
            {
                return;
            }
        }

        errors.Add(controller.name + "에 유효한 " + stateName + " 상태가 없습니다.");
    }

    private static void ValidateRuntimePlayer(List<string> errors)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RuntimePlayerPath);
        if (prefab == null)
        {
            errors.Add("Runtime 플레이어 프리팹이 없습니다.");
            return;
        }

        bool hasSerializedBridge = prefab.GetComponent<hys_HWJPossessionAnimationBridge>() != null;
        bool canReceiveRuntimeBridge = prefab.GetComponent<HWJ_PlayerMovementSystem>() != null
            && prefab.GetComponent<HWJ_CharacterMotionSystem>() != null
            && prefab.GetComponent<HWJ_SoulSystem>() != null;
        // RuntimeReady 플레이어는 Bootstrap이 위 세 구성요소를 보고 브리지를 자동 설치합니다.
        if (!hasSerializedBridge && !canReceiveRuntimeBridge)
        {
            errors.Add("Runtime 플레이어가 빙의 애니메이션 브리지를 받을 조건을 충족하지 않습니다.");
        }
        if (prefab.GetComponent<hys_Player_Animator>() == null)
        {
            errors.Add("Runtime 플레이어에 Player Animator 브리지가 없습니다.");
        }
        if (prefab.GetComponentInChildren<hys_Ghost_Animator>(true) == null)
        {
            errors.Add("Runtime 플레이어에 Ghost Animator 브리지가 없습니다.");
        }
    }
}
#endif
