#if UNITY_EDITOR
using System;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// hys 중간보스2 테스트 씬에 전용 외형, 로직, 패턴 데이터와 Animator를 한 번에 연결합니다.
public static class hys_MidBoss2SceneSetup
{
    private const string ScenePath = "Assets/01Scenes/Soul_Test/hys middle boss2.unity";
    private const string BossPrefabPath =
        "Assets/02Scripts/HWJ/Prefabs/Generated/Bosses/HWJ_MidBoss2_Runtime_Prefab.prefab";
    private const string HysPrefabPath = "Assets/02Scripts/hys/Boss/Prefabs/hys_MidBoss2.prefab";
    private const string SpritePath = "Assets/05Anims/MidBoss_02/Sprites/hys_MidBoss2_Main.png";
    private const string ControllerPath = "Assets/05Anims/MidBoss_02/hys_MidBoss2.controller";
    private const string DialogueFontPath = "Assets/07Font/Maplestory Light.ttf";
    private const string AccentFontAssetPath = "Assets/07Font/Maplestory Bold SDF.asset";
    private const string BossObjectName = "hys_MidBoss2_SceneBoss";
    private const string VisualObjectName = "hys_MidBoss2_Visual";
    private const string GroundObjectName = "hys_MidBoss2_TestGround";
    private const string SpawnRootName = "hys_MidBoss2_SpawnRoot";
    private const string SpawnPointName = "hys_MidBoss2_BossSpawnPoint";
    private const string SpawnPointId = "hys_midboss2_spawn";
    private const string CinemachineCameraName = "hys_MidBoss2_CinemachineCamera";
    private const string BossDisplayName = "붉은 기사단장 바르칸";
    private const float HealthBarHeight = 3.45f;
    private const float HealthBarWidth = 3.2f;

    private static readonly string[] PatternPaths =
    {
        "Assets/02Scripts/hys/Boss/Data/hys_SecondBoss_Pattern1.asset",
        "Assets/02Scripts/hys/Boss/Data/hys_SecondBoss_Pattern2.asset",
        "Assets/02Scripts/hys/Boss/Data/hys_SecondBoss_Pattern3.asset",
        "Assets/02Scripts/hys/Boss/Data/hys_SecondBoss_Pattern4.asset",
        "Assets/02Scripts/hys/Boss/Data/hys_SecondBoss_Pattern5.asset",
        "Assets/02Scripts/hys/Boss/Data/hys_SecondBoss_Pattern6.asset"
    };

    private static readonly string[] SummonPrefabPaths =
    {
        "Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady/Enemies/HWJ_Runtime_Enemy_General_Slime.prefab",
        "Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady/Enemies/HWJ_Runtime_Enemy_General_Rat.prefab",
        "Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady/Enemies/HWJ_Runtime_Enemy_Possessable_Sword.prefab",
        "Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady/Enemies/HWJ_Runtime_Enemy_Possessable_Axe.prefab",
        "Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady/Enemies/HWJ_Runtime_Enemy_Possessable_Bow.prefab"
    };

    [MenuItem("Tools/hys/MidBoss 2/hys 테스트 씬 구성")]
    public static void SetupFromMenu()
    {
        SetupScene();
    }

