using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using System.IO;

// HYS 중간보스2 테스트 씬에 전용 배경, 충돌 타일맵과 플레이어 원형 콜라이더를 구성합니다.
public static class hys_MidBoss2ArenaSetup
{
    private const string ScenePath = "Assets/01Scenes/Soul_Test/hys middle boss2.unity";
    private const string BackgroundPath =
        "Assets/04Image/hys/MidBoss2Arena/hys_MidBoss2_RoyalRuins_Background.png";
    private const string FloorTexturePath =
        "Assets/04Image/hys/MidBoss2Arena/hys_MidBoss2_RoyalRuins_FloorTile.png";
    private const string TileFolder = "Assets/02Scripts/hys/Boss/Environment/Tiles";
    private const string FloorTileAssetPath = TileFolder + "/hys_MidBoss2_RoyalRuins_FloorTile.asset";
    private const string EnvironmentRootName = "hys_MidBoss2_ArenaEnvironment";
    private const string OldGroundName = "hys_MidBoss2_TestGround";
    private const string TilemapName = "TEST MAP";
    private const string PlayerName = "HWJ_Player";
    private const float BossEntryRange = 10f;

    [MenuItem("Tools/hys/MidBoss 2/왕실 폐허 보스 아레나 적용")]
    private static void ApplyFromMenu()
    {
        ApplyArena();
    }

    public static void ApplyArenaFromCommandLine()
    {
        ApplyArena();
    }

    public static void CaptureArenaPreviewFromCommandLine()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Camera camera = Camera.main != null
            ? Camera.main
            : Object.FindFirstObjectByType<Camera>();
        if (camera == null) throw new MissingReferenceException("아레나 미리보기를 렌더링할 카메라가 없습니다.");

