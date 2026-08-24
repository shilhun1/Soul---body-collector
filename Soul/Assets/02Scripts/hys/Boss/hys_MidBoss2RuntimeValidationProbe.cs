#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// 명령줄 검증에서만 생성되어 인트로부터 사망까지 실제 Play Mode 흐름을 확인합니다.
[DefaultExecutionOrder(-200)]
public class hys_MidBoss2RuntimeValidationProbe : MonoBehaviour
{
    private const string ValidationArgument = "-hysMidBoss2PresentationValidate";
    private bool failed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), ValidationArgument) < 0) return;
        new GameObject("hys_MidBoss2RuntimeValidationProbe")
            .AddComponent<hys_MidBoss2RuntimeValidationProbe>();
    }

    private IEnumerator Start()
    {
        yield return null;
        hys_MidBoss2SpawnSystem bossSpawnSystem = FindFirstObjectByType<hys_MidBoss2SpawnSystem>();
        hys_MidBoss2SpawnPoint bossSpawnPoint = FindFirstObjectByType<hys_MidBoss2SpawnPoint>();
        Transform player = ResolvePlayerTransform();
        Require(bossSpawnSystem != null && bossSpawnPoint != null && player != null,
            "범위 스폰 사전 검증에 필요한 참조가 없습니다.");
        Require(GameObject.Find("hys_MidBoss2_SceneBoss") == null
            && bossSpawnSystem.SpawnCount == 0
            && bossSpawnSystem.WaitingForPlayer,
            "플레이어가 진입하기 전에 보스가 생성됐습니다.");

        Rigidbody2D resolvedPlayerBody = player.GetComponentInParent<Rigidbody2D>();
        Transform playerBodyRoot = resolvedPlayerBody != null
            ? resolvedPlayerBody.transform
            : (GameObject.Find("HWJ_Player")?.transform ?? player.root);
        hys_MidBoss2PlayerCircleColliderAdapter circleAdapter =
            FindFirstObjectByType<hys_MidBoss2PlayerCircleColliderAdapter>();
        circleAdapter?.RefreshPlayerCollider();
        yield return null;
        CircleCollider2D playerCircle = playerBodyRoot.GetComponent<CircleCollider2D>();
        BoxCollider2D[] playerBoxes = playerBodyRoot.GetComponents<BoxCollider2D>();
        bool hasEnabledBodyBox = false;
        for (int i = 0; i < playerBoxes.Length; i++)
            hasEnabledBodyBox |= playerBoxes[i] != null && playerBoxes[i].enabled && !playerBoxes[i].isTrigger;
        GameObject environment = GameObject.Find("hys_MidBoss2_ArenaEnvironment");
        Tilemap arenaTilemap = GameObject.Find("TEST MAP")?.GetComponent<Tilemap>();
        Require(circleAdapter != null && playerCircle != null && playerCircle.enabled
            && !playerCircle.isTrigger && !hasEnabledBodyBox,
            "플레이어 몸 콜라이더가 원형으로 교체되지 않았습니다.");
        Require(environment != null
            && environment.GetComponentsInChildren<SpriteRenderer>(true).Length == 3
            && arenaTilemap != null && arenaTilemap.GetUsedTilesCount() > 0,
            "왕실 폐허 배경 또는 충돌 타일맵이 없습니다.");
        if (failed) yield break;

        yield return WaitFor(
            () => bossSpawnSystem.ActivationTarget != null,
            2f,
            "보스 범위 생성 시스템이 실제 플레이어를 추적하지 못했습니다.");
        if (failed) yield break;

        // 실제 진입을 재현해 범위 밖에서는 숨겨지고 범위 안에서만 생성되는지 확인합니다.
        Transform activationTarget = bossSpawnSystem.ActivationTarget;
        Rigidbody2D activationBody = activationTarget.GetComponentInParent<Rigidbody2D>();
        Transform activationRoot = activationBody != null ? activationBody.transform : activationTarget;
        Vector3 entryPosition = new Vector3(
            bossSpawnPoint.Position.x - Mathf.Max(1f, bossSpawnSystem.PlayerEnterRange * 0.5f),
            activationRoot.position.y,
            activationRoot.position.z);
        HWJ_SoulSystem activationSoul = activationTarget.GetComponentInParent<HWJ_SoulSystem>();
        if (activationSoul != null && activationSoul.CurrentState != HWJ_SoulRuntimeState.Body)
            activationSoul.EnterBodyState();
        if (activationBody != null)
        {
            // 범위 안을 점프로 통과해도 공중에서 연출이 시작되지 않는지 먼저 확인합니다.
            activationBody.position = entryPosition + Vector3.up * 2.5f;
            activationBody.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.12f);
            Require(GameObject.Find("hys_MidBoss2_SceneBoss") == null
                && bossSpawnSystem.WaitingForPlayerLanding,
                "점프 중인 플레이어를 공중에 고정한 채 보스 인트로를 시작했습니다.");
            if (failed) yield break;

            activationBody.position = entryPosition;
            activationBody.linearVelocity = Vector2.zero;
            yield return new WaitForFixedUpdate();
        }
        else
        {
            activationRoot.position = entryPosition;
        }

        yield return WaitFor(
            () => GameObject.Find("hys_MidBoss2_SceneBoss") != null,
            8f,
            "씬 보스를 찾지 못했습니다.");
        if (failed) yield break;

        GameObject boss = GameObject.Find("hys_MidBoss2_SceneBoss");
        hys_SecondBossPresentation presentation = boss.GetComponent<hys_SecondBossPresentation>();
        hys_SecondBossLogic logic = boss.GetComponent<hys_SecondBossLogic>();
        hys_SecondBossPattern patterns = boss.GetComponent<hys_SecondBossPattern>();
        HWJ_RuntimeStatusSystem status = boss.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_BossDialogueBubbleSystem dialogue = boss.GetComponent<HWJ_BossDialogueBubbleSystem>();
        HWJ_BossCameraFocusSystem focus = boss.GetComponent<HWJ_BossCameraFocusSystem>();
        hys_SecondBossDialogueStyler dialogueStyler = boss.GetComponent<hys_SecondBossDialogueStyler>();
        hys_SecondBossSummonSpawner summonSpawner = boss.GetComponent<hys_SecondBossSummonSpawner>();
        hys_SecondBossPhaseTransitionVisual phaseVisual = boss.GetComponent<hys_SecondBossPhaseTransitionVisual>();
        hys_SecondBossCinematicOverlay cinematicOverlay = boss.GetComponent<hys_SecondBossCinematicOverlay>();
        CinemachineCamera cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();

        Require(presentation != null, "HYS 연출 컨트롤러가 없습니다.");
        Require(bossSpawnSystem != null && bossSpawnPoint != null,
            "HYS 보스 스폰포인트 또는 생성 시스템이 없습니다.");
        Require(bossSpawnSystem.SpawnCount == 1
            && !bossSpawnSystem.UsedExistingBoss
            && bossSpawnSystem.SpawnedBoss == boss
            && bossSpawnSystem.SpawnCommitted,
            "보스가 스폰포인트에서 정확히 한 번 생성되지 않았습니다.");
        int committedSpawnCount = bossSpawnSystem.SpawnCount;
        Require(bossSpawnSystem.TrySpawnBoss()
            && bossSpawnSystem.SpawnCount == committedSpawnCount
            && bossSpawnSystem.SpawnedBoss == boss,
            "생성 완료 후 중복 스폰 요청이 새 보스를 만들었습니다.");
        // 보스 AI가 첫 프레임부터 움직일 수 있으므로 생성 시스템이 기록한 순간 좌표를 확인합니다.
        Require(Vector3.Distance(bossSpawnSystem.LastSpawnPosition, bossSpawnPoint.Position) < 0.01f
            && Quaternion.Angle(bossSpawnSystem.LastSpawnRotation, bossSpawnPoint.Rotation) < 0.1f,
            "보스 생성 위치 또는 방향이 스폰포인트와 다릅니다.");
        Require(bossSpawnPoint.SharedSpawnPoint != null
            && bossSpawnPoint.SharedSpawnPoint.SpawnPointType == HWJ_SpawnPointType.Boss,
            "공용 스폰포인트가 Boss 타입이 아닙니다.");
        Require(logic != null && patterns != null && status != null, "보스 전투 참조가 없습니다.");
        Require(dialogue != null && focus != null, "대사 또는 카메라 포커스가 없습니다.");
        Require(dialogueStyler != null, "HYS 대사 스타일러가 없습니다.");
        Require(summonSpawner != null && summonSpawner.HasConfiguredPrefabs,
            "HYS 전용 소환 스포너가 구성되지 않았습니다.");
        Require(phaseVisual != null, "HYS 2페이즈 의식 연출기가 없습니다.");
        Require(cinematicOverlay != null, "Animator 없는 HYS 인트로 오버레이가 없습니다.");
        Require(cinemachineCamera != null, "Cinemachine Camera가 없습니다.");
        Require(player != null, "플레이어를 찾지 못했습니다.");
        if (failed) yield break;

        HWJ_SoulSystem soulSystem = player.GetComponentInParent<HWJ_SoulSystem>();
        HWJ_RuntimeStatusSystem playerStatus = player.GetComponentInParent<HWJ_RuntimeStatusSystem>();
        HWJ_CombatSystem playerCombat = player.GetComponentInParent<HWJ_CombatSystem>();
        Require(soulSystem != null, "플레이어 영혼 상태 시스템이 없습니다.");
        yield return WaitFor(
            () => presentation.HasTemporaryPlayerTestBoost,
            2f,
            "중간보스2 임시 플레이어 테스트 강화가 적용되지 않았습니다.");
        Require(playerStatus != null
            && playerStatus.MaxHp >= 900000f
            && playerStatus.CurrentHp >= 900000f
            && playerStatus.AttackPower >= 900000f,
            "임시 테스트 체력 또는 공격력이 충분히 증가하지 않았습니다.");
        Require(playerCombat != null
            && playerCombat.GetOutgoingDamage() > status.MaxHp + status.Defense,
            "플레이어의 임시 공격력이 중간보스2를 한 번에 처치할 수 없습니다.");
        if (failed) yield break;

        // 실제 영혼 전환 API로 인트로가 시작되지 않는지 먼저 검증합니다.
        soulSystem.EnterSoulState();
        yield return new WaitForSeconds(0.35f);
        Require(soulSystem.CurrentState != HWJ_SoulRuntimeState.Body,
            "유령 상태 검증을 시작하지 못했습니다.");
        Require(presentation.WaitingForPlayerBody && presentation.IntroPlayCount == 0,
            "유령 상태에서 인트로가 대기하지 않았습니다.");
        Require(!focus.IsFocusing
            && dialogue.ActiveSequenceType == HWJ_BossDialogueSequenceType.None,
            "유령 상태에서 대사 또는 Cinemachine 포커스가 시작됐습니다.");

        soulSystem.EnterBodyState();
        yield return null;

        status.ResetForEncounter(true);
        yield return null;
        Require(status.MaxHp > 0f && status.CurrentHp > 0f, "보스 HP가 초기화되지 않았습니다.");

        yield return WaitFor(
            () => presentation.IntroPlayCount == 1
                && presentation.IsPresentationBlocking
                && presentation.InteractiveIntroPlaying
                && presentation.IsPlayerControlLocked,
            5f,
            "대사 넘김 인트로와 플레이어 조작 잠금이 시작되지 않았습니다.");
        if (failed) yield break;
        Require(!logic.enabled, "인트로 중 보스 AI가 활성화되어 있습니다.");
        Require(focus.IsFocusing && focus.UsesCinemachineDuringFocus,
            "인트로가 Cinemachine 포커스를 사용하지 않습니다.");
        hys_PlayerCinematicControlLock playerLock =
            player.GetComponentInParent<hys_PlayerCinematicControlLock>();
        hys_Player_Animator playerAnimator =
            player.GetComponentInParent<hys_Player_Animator>();
        if (playerAnimator == null)
        {
            // Animator가 플레이어 비주얼 자식에 붙은 프리팹도 연출 고정 상태를 빠짐없이 검사합니다.
            playerAnimator = player.GetComponentInChildren<hys_Player_Animator>();
        }
        Rigidbody2D playerRigidBody = player.GetComponentInParent<Rigidbody2D>();
        Transform visiblePlayerRoot = playerRigidBody != null ? playerRigidBody.transform : player;
        Require(playerLock != null && playerLock.IsLocked
            && hys_PlayerCinematicControlLock.IsLockedFor(player),
            "인트로 중 HYS 플레이어 조작 잠금이 유지되지 않습니다.");
        Require(playerRigidBody == null || playerLock != null && playerLock.IsPhysicsFrozen
            && !playerRigidBody.simulated
            && playerRigidBody.linearVelocity.sqrMagnitude < 0.0001f,
            "인트로 중 플레이어 물리가 완전히 고정되지 않았습니다.");
        Require(playerLock != null && playerLock.AreVisualAnimatorsFrozen,
            "인트로 중 실제 플레이어 자식 Animator 또는 HWJ 비주얼이 고정되지 않았습니다.");
        Require(playerAnimator == null || playerAnimator.IsCinematicIdleLocked,
            "인트로 중 플레이어 이동·점프 애니메이션이 Idle 프레임에 고정되지 않았습니다.");
        Require(presentation.IntroDropCompleted
            && presentation.IntroLandingCount == 1
            && presentation.IntroLandingSparkCount >= 6
            && Mathf.Abs(visiblePlayerRoot.position.x - boss.transform.position.x)
                <= bossSpawnSystem.PlayerEnterRange + 0.35f
            && Mathf.Abs(visiblePlayerRoot.position.x - entryPosition.x) <= 0.35f,
            "착지 대기 또는 보스 낙하·불꽃 연출이 완료되지 않았습니다.");
        Require(bossSpawnSystem.PlayerEnterRange <= 4.5f,
            "보스 인트로 진입 정지 지점이 화면 밖 거리로 설정됐습니다.");
        Require(cinematicOverlay.IsVisible && cinematicOverlay.HasRoyalSeal,
            "인트로 오버레이 또는 플레이어 전기 인장이 표시되지 않았습니다.");
        Require(cinematicOverlay.RoyalSealRingCount >= 5
            && cinematicOverlay.RoyalSealLightningCount >= 5,
            "강화된 다중 회전 인장 또는 전기 아크가 생성되지 않았습니다.");
        Require(presentation.IsBossVisualScaleApplied
            && presentation.BossVisualScaleMultiplier >= 1.2f,
            "중간보스2 비주얼 크기 확대가 적용되지 않았습니다.");
        Require(!cinematicOverlay.HasBottomControls,
            "삭제한 화면 하단 진행바 또는 조작 안내가 다시 생성됐습니다.");
        Require(dialogue.GetSequenceLineCount(HWJ_BossDialogueSequenceType.Intro) >= 12
            && dialogue.GetSequenceLineCount(HWJ_BossDialogueSequenceType.PhaseTransition) >= 12,
            "1·2페이즈 컨셉 대사가 충분히 확장되지 않았습니다.");
        yield return WaitFor(
            () => dialogueStyler.HasStyledBubble,
            2f,
            "HYS 기사단장 대사창 스타일이 적용되지 않았습니다.");
        if (failed) yield break;
        Require(dialogueStyler.DialogueCharacterSize >= 20f
            && dialogueStyler.DialogueCharacterSize <= 64f,
            "시네마신 대사 글자 크기가 패널 폭에 맞지 않습니다.");
        yield return null;
        Require(dialogueStyler.IsDialogueInsidePanel,
            "대사 글씨가 기사단장 패널 밖으로 튀어나왔습니다.");
        Require(dialogueStyler.PanelColor.r < 0.15f && dialogueStyler.PanelColor.a > 0.85f,
            "대사창이 어두운 기사단장 테마로 변경되지 않았습니다.");

        int firstLineIndex = presentation.CurrentInteractiveLineIndex;
        presentation.RequestDialogueAdvance();
        yield return WaitFor(
            () => presentation.DialogueAdvanceCount >= 1
                && presentation.CurrentInteractiveLineIndex != firstLineIndex,
            3f,
            "다음 대사 입력으로 인트로 줄이 넘어가지 않았습니다.");
        presentation.RequestIntroSkip();
        yield return WaitFor(
            () => presentation.IsCombatStartGraceActive,
            5f,
            "인트로 스킵 뒤 전투 시작 안전시간이 시작되지 않았습니다.");
        if (failed) yield break;
        Require(presentation.IsPresentationBlocking
            && !presentation.IsPlayerControlLocked
            && !logic.enabled,
            "안전시간 동안 플레이어가 먼저 풀리거나 보스 AI가 대기하지 않았습니다.");
        yield return WaitFor(
            () => !presentation.IsPresentationBlocking,
            3f,
            "0.7초 전투 시작 안전시간 후 보스 AI가 해제되지 않았습니다.");
        yield return WaitFor(() => !focus.IsFocusing, 3f, "인트로 후 카메라가 복구되지 않았습니다.");
        if (failed) yield break;
        Require(logic.enabled && presentation.IntroPlayCount == 1, "인트로 종료 후 AI가 복구되지 않았습니다.");
        Require(!presentation.IsPlayerControlLocked
            && (playerLock == null || !playerLock.IsLocked)
            && (playerAnimator == null || !playerAnimator.IsCinematicIdleLocked)
            && (playerRigidBody == null || playerRigidBody.simulated)
            && !cinematicOverlay.IsVisible
            && presentation.IntroSkipCount == 1,
            "인트로 스킵 후 플레이어 잠금 또는 오버레이가 남았습니다.");
        Require(cinemachineCamera.Target.TrackingTarget == player,
            "인트로 후 Cinemachine 추적 대상이 플레이어로 돌아오지 않았습니다.");

        Require(hys_MidBoss2EncounterSession.HasSeenIntro(presentation),
            "인트로 재생 완료 상태가 재입장 세션에 기록되지 않았습니다.");
        GameObject reentryProbe = new GameObject("hys_MidBoss2_ReentryPresentationProbe");
        hys_SecondBossPresentation reentryPresentation =
            reentryProbe.AddComponent<hys_SecondBossPresentation>();
        yield return null;
        yield return null;
        Require(reentryPresentation.IntroSuppressedByReentry
            && reentryPresentation.IntroPlayed
            && reentryPresentation.IntroPlayCount == 0
            && !reentryPresentation.IsPresentationBlocking,
            "같은 실행 중 재입장 시 이미 본 인트로가 다시 시작됐습니다.");
        Destroy(reentryProbe);
        yield return null;

        int hitCountBefore = presentation.HitFlashCount;
        status.SetCurrentHpForDebug(status.MaxHp * 0.75f);
        yield return null;
        Require(presentation.HitFlashCount == hitCountBefore + 1, "피격 붉은 점멸이 실행되지 않았습니다.");
        Require(presentation.ImpulseCount > 0, "피격 Cinemachine Impulse가 실행되지 않았습니다.");
        yield return WaitFor(
            () => patterns.DashPathTelegraphCount >= 1,
            4f,
            "물리 돌진 패턴의 방향선 전조가 생성되지 않았습니다.");
        if (failed) yield break;

        status.SetCurrentHpForDebug(status.MaxHp * 0.49f);
        yield return WaitFor(
            () => presentation.PhaseTransitionPlayCount == 1
                && presentation.IsPresentationBlocking
                && presentation.InteractiveDialoguePlaying
                && presentation.ActiveInteractiveSequence
                    == HWJ_BossDialogueSequenceType.PhaseTransition,
            4f,
            "HP 50%의 2페이즈 전환이 시작되지 않았습니다.");
        if (failed) yield break;
        float protectedHp = status.CurrentHp;
        status.ApplyDamage(status.MaxHp * 0.2f, this, null);
        yield return null;
        Require(Mathf.Approximately(status.CurrentHp, protectedHp), "2페이즈 전환 중 피해가 차단되지 않았습니다.");
        Require(!logic.enabled, "2페이즈 전환 중 보스 AI가 활성화되어 있습니다.");
        Require(presentation.IsPlayerControlLocked,
            "2페이즈 연출 중 플레이어 조작이 잠기지 않았습니다.");
        Require(presentation.PhaseVisualBurstCount >= 1,
            "2페이즈 마력진과 검 포위 연출이 시작되지 않았습니다.");
        Require(phaseVisual.IsPlaying && phaseVisual.PlayCount == 1,
            "2페이즈 룬 의식 연출이 시작되지 않았습니다.");

        int phaseLineIndex = presentation.CurrentInteractiveLineIndex;
        int phaseAdvanceCount = presentation.DialogueAdvanceCount;
        presentation.RequestDialogueAdvance();
        yield return WaitFor(
            () => presentation.DialogueAdvanceCount > phaseAdvanceCount
                && presentation.CurrentInteractiveLineIndex != phaseLineIndex,
            3f,
            "다음 대사 입력으로 2페이즈 대사가 넘어가지 않았습니다.");
        presentation.RequestDialogueSkip();

        yield return WaitFor(
            () => !presentation.IsPresentationBlocking,
            6f,
            "2페이즈 전환이 종료되지 않았습니다.");
        yield return WaitFor(() => !focus.IsFocusing, 3f, "2페이즈 후 카메라가 복구되지 않았습니다.");
        if (failed) yield break;
        Require(presentation.PhaseVisualBurstCount >= 5,
            "2페이즈 4단 충격파 연출이 끝까지 실행되지 않았습니다.");
        Require(phaseVisual.LastStageCount >= 5 && !phaseVisual.IsPlaying,
            "2페이즈 룬·기둥·최종 섬광 연출이 끝까지 실행되지 않았습니다.");
        Require(!presentation.IsPlayerControlLocked,
            "2페이즈 연출 종료 후 플레이어 조작 잠금이 남았습니다.");
        Require(!presentation.InteractiveDialoguePlaying
            && !cinematicOverlay.IsVisible
            && presentation.DialogueSkipCount >= 2,
            "2페이즈 전체 스킵 후 대사 UI 또는 입력 상태가 남았습니다.");
        status.SetCurrentHpForDebug(status.MaxHp * 0.4f);
        yield return new WaitForSeconds(0.4f);
        Require(presentation.PhaseTransitionPlayCount == 1, "2페이즈 전환이 중복 실행됐습니다.");

        Require(playerStatus != null, "플레이어 상태 시스템이 없습니다.");
        if (failed) yield break;

        MethodInfo setCombatBlocked = typeof(hys_SecondBossPresentation).GetMethod(
            "SetCombatBlocked",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo acquirePlayerControl = typeof(hys_SecondBossPresentation).GetMethod(
            "AcquirePlayerControl",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(setCombatBlocked != null && acquirePlayerControl != null,
            "연출 중단 복구 검증 API를 찾지 못했습니다.");
        if (failed) yield break;

        // 플레이어가 연출 도중 사망한 상황을 만들어 카메라·입력·UI가 즉시 복구되는지 확인합니다.
        int playerDeathAbortCount = presentation.PlayerDeathAbortCount;
        setCombatBlocked.Invoke(presentation, new object[] { true });
        acquirePlayerControl.Invoke(presentation, new object[] { player });
        cinematicOverlay.BeginDialogueControls("중단 복구 검증", Color.red);
        focus.FocusOnBoss(boss.transform, player, 5f);
        playerStatus.SetState(HWJ_RuntimeState.Dead);
        yield return WaitFor(
            () => presentation.PlayerDeathAbortCount > playerDeathAbortCount,
            2f,
            "연출 도중 플레이어 사망을 감지하지 못했습니다.");
        if (failed) yield break;
        Require(!presentation.IsPresentationBlocking
            && !presentation.IsPlayerControlLocked
            && !focus.IsFocusing
            && !cinematicOverlay.IsVisible,
            "플레이어 사망 후 카메라·조작·대사 UI가 복구되지 않았습니다.");
        playerStatus.ResetForEncounter(true);
        yield return null;

        // 씬 전환과 동일한 OnDisable 중단에서도 모든 임시 상태가 남지 않아야 합니다.
        setCombatBlocked.Invoke(presentation, new object[] { true });
        acquirePlayerControl.Invoke(presentation, new object[] { player });
        cinematicOverlay.BeginDialogueControls("씬 이동 복구 검증", Color.yellow);
        focus.FocusOnBoss(boss.transform, player, 5f);
        presentation.enabled = false;
        yield return null;
        Require(!presentation.IsPresentationBlocking
            && !presentation.IsPlayerControlLocked
            && !focus.IsFocusing
            && !cinematicOverlay.IsVisible,
            "씬 이동 중단 시 카메라·조작·대사 UI가 복구되지 않았습니다.");
        presentation.enabled = true;
        yield return null;

        List<GameObject> spawnedByDedicatedSpawner = null;
        yield return summonSpawner.SpawnFormationRoutine(
            3,
            spawned => spawnedByDedicatedSpawner = spawned);
        Require(patterns.SummonSpawner == summonSpawner,
            "보스 패턴이 전용 소환 스포너를 사용하지 않습니다.");
        Require(summonSpawner.SpawnSequenceCount >= 1
            && summonSpawner.LastRequestedCount == 3
            && summonSpawner.LastSpawnedCount == 3
            && summonSpawner.LastActivatedAiCount == 3
            && spawnedByDedicatedSpawner != null
            && spawnedByDedicatedSpawner.Count == 3,
            "전용 소환 스포너가 3마리 진형을 정상 생성하지 못했습니다.");
        if (spawnedByDedicatedSpawner != null)
        {
            Vector3[] summonedStartPositions = new Vector3[spawnedByDedicatedSpawner.Count];
            for (int i = 0; i < spawnedByDedicatedSpawner.Count; i++)
            {
                GameObject summoned = spawnedByDedicatedSpawner[i];
                Require(summoned != null, "생성 직후 소환 몬스터가 사라졌습니다.");
                if (summoned == null) continue;
                summonedStartPositions[i] = summoned.transform.position;
                HWJ_MonsterAISystem summonedAi =
                    summoned.GetComponentInChildren<HWJ_MonsterAISystem>(true);
                Require(summonedAi != null && summonedAi.enabled && summonedAi.Target == player,
                    "소환 몬스터 AI에 플레이어 타깃이 전달되지 않았습니다.");
            }

            yield return new WaitForSeconds(1.2f);
            bool anySummonMoved = false;
            for (int i = 0; i < spawnedByDedicatedSpawner.Count; i++)
            {
                GameObject summoned = spawnedByDedicatedSpawner[i];
                if (summoned == null) continue;
                anySummonMoved |= Vector3.Distance(
                    summonedStartPositions[i],
                    summoned.transform.position) > 0.05f;
            }
            Require(anySummonMoved, "소환 몬스터가 플레이어를 향해 움직이지 않았습니다.");

            for (int i = 0; i < spawnedByDedicatedSpawner.Count; i++)
                if (spawnedByDedicatedSpawner[i] != null) Destroy(spawnedByDedicatedSpawner[i]);
        }

        logic.StopEncounter();
        logic.enabled = false;
        patterns.CancelActivePattern();
        int strongImpactCountBefore = presentation.ImpulseCount;
        bool warningPatternStarted = patterns.TryStartPattern(
            hys_SecondBossPatternId.GroundSwordEruption,
            player,
            1f,
            null);
        yield return null;
        Require(warningPatternStarted, "바닥 경고 검증용 패턴을 시작하지 못했습니다.");
        Require(FindFirstObjectByType<hys_SkillWarningIndicator>() != null,
            "공격 직전 바닥 경고가 생성되지 않았습니다.");
        hys_SecondBossAttackTelegraph[] telegraphs =
            FindObjectsByType<hys_SecondBossAttackTelegraph>(FindObjectsSortMode.None);
        bool foundEruptionCountdown = false;
        for (int i = 0; i < telegraphs.Length; i++)
        {
            foundEruptionCountdown |= telegraphs[i] != null
                && telegraphs[i].Style == hys_SecondBossTelegraphStyle.SwordEruption
                && telegraphs[i].HasCountdown;
        }
        Require(foundEruptionCountdown
            && patterns.TelegraphRequestCount > 0
            && patterns.CountdownTelegraphCount > 0,
            "검 솟구치기의 붉은 바닥 문양 또는 숫자 카운트다운이 없습니다.");
        yield return WaitFor(
            () => presentation.ImpulseCount > strongImpactCountBefore,
            5f,
            "강공격 순간 Cinemachine Impulse가 실행되지 않았습니다.");
        if (failed) yield break;
        patterns.CancelActivePattern();

        playerStatus.ResetForEncounter(true);
        playerStatus.SetState(HWJ_RuntimeState.Dead);
        yield return null;
        Require(presentation.PlayerDefeatLineShown, "플레이어 처치 대사가 실행되지 않았습니다.");
        playerStatus.ResetForEncounter(true);

        status.SetCurrentHpForDebug(Mathf.Max(1f, status.MaxHp * 0.05f));
        status.ApplyDamage(status.MaxHp, this, null);
        yield return WaitFor(
            () => presentation.DeathPlayCount == 1
                && presentation.InteractiveDialoguePlaying
                && presentation.ActiveInteractiveSequence == HWJ_BossDialogueSequenceType.Death,
            4f,
            "보스 사망 대사와 카메라 연출이 시작되지 않았습니다.");
        if (failed) yield break;
        Require(focus.IsFocusing && focus.UsesCinemachineDuringFocus,
            "사망 연출이 Cinemachine 포커스를 사용하지 않습니다.");
        int deathLineIndex = presentation.CurrentInteractiveLineIndex;
        int deathAdvanceCount = presentation.DialogueAdvanceCount;
        presentation.RequestDialogueAdvance();
        yield return WaitFor(
            () => presentation.DialogueAdvanceCount > deathAdvanceCount
                && presentation.CurrentInteractiveLineIndex != deathLineIndex,
            3f,
            "다음 대사 입력으로 사망 대사가 넘어가지 않았습니다.");
        presentation.RequestDialogueSkip();
        yield return WaitFor(
            () => !presentation.InteractiveDialoguePlaying
                && !presentation.IsPlayerControlLocked
                && !focus.IsFocusing
                && !cinematicOverlay.IsVisible,
            5f,
            "사망 대사 전체 스킵 후 카메라·조작·UI가 복구되지 않았습니다.");
        if (failed) yield break;
        Require(presentation.DialogueSkipCount >= 3,
            "인트로·2페이즈·사망 대사의 전체 스킵 입력이 통일되지 않았습니다.");

        TextMesh bossName = FindTextByName(boss, "BossName");
        Require(bossName != null && bossName.text == presentation.BossDisplayName,
            "체력바 보스 이름이 표시되지 않았습니다.");
        if (failed) yield break;
        Require(bossSpawnSystem.SpawnCount == 1,
            "검증 중 보스가 중복 생성됐습니다.");
        yield return null;
        Require(bossSpawnSystem.EncounterCompleted
            && hys_MidBoss2EncounterSession.IsDefeated(bossSpawnSystem)
            && !bossSpawnSystem.TrySpawnBoss()
            && bossSpawnSystem.SpawnCount == 1,
            "보스 처치 후 재입장·재소환 차단 상태가 유지되지 않았습니다.");

        Debug.Log("[hys MidBoss2 PlayMode] PASS - 공격별 전조·돌진 방향선·카운트다운, 전 대사 조작, 0.7초 안전시간, 재입장·중복 소환 차단을 확인했습니다.");
        EditorApplication.Exit(0);
    }

    private IEnumerator WaitFor(Func<bool> condition, float timeoutSeconds, string failureMessage)
    {
        float endTime = Time.realtimeSinceStartup + timeoutSeconds;
        while (!condition() && Time.realtimeSinceStartup < endTime) yield return null;
        if (!condition()) Fail(failureMessage);
    }

    private void Require(bool condition, string message)
    {
        if (!condition) Fail(message);
    }

    private void Fail(string message)
    {
        if (failed) return;
        failed = true;
        Debug.LogError("[hys MidBoss2 PlayMode] FAIL - " + message);
        EditorApplication.Exit(1);
    }

    private static Transform ResolvePlayerTransform()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
            return HWJ_GameAccess.Manager.PlayerResolver.transform;

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
                return resolvers[i].transform;
        }
        return null;
    }

    private static TextMesh FindTextByName(GameObject root, string objectName)
    {
        TextMesh[] texts = root.GetComponentsInChildren<TextMesh>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == objectName) return texts[i];
        }
        return null;
    }
}
#endif