    // 배치 검증에서도 같은 구성 과정을 실행할 수 있게 공개 진입점을 유지합니다.
    public static void SetupScene()
    {
        ConfigureSpriteImporter();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RemoveExistingObject(BossObjectName);
        RemoveExistingObject(GroundObjectName);
        RemoveExistingObject(SpawnRootName);

        GameObject prefab = RequireAsset<GameObject>(BossPrefabPath);
        GameObject boss = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (boss == null)
        {
            throw new InvalidOperationException("중간보스2 프리팹 인스턴스를 만들지 못했습니다.");
        }

        boss.name = BossObjectName;
        boss.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        boss.transform.localScale = Vector3.one;

        SpriteRenderer originalRenderer = RequireComponent<SpriteRenderer>(boss);
        originalRenderer.enabled = false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(originalRenderer);

        // 원본 프리팹 외형과 분리된 hys 전용 Visual을 사용해 씬 오버라이드를 안정적으로 유지합니다.
        GameObject visual = new GameObject(VisualObjectName);
        visual.transform.SetParent(boss.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = RequireAsset<Sprite>(SpritePath);
        renderer.color = Color.white;
        renderer.sortingOrder = 10;
        renderer.flipX = true;

        Animator animator = RequireComponent<Animator>(boss);
        animator.runtimeAnimatorController = RequireAsset<RuntimeAnimatorController>(ControllerPath);
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

        hys_SecondBossPattern pattern = GetOrAdd<hys_SecondBossPattern>(boss);
        hys_SecondBossLogic logic = GetOrAdd<hys_SecondBossLogic>(boss);
        hys_SecondBossAnimatorBridge bridge = GetOrAdd<hys_SecondBossAnimatorBridge>(boss);
        hys_SecondBossPatternExecutor executor = GetOrAdd<hys_SecondBossPatternExecutor>(boss);
        hys_SecondBossPresentation presentation = GetOrAdd<hys_SecondBossPresentation>(boss);
        hys_SecondBossDialogueStyler dialogueStyler = GetOrAdd<hys_SecondBossDialogueStyler>(boss);
        hys_SecondBossSummonSpawner summonSpawner = GetOrAdd<hys_SecondBossSummonSpawner>(boss);
        hys_SecondBossPhaseTransitionVisual phaseVisual = GetOrAdd<hys_SecondBossPhaseTransitionVisual>(boss);
        CinemachineImpulseSource impulseSource = GetOrAdd<CinemachineImpulseSource>(boss);
        HWJ_BossDialogueBubbleSystem dialogue = RequireComponent<HWJ_BossDialogueBubbleSystem>(boss);
        HWJ_BossCameraFocusSystem cameraFocus = RequireComponent<HWJ_BossCameraFocusSystem>(boss);
        TextMesh bossNameText = FindChildComponentByName<TextMesh>(boss, "BossName");

        ConnectPatternReferences(pattern, boss, renderer);
        ConfigureSummonSpawner(summonSpawner);
        ConnectLogicReferences(logic, boss, renderer, pattern);
        ConfigureDialogue(dialogue);
        ConfigureDialogueStyler(dialogueStyler);
        bridge.Initialize(logic, pattern, animator);
        executor.Initialize(pattern);
        CinemachineCamera cinemachineCamera = ConfigureCamera(scene, boss);
        ConfigureCameraFocus(cameraFocus, cinemachineCamera);
        presentation.Initialize(
            logic,
            pattern,
            boss.GetComponent<HWJ_RuntimeStatusSystem>(),
            renderer,
            boss.GetComponent<Rigidbody2D>(),
            dialogue,
            cameraFocus,
            impulseSource,
            bossNameText);
        ConfigureBossHealthBar(boss);
        DisableCompetingBossControllers(boss);
        CreateGround(scene);

        EditorUtility.SetDirty(boss);
        EditorUtility.SetDirty(visual);
        EditorUtility.SetDirty(originalRenderer);
        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(animator);
        EditorUtility.SetDirty(pattern);
        EditorUtility.SetDirty(logic);
        EditorUtility.SetDirty(bridge);
        EditorUtility.SetDirty(executor);
        EditorUtility.SetDirty(presentation);
        EditorUtility.SetDirty(dialogueStyler);
        EditorUtility.SetDirty(summonSpawner);
        EditorUtility.SetDirty(phaseVisual);
        EditorUtility.SetDirty(impulseSource);
        EditorUtility.SetDirty(dialogue);
        EditorUtility.SetDirty(cameraFocus);

        // HWJ 원본을 건드리지 않고 HYS가 소유하는 Prefab Variant로 씬 구성을 저장합니다.
        EnsureFolder("Assets/02Scripts/hys/Boss/Prefabs");
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
            boss,
            HysPrefabPath,
            InteractionMode.AutomatedAction);
        if (savedPrefab == null)
        {
            throw new InvalidOperationException("HYS 중간보스2 Prefab Variant를 저장하지 못했습니다.");
        }

        // 편집 씬에는 보스 본체를 남기지 않고 플레이어 시작 지점과 같은 런타임 스폰 마커만 둡니다.
        Vector3 bossSpawnPosition = boss.transform.position;
        Quaternion bossSpawnRotation = boss.transform.rotation;
        UnityEngine.Object.DestroyImmediate(boss);
        GameObject hysBossPrefab = RequireAsset<GameObject>(HysPrefabPath);
        CreateBossSpawnSystem(scene, hysBossPrefab, bossSpawnPosition, bossSpawnRotation);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        ValidateScene(scene);
        Debug.Log("[hys MidBoss2 Scene Setup] PASS - HYS 보스 프리팹과 Boss 타입 스폰포인트 기반 생성을 확인했습니다.");
    }

