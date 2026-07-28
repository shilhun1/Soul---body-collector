#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 그래픽이 없는 최종보스 1페이즈 테스트 오브젝트를 메뉴에서 생성합니다.
/// </summary>
public static class hys_FinalBossPrototypeCreator
{
    private const string DataRoot = "Assets/02Scripts/hys/Boss/Data/";
    private static readonly string[] SummonPrefabGuids =
    {
        "d5bf07532af8bbc45a0f20ef875ee17a",
        "b5b460fd804c27741882ca7cb73f719c",
        "f3be394d9fe9a654c972f7282987f973",
        "63bca773b812f4e4091ce7ef09775955",
        "a59df2ced68ffef41899d729c2e94a7d"
    };

    [MenuItem("GameObject/hys/최종보스 1페이즈 테스트 오브젝트 생성", false, 20)]
    private static void CreatePrototype()
    {
        GameObject boss = new GameObject("hys_FinalBoss_Phase1_Prototype");
        Undo.RegisterCreatedObjectUndo(boss, "최종보스 1페이즈 생성");
        if (Selection.activeTransform != null)
            boss.transform.position = Selection.activeTransform.position;

        SpriteRenderer renderer = Undo.AddComponent<SpriteRenderer>(boss);
        // 전용 그래픽이 오기 전까지 기존 기사 이미지를 테스트용으로 표시합니다.
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            AssetDatabase.GUIDToAssetPath("c0314a73b96ee274ca67767bc0ae1a32"));
        Rigidbody2D body = Undo.AddComponent<Rigidbody2D>(boss);
        body.freezeRotation = true;
        BoxCollider2D collider = Undo.AddComponent<BoxCollider2D>(boss);
        collider.size = new Vector2(2.2f, 4.2f);

        HWJ_RootObjectDataResolver resolver = Undo.AddComponent<HWJ_RootObjectDataResolver>(boss);
        HWJ_RuntimeStatusSystem status = Undo.AddComponent<HWJ_RuntimeStatusSystem>(boss);
        HWJ_CombatSystem combat = Undo.AddComponent<HWJ_CombatSystem>(boss);
        hys_FinalBossShield shield = Undo.AddComponent<hys_FinalBossShield>(boss);
        hys_FinalBossPattern pattern = Undo.AddComponent<hys_FinalBossPattern>(boss);
        hys_FinalBossLogic logic = Undo.AddComponent<hys_FinalBossLogic>(boss);

        SerializedObject resolverSerialized = new SerializedObject(resolver);
        resolverSerialized.FindProperty("rootObjectData").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(DataRoot + "hys_FinalBoss_RootObjectData.asset");
        resolverSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject patternSerialized = new SerializedObject(pattern);
        SerializedProperty patternArray = patternSerialized.FindProperty("patternData");
        patternArray.arraySize = 10;
        for (int i = 0; i < 10; i++)
        {
            patternArray.GetArrayElementAtIndex(i).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<HWJ_BossPatternDataSO>(
                    DataRoot + $"hys_FinalBoss_Pattern{i + 1}.asset");
        }

        SerializedProperty summonArray = patternSerialized.FindProperty("summonMonsterPrefabs");
        summonArray.arraySize = SummonPrefabGuids.Length;
        for (int i = 0; i < SummonPrefabGuids.Length; i++)
        {
            summonArray.GetArrayElementAtIndex(i).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    AssetDatabase.GUIDToAssetPath(SummonPrefabGuids[i]));
        }
        patternSerialized.ApplyModifiedPropertiesWithoutUndo();

        // 나머지 참조는 각 hys 컴포넌트가 Awake에서 같은 오브젝트를 기준으로 자동 연결합니다.
        Selection.activeGameObject = boss;
        EditorGUIUtility.PingObject(boss);
        EditorUtility.SetDirty(boss);
    }
}
#endif
