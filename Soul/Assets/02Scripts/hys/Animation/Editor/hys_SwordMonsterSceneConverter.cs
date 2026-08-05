using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

// 잘못 구성된 보스 테스트 오브젝트를 팀원의 일반 Sword 몬스터 프리팹 구조로 교체합니다.
public static class hys_SwordMonsterSceneConverter
{
    private const string ScenePath = "Assets/01Scenes/hys.unity";
    private const string SwordPrefabPath = "Assets/03Prefabs/HWJ/HWJ_Monster_Sword_Test.prefab";
    private const string QueenIdlePath =
        "Assets/Pixel art Chess Knights pack/05_Queen/Red_Queen/R_Queen_idle00.png";
    private const string SwordControllerPath =
        "Assets/05Anims/hys_Enemy_Anims/Sword/hys_Monster_Sword.controller";

    [MenuItem("Tools/HYS/Monster/보스 테스트를 일반 Sword 몬스터로 교체")]
    public static void Convert()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException("hys 씬을 연 상태에서 실행해야 합니다.");
        }

        GameObject oldObject = FindGameObject(scene, "hys_SwordMonster_Test");
        if (oldObject == null)
        {
            throw new InvalidOperationException("교체할 hys_SwordMonster_Test를 찾지 못했습니다.");
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException("Sword 몬스터 프리팹을 찾지 못했습니다: " + SwordPrefabPath);
        }

        Vector3 position = oldObject.transform.position;
        Quaternion rotation = oldObject.transform.rotation;
        Vector3 scale = oldObject.transform.localScale;

        hys_LivingMonsterPossessionTest oldPossession =
            oldObject.GetComponent<hys_LivingMonsterPossessionTest>();
        hys_BodyCollisionProfile oldCollisionProfile =
            oldObject.GetComponent<hys_BodyCollisionProfile>();
        hys_PossessionFeetAnchor oldFeetAnchor =
            oldObject.GetComponent<hys_PossessionFeetAnchor>();

        GameObject replacement = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (replacement == null)
        {
            throw new InvalidOperationException("Sword 몬스터 프리팹 인스턴스 생성에 실패했습니다.");
        }

        PrefabUtility.UnpackPrefabInstance(
            replacement,
            PrefabUnpackMode.Completely,
            InteractionMode.AutomatedAction);
        replacement.name = "hys_SwordMonster_Test";
        replacement.transform.SetPositionAndRotation(position, rotation);
        replacement.transform.localScale = scale;

        ConfigureStandardEnemy(replacement);
        CopyPossessionSupport(
            oldPossession,
            oldCollisionProfile,
            oldFeetAnchor,
            replacement);

        UnityEngine.Object.DestroyImmediate(oldObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[hys Sword Monster] 보스 스크립트를 제거하고 일반 Sword 몬스터 프리팹 구조로 교체했습니다.");
    }

    private static void ConfigureStandardEnemy(GameObject monster)
    {
        HWJ_RootObjectDataResolver resolver = monster.GetComponent<HWJ_RootObjectDataResolver>();
        HWJ_RuntimeStatusSystem status = monster.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_CombatExecutionSystem combatExecution = monster.GetComponent<HWJ_CombatExecutionSystem>();
        HWJ_CombatSystem combat = monster.GetComponent<HWJ_CombatSystem>();
        HWJ_MonsterAISystem monsterAi = monster.GetComponent<HWJ_MonsterAISystem>();
        HWJ_EnemyNavigationSystem navigation = monster.GetComponent<HWJ_EnemyNavigationSystem>();
        HWJ_EnemyAttackSystem attack = monster.GetComponent<HWJ_EnemyAttackSystem>();
        HWJ_CharacterMotionSystem motion = monster.GetComponent<HWJ_CharacterMotionSystem>();
        Rigidbody2D body = monster.GetComponent<Rigidbody2D>();
        SpriteRenderer renderer = monster.GetComponentInChildren<SpriteRenderer>(true);

        if (resolver == null || status == null || combatExecution == null || combat == null
            || monsterAi == null || navigation == null || attack == null || motion == null
            || body == null || renderer == null)
        {
            throw new InvalidOperationException("Sword 일반 몬스터 필수 컴포넌트가 부족합니다.");
        }

        Sprite queenIdle = AssetDatabase.LoadAssetAtPath<Sprite>(QueenIdlePath);
        RuntimeAnimatorController swordController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SwordControllerPath);
        if (queenIdle == null || swordController == null)
        {
            throw new InvalidOperationException("Red Queen Sprite 또는 Sword Controller를 찾지 못했습니다.");
        }

        renderer.sprite = queenIdle;
        renderer.color = Color.white;
        renderer.flipX = false;

        Animator animator = renderer.GetComponent<Animator>();
        if (animator == null)
        {
            animator = renderer.gameObject.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = swordController;
        animator.applyRootMotion = false;

        SetReference(status, "dataResolver", resolver);
        SetReference(status, "motionSystem", motion);
        SetReference(combatExecution, "dataResolver", resolver);
        SetReference(combatExecution, "runtimeStatus", status);
        SetReference(combatExecution, "combatSystem", combat);
        SetReference(combatExecution, "motionSystem", motion);
        SetReference(combat, "dataResolver", resolver);
        SetReference(combat, "runtimeStatus", status);
        SetReference(monsterAi, "dataResolver", resolver);
        SetReference(monsterAi, "runtimeStatus", status);
        SetReference(monsterAi, "enemyAttackSystem", attack);
        SetReference(monsterAi, "motionSystem", motion);
        SetReference(monsterAi, "body", body);
        SetReference(navigation, "dataResolver", resolver);
        SetReference(navigation, "runtimeStatus", status);
        SetReference(navigation, "motionSystem", motion);
        SetReference(navigation, "monsterAI", monsterAi);
        SetReference(navigation, "body", body);
        SetReference(attack, "dataResolver", resolver);
        SetReference(attack, "runtimeStatus", status);
        SetReference(attack, "combatExecutionSystem", combatExecution);
        SetReference(attack, "monsterAI", monsterAi);
        SetReference(motion, "animator", animator);
        SetReference(motion, "spriteRenderer", renderer);
        SetReference(motion, "body", body);
        SetReference(motion, "dataResolver", resolver);
        SetReference(motion, "runtimeStatus", status);

        hys_HWJEnemyPatternAnimatorBridge patternBridge =
            monster.GetComponent<hys_HWJEnemyPatternAnimatorBridge>();
        if (patternBridge == null)
        {
            patternBridge = monster.AddComponent<hys_HWJEnemyPatternAnimatorBridge>();
        }

        patternBridge.Initialize(attack, animator);

        hys_MonsterAnimatorLifecycle lifecycle =
            monster.GetComponent<hys_MonsterAnimatorLifecycle>();
        if (lifecycle == null)
        {
            lifecycle = monster.AddComponent<hys_MonsterAnimatorLifecycle>();
        }

        lifecycle.Initialize(animator, status);
    }

    private static void CopyPossessionSupport(
        hys_LivingMonsterPossessionTest oldPossession,
        hys_BodyCollisionProfile oldCollisionProfile,
        hys_PossessionFeetAnchor oldFeetAnchor,
        GameObject replacement)
    {
        hys_BodyCollisionProfile newCollisionProfile =
            CopyComponent(oldCollisionProfile, replacement) as hys_BodyCollisionProfile;
        hys_PossessionFeetAnchor newFeetAnchor =
            CopyComponent(oldFeetAnchor, replacement) as hys_PossessionFeetAnchor;
        hys_LivingMonsterPossessionTest newPossession =
            CopyComponent(oldPossession, replacement) as hys_LivingMonsterPossessionTest;

        if (newPossession == null)
        {
            return;
        }

        SetReference(newPossession, "targetResolver", replacement.GetComponent<HWJ_RootObjectDataResolver>());
        SetReference(newPossession, "targetStatus", replacement.GetComponent<HWJ_RuntimeStatusSystem>());
        SetReference(newPossession, "collisionProfile", newCollisionProfile);
        SetReference(newPossession, "feetAnchor", newFeetAnchor);
        SetReference(newPossession, "targetBodyCollider", replacement.GetComponent<Collider2D>());
    }

    private static Component CopyComponent(Component source, GameObject target)
    {
        if (source == null)
        {
            return null;
        }

        ComponentUtility.CopyComponent(source);
        if (!ComponentUtility.PasteComponentAsNew(target))
        {
            throw new InvalidOperationException(source.GetType().Name + " 복사에 실패했습니다.");
        }

        Component[] components = target.GetComponents(source.GetType());
        return components[components.Length - 1];
    }

    private static void SetReference(Component component, string propertyName, UnityEngine.Object value)
    {
        if (component == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(component);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(component);
    }

    private static GameObject FindGameObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    return child.gameObject;
                }
            }
        }

        return null;
    }
}