    private static void CreateBossSpawnSystem(
        Scene scene,
        GameObject bossPrefab,
        Vector3 position,
        Quaternion rotation)
    {
        GameObject root = new GameObject(SpawnRootName);
        SceneManager.MoveGameObjectToScene(root, scene);

        GameObject pointObject = new GameObject(SpawnPointName);
        pointObject.transform.SetParent(root.transform, false);
        pointObject.transform.SetPositionAndRotation(position, rotation);
        hys_MidBoss2SpawnPoint point = pointObject.AddComponent<hys_MidBoss2SpawnPoint>();
        point.Initialize(SpawnPointId);

        // 공용 스폰 도구에서도 보스 마커로 인식되도록 HWJ_SpawnPoint의 Boss 타입을 함께 사용합니다.
        HWJ_SpawnPoint sharedPoint = pointObject.GetComponent<HWJ_SpawnPoint>();
        SerializedObject sharedSerialized = new SerializedObject(sharedPoint);
        sharedSerialized.FindProperty("pointId").stringValue = SpawnPointId;
        sharedSerialized.FindProperty("spawnPointType").enumValueIndex = (int)HWJ_SpawnPointType.Boss;
        sharedSerialized.FindProperty("spawnParent").objectReferenceValue = null;
        sharedSerialized.FindProperty("gizmoRadius").floatValue = 0.9f;
        sharedSerialized.ApplyModifiedPropertiesWithoutUndo();

        hys_MidBoss2SpawnSystem spawnSystem = root.AddComponent<hys_MidBoss2SpawnSystem>();
        spawnSystem.Initialize(bossPrefab, point);
        // 플레이어가 중앙 전투 범위에 들어오기 전에는 보스와 인트로를 생성하지 않습니다.
        spawnSystem.ConfigurePlayerEntry(10f, true);
        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(pointObject);
        EditorUtility.SetDirty(point);
        EditorUtility.SetDirty(sharedPoint);
        EditorUtility.SetDirty(spawnSystem);
    }

