using UnityEditor;
using UnityEngine;

// 직접 제작한 Sword 몬스터 PNG와 Clip을 다시 Import할 때 사용하는 안전한 메뉴입니다.
// 이전처럼 공격·Idle 원본을 변형해 땜빵 Sprite를 자동 생성하지 않습니다.
public static class hys_SwordMonsterMissingMotionBuilder
{
    private const string SwordRoot = "Assets/05Anims/hys_Enemy_Anims/Sword";

    [MenuItem("Tools/HYS/Animation/Sword 몬스터 직접 도트 다시 불러오기")]
    public static void Generate()
    {
        AssetDatabase.ImportAsset(SwordRoot, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[hys Sword Monster] 직접 제작한 도트 PNG와 Clip을 다시 불러왔습니다.");
    }

    // 배치 모드에서도 새 그림을 만들지 않고 현재 직접 도트 Asset만 다시 Import합니다.
    public static void GenerateFromCommandLine()
    {
        Generate();
    }
}
