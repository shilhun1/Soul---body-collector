#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// 중간보스2 맵 양 끝에 기존 바닥 타일로 보이는 세로 충돌벽을 만듭니다.
public static class hys_MidBoss2BoundaryWallSetup
{
    private const string ScenePath = "Assets/01Scenes/Soul_Test/hys middle boss2.unity";
    private const string TilemapName = "TEST MAP";
    private const string OldWallRootName = "hys_MidBoss2_FallPreventionWalls";
    private const int LeftX = -35;
    private const int RightX = 35;
    private const int WallBottomY = 0;
    private const int WallTopY = 19;

    [MenuItem("Tools/hys/Middle Boss2/양 끝 세로 타일벽 설치")]
    public static void Apply()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject tilemapObject = GameObject.Find(TilemapName);
        Tilemap tilemap = tilemapObject != null ? tilemapObject.GetComponent<Tilemap>() : null;
        if (tilemap == null)
        {
            Debug.LogError("[hys MidBoss2] TEST MAP 타일맵을 찾지 못했습니다.");
            return;
        }

        // 바닥 끝 타일과 같은 타일을 사용해 벽이 맵과 자연스럽게 이어지게 합니다.
        TileBase wallTile = tilemap.GetTile(new Vector3Int(LeftX, -1, 0));
        if (wallTile == null)
        {
            Debug.LogError("[hys MidBoss2] 벽에 사용할 바닥 타일을 찾지 못했습니다.");
            return;
        }

        for (int y = WallBottomY; y <= WallTopY; y++)
        {
            tilemap.SetTile(new Vector3Int(LeftX, y, 0), wallTile);
            tilemap.SetTile(new Vector3Int(RightX, y, 0), wallTile);
        }

        // 이전에 만들었던 보이지 않는 보조 충돌벽은 중복 충돌을 막기 위해 제거합니다.
        GameObject oldWallRoot = GameObject.Find(OldWallRootName);
        if (oldWallRoot != null)
            Object.DestroyImmediate(oldWallRoot);

        tilemap.CompressBounds();
        EditorUtility.SetDirty(tilemap);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[hys MidBoss2] 맵 양 끝에 높이 20타일의 세로 충돌벽을 설치했습니다.");
    }
}
#endif
