#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Soul_Build 1_boss 경기장을 큰 그리드 기준 가로 4칸, 세로 1칸 줄여 다시 구성합니다.
public static class hys_1BossArenaResizeSetup
{
    private const string ScenePath = "Assets/01Scenes/Soul_Build/1_boss.unity";
    private const string BackgroundName = "hys_1Boss_BarracksBackground";

    private static readonly Vector3 BackgroundPosition = new Vector3(43.5f, -16.5f, 0f);
    private static readonly Vector3 BackgroundScale = new Vector3(3.0812325f, 3.0812325f, 1f);

    [InitializeOnLoadMethod]
    private static void ScheduleRequestedResize()
    {
        EditorApplication.delayCall += TryApplyRequestedResize;
    }

    [MenuItem("Tools/hys/Soul Build/1_boss 경기장 4x1칸 축소")]
    public static void ApplyFromMenu()
    {
        // 메뉴를 실수로 다시 눌러도 이미 축소된 타일을 중복 가공하지 않습니다.
        ApplyResize(false);
    }

    private static void TryApplyRequestedResize()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += TryApplyRequestedResize;
            return;
        }

        ApplyResize(false);
    }

    private static void ApplyResize(bool force)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject mapRoot = GameObject.Find("map");
        GameObject background = GameObject.Find(BackgroundName);
        if (mapRoot == null || background == null)
        {
            Debug.LogError("[hys 1_boss] 맵 또는 배경 오브젝트를 찾지 못했습니다.");
            return;
        }

        Tilemap baseTilemap = FindTilemap(mapRoot, "base");
        Tilemap platformTilemap = FindTilemap(mapRoot, "platform");
        if (baseTilemap == null || platformTilemap == null)
        {
            Debug.LogError("[hys 1_boss] base 또는 platform Tilemap을 찾지 못했습니다.");
            return;
        }

        // 이미 축소된 씬은 도메인 재로드 때 다시 가공하지 않습니다.
        if (!force
            && baseTilemap.cellBounds.xMin == 4
            && baseTilemap.cellBounds.xMax == 85
            && baseTilemap.cellBounds.yMin == -34
            && baseTilemap.cellBounds.yMax == 3
            && Vector3.Distance(background.transform.position, BackgroundPosition) < 0.01f)
        {
            return;
        }

        ResizeBoundary(baseTilemap);
        TrimOuterPlatforms(platformTilemap);

        background.transform.position = BackgroundPosition;
        background.transform.localScale = BackgroundScale;

        EditorUtility.SetDirty(baseTilemap);
        EditorUtility.SetDirty(platformTilemap);
        EditorUtility.SetDirty(background.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[hys 1_boss] 경기장을 가로 4칸·세로 1칸 줄이고 배경을 62x22 내부에 맞췄습니다.");
    }

    private static Tilemap FindTilemap(GameObject mapRoot, string objectName)
    {
        Tilemap[] tilemaps = mapRoot.GetComponentsInChildren<Tilemap>(true);
        for (int i = 0; i < tilemaps.Length; i++)
            if (tilemaps[i] != null && tilemaps[i].name == objectName) return tilemaps[i];
        return null;
    }

    private static void ResizeBoundary(Tilemap tilemap)
    {
        Dictionary<Vector3Int, TileBase> resizedTiles = new Dictionary<Vector3Int, TileBase>();
        BoundsInt bounds = tilemap.cellBounds;
        foreach (Vector3Int position in bounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(position);
            if (tile == null || !TryMapBoundaryPosition(position, out Vector3Int resizedPosition)) continue;
            resizedTiles[resizedPosition] = tile;
        }

        tilemap.ClearAllTiles();
        foreach (KeyValuePair<Vector3Int, TileBase> pair in resizedTiles)
            tilemap.SetTile(pair.Key, pair.Value);
        tilemap.CompressBounds();
    }

    private static bool TryMapBoundaryPosition(Vector3Int source, out Vector3Int destination)
    {
        destination = source;

        // 바닥은 유지하되 가로 양쪽을 큰 그리드 두 칸씩 잘라냅니다.
        if (source.y >= -34 && source.y <= -28)
        {
            int maximumX = source.y <= -29 ? 84 : 83;
            return source.x >= 4 && source.x <= maximumX;
        }

        // 기존 좌우 벽을 각각 안쪽으로 20유닛 이동하고 새 천장 높이까지만 남깁니다.
        if (source.y >= -27 && source.y <= -6)
        {
            if (source.x >= -16 && source.x <= -8)
            {
                destination.x = source.x + 20;
                return true;
            }

            if (source.x >= 95 && source.x <= 103)
            {
                destination.x = source.x - 20;
                return true;
            }
        }

        // 천장 전체를 큰 그리드 한 칸 아래로 내리고 새 가로 폭으로 자릅니다.
        if (source.y >= 5 && source.y <= 12 && source.x >= 4 && source.x <= 83)
        {
            destination.y = source.y - 10;
            return true;
        }

        return false;
    }

    private static void TrimOuterPlatforms(Tilemap tilemap)
    {
        List<Vector3Int> removedPositions = new List<Vector3Int>();
        foreach (Vector3Int position in tilemap.cellBounds.allPositionsWithin)
            if (tilemap.HasTile(position) && (position.x < 13 || position.x > 74))
                removedPositions.Add(position);

        for (int i = 0; i < removedPositions.Count; i++)
            tilemap.SetTile(removedPositions[i], null);
        tilemap.CompressBounds();
    }
}
#endif