        // 중앙 보스방을 16:9 게임 화면으로 렌더링해 배경과 타일 배치를 눈으로 확인합니다.
        camera.transform.SetPositionAndRotation(new Vector3(0f, 3.1f, -10f), Quaternion.identity);
        camera.orthographic = true;
        camera.orthographicSize = 5.6f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.018f, 0.027f, 0.055f, 1f);

        RenderTexture target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        Texture2D screenshot = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        screenshot.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
        screenshot.Apply();

        string outputPath = Path.Combine(Path.GetTempPath(), "hys_MidBoss2_ArenaPreview_20260824.png");
        File.WriteAllBytes(outputPath, screenshot.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(screenshot);
        Debug.Log("[hys MidBoss2 Arena Preview] PASS - " + outputPath);
    }

    public static void ApplyArena()
    {
        ConfigureSpriteImporter(BackgroundPath, 100f);
        ConfigureSpriteImporter(FloorTexturePath, 1256f);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Sprite backgroundSprite = RequireAsset<Sprite>(BackgroundPath);
        Sprite floorSprite = RequireAsset<Sprite>(FloorTexturePath);

        ConfigurePlayerCircleCollider();
        ConfigureBossEntrySpawn();
        ConfigureEnvironment(scene, backgroundSprite, floorSprite);
        ConfigureCameraClearColor();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateArena();
        Debug.Log("[hys MidBoss2 Arena Setup] PASS - 범위 스폰, 원형 플레이어 콜라이더, 왕실 폐허 배경과 충돌 타일맵을 적용했습니다.");
    }

    private static void ConfigurePlayerCircleCollider()
    {
        GameObject player = GameObject.Find(PlayerName);
        if (player == null) throw new MissingReferenceException("중간보스2 씬에서 HWJ_Player를 찾지 못했습니다.");

        BoxCollider2D[] boxes = player.GetComponents<BoxCollider2D>();
        for (int i = 0; i < boxes.Length; i++)
        {
            if (boxes[i] != null && !boxes[i].isTrigger)
                Object.DestroyImmediate(boxes[i]);
        }

        CircleCollider2D circle = player.GetComponent<CircleCollider2D>();
        if (circle == null) circle = player.AddComponent<CircleCollider2D>();
        // 발밑 중심의 원형 몸체로 벽 모서리에 걸리는 현상을 줄입니다.
        circle.isTrigger = false;
        circle.offset = new Vector2(-0.08f, -0.88f);
        circle.radius = 0.9f;
        circle.enabled = true;
        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(circle);
    }

    private static void ConfigureBossEntrySpawn()
    {
        hys_MidBoss2SpawnSystem spawnSystem = Object.FindFirstObjectByType<hys_MidBoss2SpawnSystem>(
            FindObjectsInactive.Include);
        if (spawnSystem == null) throw new MissingReferenceException("HYS 중간보스2 생성 시스템이 없습니다.");
        spawnSystem.ConfigurePlayerEntry(BossEntryRange, true);
        EditorUtility.SetDirty(spawnSystem);
    }

    private static void ConfigureEnvironment(Scene scene, Sprite backgroundSprite, Sprite floorSprite)
    {
        RemoveExistingObject(EnvironmentRootName);
        RemoveExistingObject(OldGroundName);

        GameObject root = new GameObject(EnvironmentRootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        root.AddComponent<hys_MidBoss2PlayerCircleColliderAdapter>();

        // 접근 구간과 중앙 전투장을 모두 덮도록 동일 배경을 세 구간에 배치합니다.
        CreateBackground(root.transform, backgroundSprite, -36f, "Approach");
        CreateBackground(root.transform, backgroundSprite, 0f, "Arena");
        CreateBackground(root.transform, backgroundSprite, 36f, "ExitSide");

        Tile floorTile = CreateOrUpdateFloorTile(floorSprite);
        GameObject tilemapObject = GameObject.Find(TilemapName);
        if (tilemapObject == null) throw new MissingReferenceException("중간보스2 씬의 TEST MAP 타일맵이 없습니다.");

        Tilemap tilemap = tilemapObject.GetComponent<Tilemap>();
        TilemapRenderer renderer = tilemapObject.GetComponent<TilemapRenderer>();
        TilemapCollider2D collider = tilemapObject.GetComponent<TilemapCollider2D>();
        if (tilemap == null || renderer == null || collider == null)
            throw new MissingReferenceException("TEST MAP의 Tilemap 구성요소가 완전하지 않습니다.");

        Grid parentGrid = tilemapObject.GetComponentInParent<Grid>();
        if (parentGrid != null)
        {
            // 기존 테스트 Grid의 임시 오프셋을 제거해 타일 상단을 월드 바닥 y=0에 맞춥니다.
            parentGrid.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            parentGrid.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(parentGrid.transform);
        }

        tilemap.ClearAllTiles();
        for (int x = -35; x <= 35; x++)
        {
            for (int y = -5; y <= -1; y++)
                tilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
        }

        tilemap.color = new Color(0.82f, 0.86f, 0.95f, 1f);
        renderer.sortingOrder = -10;
        collider.enabled = true;
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0) tilemapObject.layer = groundLayer;
        EditorUtility.SetDirty(tilemap);
        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(collider);
    }

    private static void CreateBackground(Transform parent, Sprite sprite, float x, string suffix)
    {
        GameObject background = new GameObject("hys_MidBoss2_Background_" + suffix);
        background.transform.SetParent(parent, false);
        background.transform.position = new Vector3(x, 5f, 8f);
        SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = -100;
        renderer.color = new Color(0.82f, 0.88f, 1f, 1f);

        float sourceWidth = sprite.bounds.size.x;
        float scale = sourceWidth > 0.01f ? 36f / sourceWidth : 1f;
        background.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static Tile CreateOrUpdateFloorTile(Sprite sprite)
    {
        EnsureFolder("Assets/02Scripts/hys/Boss/Environment");
        EnsureFolder(TileFolder);
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(FloorTileAssetPath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = "hys_MidBoss2_RoyalRuins_FloorTile";
            AssetDatabase.CreateAsset(tile, FloorTileAssetPath);
        }

        tile.sprite = sprite;
        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void ConfigureCameraClearColor()
    {
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == null) continue;
            cameras[i].clearFlags = CameraClearFlags.SolidColor;
            cameras[i].backgroundColor = new Color(0.018f, 0.027f, 0.055f, 1f);
            EditorUtility.SetDirty(cameras[i]);
        }
    }

    private static void ConfigureSpriteImporter(string path, float pixelsPerUnit)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new MissingReferenceException("이미지 Importer가 없습니다: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void ValidateArena()
    {
        hys_MidBoss2SpawnSystem spawnSystem = Object.FindFirstObjectByType<hys_MidBoss2SpawnSystem>(
            FindObjectsInactive.Include);
        GameObject player = GameObject.Find(PlayerName);
        Tilemap tilemap = GameObject.Find(TilemapName)?.GetComponent<Tilemap>();
        GameObject environment = GameObject.Find(EnvironmentRootName);

        Require(spawnSystem != null && spawnSystem.SpawnWhenPlayerEntersRange
            && Mathf.Abs(spawnSystem.PlayerEnterRange - BossEntryRange) < 0.01f,
            "보스 진입 범위 생성 설정이 없습니다.");
        Require(player != null && player.GetComponent<CircleCollider2D>() != null,
            "플레이어 원형 콜라이더가 없습니다.");
        Require(player.GetComponent<BoxCollider2D>() == null,
            "플레이어 네모 몸 콜라이더가 남아 있습니다.");
        Require(environment != null && environment.GetComponentsInChildren<SpriteRenderer>(true).Length == 3,
            "왕실 폐허 배경 3구간이 구성되지 않았습니다.");
        Require(environment.GetComponent<hys_MidBoss2PlayerCircleColliderAdapter>() != null,
            "지속 플레이어용 원형 콜라이더 어댑터가 없습니다.");
        Require(tilemap != null && tilemap.GetUsedTilesCount() > 0
            && tilemap.GetComponent<TilemapCollider2D>() != null,
            "왕실 폐허 충돌 타일맵이 구성되지 않았습니다.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        string name = path.Substring(slash + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static T RequireAsset<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new MissingReferenceException("필수 에셋을 찾지 못했습니다: " + path);
        return asset;
    }

    private static void RemoveExistingObject(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) Object.DestroyImmediate(existing);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}