    private static void ConfigureSpriteImporter()
    {
        AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException($"스프라이트 Importer를 찾지 못했습니다: {SpritePath}");
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32f;
        // Unity 6에서는 스프라이트 정렬과 피벗을 TextureImporterSettings로 적용합니다.
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0f);
        importer.SetTextureSettings(settings);
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void ConnectPatternReferences(
        hys_SecondBossPattern pattern,
        GameObject boss,
        SpriteRenderer renderer)
    {
        SerializedObject serialized = new SerializedObject(pattern);
        SetObject(serialized, "dataResolver", boss.GetComponent<HWJ_RootObjectDataResolver>());
        SetObject(serialized, "combatSystem", boss.GetComponent<HWJ_CombatSystem>());
        SetObject(serialized, "runtimeStatus", boss.GetComponent<HWJ_RuntimeStatusSystem>());
        SetObject(serialized, "body", boss.GetComponent<Rigidbody2D>());
        SetObject(serialized, "bodyCollider", boss.GetComponent<Collider2D>());
        SetObject(serialized, "spriteRenderer", renderer);
        SetObject(serialized, "summonSpawner", boss.GetComponent<hys_SecondBossSummonSpawner>());
        SetAssetArray<HWJ_BossPatternDataSO>(serialized, "patternData", PatternPaths);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureSummonSpawner(hys_SecondBossSummonSpawner summonSpawner)
    {
        GameObject[] prefabs = new GameObject[SummonPrefabPaths.Length];
        for (int i = 0; i < SummonPrefabPaths.Length; i++)
            prefabs[i] = RequireAsset<GameObject>(SummonPrefabPaths[i]);
        summonSpawner.Configure(prefabs);
    }

    private static void ConnectLogicReferences(
        hys_SecondBossLogic logic,
        GameObject boss,
        SpriteRenderer renderer,
        hys_SecondBossPattern pattern)
    {
        SerializedObject serialized = new SerializedObject(logic);
        SetObject(serialized, "dataResolver", boss.GetComponent<HWJ_RootObjectDataResolver>());
        SetObject(serialized, "runtimeStatus", boss.GetComponent<HWJ_RuntimeStatusSystem>());
        SetObject(serialized, "body", boss.GetComponent<Rigidbody2D>());
        SetObject(serialized, "spriteRenderer", renderer);
        SetObject(serialized, "patternSystem", pattern);
        serialized.FindProperty("autoFindPlayerTarget").boolValue = true;
        serialized.FindProperty("autoStartEncounter").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void DisableCompetingBossControllers(GameObject boss)
    {
        // 테스트 씬에서는 hys 보스 로직만 패턴과 이동을 제어하도록 기존 HWJ 두뇌만 비활성화합니다.
        foreach (MonoBehaviour behaviour in boss.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null) continue;
            string typeName = behaviour.GetType().Name;
            if (typeName == "HWJ_BossBrainSystem"
                || typeName == "HWJ_BossPatternSystem"
                || typeName == "HWJ_MidBossPatternSystem")
            {
                behaviour.enabled = false;
                EditorUtility.SetDirty(behaviour);
                PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
            }
        }
    }

    private static void ConfigureBossHealthBar(GameObject boss)
    {
        HWJ_BossHealthBarSystem healthBar = boss.GetComponentInChildren<HWJ_BossHealthBarSystem>(true);
        if (healthBar == null) throw new InvalidOperationException("보스 체력바를 찾지 못했습니다.");

        // 큰 보스 외형을 가리지 않도록 체력바를 머리 위로 올리고 폭을 외형에 맞게 줄입니다.
        Transform barTransform = healthBar.transform;
        barTransform.localPosition = new Vector3(0f, HealthBarHeight, 0f);

        SerializedObject serialized = new SerializedObject(healthBar);
        SerializedProperty widthProperty = serialized.FindProperty("barWidth");
        SerializedProperty backgroundProperty = serialized.FindProperty("backgroundRenderer");
        SerializedProperty fillProperty = serialized.FindProperty("fillRenderer");
        if (widthProperty == null || backgroundProperty == null || fillProperty == null)
            throw new InvalidOperationException("보스 체력바 표시 필드를 찾지 못했습니다.");

        widthProperty.floatValue = HealthBarWidth;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        float halfWidth = HealthBarWidth * 0.5f;
        SetHealthBarLine(backgroundProperty.objectReferenceValue as LineRenderer, -halfWidth, halfWidth);
        SetHealthBarLine(fillProperty.objectReferenceValue as LineRenderer, -halfWidth, halfWidth);

        TextMesh bossName = FindChildComponentByName<TextMesh>(boss, "BossName");
        if (bossName != null)
        {
            bossName.text = BossDisplayName;
            bossName.gameObject.SetActive(true);
            EditorUtility.SetDirty(bossName);
        }

        EditorUtility.SetDirty(healthBar);
        EditorUtility.SetDirty(barTransform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(healthBar);
        PrefabUtility.RecordPrefabInstancePropertyModifications(barTransform);
    }

    private static void SetHealthBarLine(LineRenderer line, float startX, float endX)
    {
        if (line == null) throw new InvalidOperationException("보스 체력바 선 Renderer가 없습니다.");
        line.positionCount = 2;
        line.SetPosition(0, new Vector3(startX, 0f, 0f));
        line.SetPosition(1, new Vector3(endX, 0f, 0f));
        line.enabled = true;
        EditorUtility.SetDirty(line);
        PrefabUtility.RecordPrefabInstancePropertyModifications(line);
    }

    private static void CreateGround(Scene scene)
    {
        GameObject ground = new GameObject(GroundObjectName);
        SceneManager.MoveGameObjectToScene(ground, scene);
        ground.transform.position = new Vector3(0f, -1.5f, 0f);
        BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(30f, 1f);
    }

    private static CinemachineCamera ConfigureCamera(Scene scene, GameObject boss)
    {
        Camera camera = Camera.main;

        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
        }

        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 1.5f, -10f);
        camera.transform.rotation = Quaternion.identity;
        camera.backgroundColor = new Color(0.035f, 0.04f, 0.06f, 1f);
        GetOrAdd<CinemachineBrain>(camera.gameObject);
        EditorUtility.SetDirty(camera);

        // HYS 테스트 씬에서는 Main Camera 한 대만 출력과 오디오를 담당하게 정리합니다.
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Camera[] sceneCameras = root.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < sceneCameras.Length; i++)
            {
                if (sceneCameras[i] == null || sceneCameras[i] == camera) continue;
                sceneCameras[i].enabled = false;
                AudioListener otherListener = sceneCameras[i].GetComponent<AudioListener>();
                if (otherListener != null) otherListener.enabled = false;
                EditorUtility.SetDirty(sceneCameras[i]);
                if (otherListener != null) EditorUtility.SetDirty(otherListener);
            }
        }

        GameObject cinemachineObject = GameObject.Find(CinemachineCameraName);
        if (cinemachineObject == null)
        {
            cinemachineObject = new GameObject(CinemachineCameraName);
            SceneManager.MoveGameObjectToScene(cinemachineObject, scene);
        }

        CinemachineCamera cinemachineCamera = GetOrAdd<CinemachineCamera>(cinemachineObject);
        CinemachineFollow follow = GetOrAdd<CinemachineFollow>(cinemachineObject);
        GetOrAdd<CinemachineImpulseListener>(cinemachineObject);
        cinemachineCamera.Priority = 20;
        cinemachineCamera.Lens.OrthographicSize = 5f;
        cinemachineCamera.Lens.NearClipPlane = 0.1f;
        cinemachineCamera.Lens.FarClipPlane = 1000f;
        cinemachineCamera.Target.TrackingTarget = FindPlayerTransform(scene) ?? boss.transform;
        follow.FollowOffset = new Vector3(0f, 1.5f, -10f);
        cinemachineObject.transform.position = new Vector3(0f, 1.5f, -10f);

        EditorUtility.SetDirty(cinemachineCamera);
        EditorUtility.SetDirty(follow);
        return cinemachineCamera;
    }

    private static void ConfigureDialogue(HWJ_BossDialogueBubbleSystem dialogue)
    {
        SerializedObject serialized = new SerializedObject(dialogue);
        SetStringArray(serialized, "introDialogueSequence", new[]
        {
            "멈춰라.",
            "한 걸음도 더 오지 마라.",
            "여기는 왕실의 처형장이다.",
            "나는 기사단장 바르칸.",
            "왕명을 받들어 반역자를 베었다.",
            "그 수를 세는 일은 오래전에 그만뒀지.",
            "죄의 무게는 왕께서 정한다.",
            "나는 검을 내릴 뿐이다.",
            "네가 걸친 육신도 빼앗은 것이겠지.",
            "그렇다면 영혼까지 심문하겠다.",
            "방패 뒤로 숨지 않겠다.",
            "와라.",
            "네 충성을 피로 증명해라."
        });
        SetStringArray(serialized, "phaseTwoDialogueSequence", new[]
        {
            "훌륭하다.",
            "내 갑옷을 꺾었군.",
            "오래 잊고 있던 고통이다.",
            "그렇다면 금기를 열겠다.",
            "왕실은 이 힘을 두려워했다.",
            "그래서 내게 봉인하라 명했지.",
            "하지만 왕명은 아직 끝나지 않았다.",
            "죽은 기사들이여.",
            "다시 검을 들어라.",
            "반역자의 길을 막아라.",
            "이곳은 이제 처형장이다.",
            "산 자의 법은 버려라.",
            "두 번째 처형을 시작한다."
        });
        SetStringArray(serialized, "deathDialogueSequence", new[]
        {
            "내 검이... 멈췄다고?",
            "가라. 다음 문은 네가 열어라."
        });
        serialized.FindProperty("soulLostDialogue").stringValue = "몸을 잃고 도망칠 생각인가.";
        serialized.FindProperty("sequenceLineDurationSeconds").floatValue = 1.25f;
        serialized.FindProperty("secondsPerCharacter").floatValue = 0.055f;
        serialized.FindProperty("maximumLineDurationSeconds").floatValue = 2.1f;
        serialized.FindProperty("sequenceGapSeconds").floatValue = 0.08f;
        serialized.FindProperty("defaultDurationSeconds").floatValue = 1.5f;
        // HYS 전용 스타일러가 덧씌우기 전 첫 프레임에도 충분히 읽히는 기본 크기와 색을 지정합니다.
        serialized.FindProperty("dialogueFontSize").intValue = 56;
        serialized.FindProperty("dialogueCharacterSize").floatValue = 0.072f;
        serialized.FindProperty("maxCharactersPerLine").intValue = 18;
        serialized.FindProperty("horizontalBubblePadding").floatValue = 1.8f;
        serialized.FindProperty("verticalBubblePadding").floatValue = 1.15f;
        serialized.FindProperty("textColor").colorValue = new Color(1f, 0.93f, 0.76f, 1f);
        serialized.FindProperty("bubbleColor").colorValue = new Color(0.035f, 0.045f, 0.075f, 0.94f);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureDialogueStyler(hys_SecondBossDialogueStyler styler)
    {
        SerializedObject serialized = new SerializedObject(styler);
        SetObject(serialized, "dialogueFont", RequireAsset<Font>(DialogueFontPath));
        SetObject(serialized, "accentFontAsset", RequireAsset<TMP_FontAsset>(AccentFontAssetPath));
        serialized.FindProperty("dialogueTmpFontSize").floatValue = 46f;
        serialized.FindProperty("titleTmpFontSize").floatValue = 32f;
        serialized.FindProperty("bossNameTmpFontSize").floatValue = 34f;
        serialized.FindProperty("minimumPanelWidth").floatValue = 7.6f;
        serialized.FindProperty("minimumPanelHeight").floatValue = 1.95f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureCameraFocus(
        HWJ_BossCameraFocusSystem cameraFocus,
        CinemachineCamera cinemachineCamera)
    {
        SerializedObject serialized = new SerializedObject(cameraFocus);
        SetObject(serialized, "targetCinemachineCamera", cinemachineCamera);
        serialized.FindProperty("dialogueFocusOrthographicSize").floatValue = 3.7f;
        serialized.FindProperty("zoomLerpSpeed").floatValue = 5f;
        serialized.FindProperty("returnBlendSeconds").floatValue = 0.55f;
        serialized.FindProperty("focusOffset").vector2Value = new Vector2(0f, 1.25f);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ValidateScene(Scene scene)
    {
        if (GameObject.Find(BossObjectName) != null)
            throw new InvalidOperationException("편집 씬에 보스 본체가 직접 배치되어 있습니다.");

        hys_MidBoss2SpawnPoint point = UnityEngine.Object.FindFirstObjectByType<hys_MidBoss2SpawnPoint>();
        hys_MidBoss2SpawnSystem spawnSystem = UnityEngine.Object.FindFirstObjectByType<hys_MidBoss2SpawnSystem>();
        if (point == null || point.gameObject.scene != scene || point.SpawnId != SpawnPointId)
            throw new InvalidOperationException("HYS 중간보스2 스폰포인트가 없습니다.");
        if (point.SharedSpawnPoint == null
            || point.SharedSpawnPoint.SpawnPointType != HWJ_SpawnPointType.Boss
            || point.SharedSpawnPoint.PointId != SpawnPointId)
            throw new InvalidOperationException("공용 Boss 타입 스폰포인트 설정이 올바르지 않습니다.");
        if (spawnSystem == null
            || spawnSystem.gameObject.scene != scene
            || spawnSystem.SpawnPoint != point
            || spawnSystem.BossPrefab == null
            || AssetDatabase.GetAssetPath(spawnSystem.BossPrefab) != HysPrefabPath)
            throw new InvalidOperationException("HYS 중간보스2 생성 시스템이 프리팹과 연결되지 않았습니다.");
        if (!spawnSystem.SpawnWhenPlayerEntersRange
            || !Mathf.Approximately(spawnSystem.PlayerEnterRange, 10f))
            throw new InvalidOperationException("HYS 중간보스2가 플레이어 진입 범위 생성 방식이 아닙니다.");

        GameObject boss = RequireAsset<GameObject>(HysPrefabPath);
        if (boss.GetComponent<hys_SecondBossLogic>() == null) throw new InvalidOperationException("보스 로직이 없습니다.");
        if (boss.GetComponent<hys_SecondBossPattern>() == null) throw new InvalidOperationException("보스 패턴이 없습니다.");
        if (boss.GetComponent<hys_SecondBossAnimatorBridge>() == null) throw new InvalidOperationException("Animator 연결기가 없습니다.");
        if (boss.GetComponent<hys_SecondBossPatternExecutor>() == null) throw new InvalidOperationException("HWJ 패턴 어댑터가 없습니다.");
        hys_SecondBossPresentation presentation = boss.GetComponent<hys_SecondBossPresentation>();
        if (presentation == null) throw new InvalidOperationException("HYS 보스 연출 컨트롤러가 없습니다.");
        if (boss.GetComponent<hys_SecondBossDialogueStyler>() == null)
            throw new InvalidOperationException("HYS 보스 대사 스타일러가 없습니다.");
        hys_SecondBossSummonSpawner summonSpawner = boss.GetComponent<hys_SecondBossSummonSpawner>();
        if (summonSpawner == null || !summonSpawner.HasConfiguredPrefabs)
            throw new InvalidOperationException("HYS 보스 소환 스포너가 구성되지 않았습니다.");
        if (boss.GetComponent<hys_SecondBossPhaseTransitionVisual>() == null)
            throw new InvalidOperationException("HYS 2페이즈 의식 연출기가 없습니다.");
        if (presentation.BossDisplayName != BossDisplayName)
            throw new InvalidOperationException("보스 표시 이름이 올바르지 않습니다.");
        if (boss.GetComponent<CinemachineImpulseSource>() == null)
            throw new InvalidOperationException("Cinemachine Impulse Source가 없습니다.");

        HWJ_BossDialogueBubbleSystem dialogue = boss.GetComponent<HWJ_BossDialogueBubbleSystem>();
        if (dialogue == null
            || dialogue.GetSequenceLineCount(HWJ_BossDialogueSequenceType.Intro) < 12
            || dialogue.GetSequenceLineCount(HWJ_BossDialogueSequenceType.PhaseTransition) < 12)
            throw new InvalidOperationException("보스 컨셉 대사가 충분히 구성되지 않았습니다.");

        Transform visual = boss.transform.Find(VisualObjectName);
        SpriteRenderer renderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        Animator animator = boss.GetComponentInChildren<Animator>(true);
        if (renderer == null || renderer.sprite == null
            || AssetDatabase.GetAssetPath(renderer.sprite) != SpritePath)
            throw new InvalidOperationException("중간보스2 스프라이트 연결이 올바르지 않습니다.");
        if (animator == null || animator.runtimeAnimatorController == null
            || animator.runtimeAnimatorController.name != "hys_MidBoss2")
            throw new InvalidOperationException("중간보스2 Animator 연결이 올바르지 않습니다.");

        HWJ_BossHealthBarSystem healthBar = boss.GetComponentInChildren<HWJ_BossHealthBarSystem>(true);
        if (healthBar == null || !Mathf.Approximately(healthBar.transform.localPosition.y, HealthBarHeight))
            throw new InvalidOperationException("중간보스2 체력바 위치가 올바르지 않습니다.");
        SerializedProperty widthProperty = new SerializedObject(healthBar).FindProperty("barWidth");
        if (widthProperty == null || !Mathf.Approximately(widthProperty.floatValue, HealthBarWidth))
            throw new InvalidOperationException("중간보스2 체력바 폭이 올바르지 않습니다.");

        TextMesh bossName = FindChildComponentByName<TextMesh>(boss, "BossName");
        if (bossName == null || bossName.text != BossDisplayName)
            throw new InvalidOperationException("체력바 보스 이름이 올바르지 않습니다.");
        CinemachineCamera cinemachineCamera = UnityEngine.Object.FindFirstObjectByType<CinemachineCamera>();
        if (cinemachineCamera == null
            || cinemachineCamera.GetComponent<CinemachineImpulseListener>() == null)
            throw new InvalidOperationException("Cinemachine 카메라 또는 Impulse Listener가 없습니다.");
        if (Camera.main == null || Camera.main.GetComponent<CinemachineBrain>() == null)
            throw new InvalidOperationException("Main Camera에 Cinemachine Brain이 없습니다.");
    }

    private static void RemoveExistingObject(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static T RequireComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponentInChildren<T>(true);
        if (component == null) throw new InvalidOperationException($"필수 컴포넌트가 없습니다: {typeof(T).Name}");
        return component;
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException($"필수 에셋이 없습니다: {path}");
        return asset;
    }

    private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) throw new InvalidOperationException($"직렬화 필드를 찾지 못했습니다: {propertyName}");
        property.objectReferenceValue = value;
    }

    private static void SetAssetArray<T>(SerializedObject serialized, string propertyName, string[] paths)
        where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || !property.isArray)
            throw new InvalidOperationException($"배열 필드를 찾지 못했습니다: {propertyName}");

        property.arraySize = paths.Length;
        for (int i = 0; i < paths.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = RequireAsset<T>(paths[i]);
        }
    }

    private static void SetStringArray(
        SerializedObject serialized,
        string propertyName,
        string[] values)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || !property.isArray)
            throw new InvalidOperationException($"문자열 배열을 찾지 못했습니다: {propertyName}");

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).stringValue = values[i];
    }

    private static Transform FindPlayerTransform(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            HWJ_RootObjectDataResolver[] resolvers = root.GetComponentsInChildren<HWJ_RootObjectDataResolver>(true);
            for (int i = 0; i < resolvers.Length; i++)
            {
                if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
                    return resolvers[i].transform;
            }
        }
        return null;
    }

    private static T FindChildComponentByName<T>(GameObject root, string objectName)
        where T : Component
    {
        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].name == objectName)
                return components[i];
        }
        return null;
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
