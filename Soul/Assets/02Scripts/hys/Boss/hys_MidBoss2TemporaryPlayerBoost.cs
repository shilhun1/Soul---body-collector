using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

// 중간보스2 테스트 씬에서만 플레이어 체력과 공격력을 크게 올리고 씬을 나가면 원복합니다.
[DisallowMultipleComponent]
public class hys_MidBoss2TemporaryPlayerBoost : MonoBehaviour
{
    [SerializeField] private HWJ_RuntimeStatusSystem playerStatus;
    [SerializeField] private float maxHpBonus = 1000000f;
    [SerializeField] private float attackPowerBonus = 1000000f;
    [SerializeField] private string ownerSceneKey;
    [SerializeField] private bool initialized;
    [SerializeField] private bool applied;

    private HWJ_StatOrbDataSO hpOrb;
    private HWJ_StatOrbDataSO attackOrb;

    public bool IsApplied => applied;
    public float MaxHpBonus => maxHpBonus;
    public float AttackPowerBonus => attackPowerBonus;
    public HWJ_RuntimeStatusSystem PlayerStatus => playerStatus;

    public void Initialize(
        HWJ_RuntimeStatusSystem status,
        float hpBonus,
        float damageBonus,
        string sceneKey)
    {
        if (applied) RemoveBoost();
        playerStatus = status;
        maxHpBonus = Mathf.Max(0f, hpBonus);
        attackPowerBonus = Mathf.Max(0f, damageBonus);
        ownerSceneKey = sceneKey;
        initialized = playerStatus != null;
        ApplyBoost();
    }

    private void OnEnable()
    {
        if (initialized && !applied) ApplyBoost();
    }

    private void Update()
    {
        if (!initialized || string.IsNullOrWhiteSpace(ownerSceneKey)) return;
        if (string.Equals(GetActiveSceneKey(), ownerSceneKey, System.StringComparison.Ordinal)) return;

        // 플레이어가 DontDestroyOnLoad여도 다른 씬까지 테스트 수치가 남지 않게 즉시 제거합니다.
        RemoveBoost();
        Destroy(this);
    }

    private void OnDisable()
    {
        RemoveBoost();
    }

    private void OnDestroy()
    {
        RemoveBoost();
        DestroyRuntimeOrb(ref hpOrb);
        DestroyRuntimeOrb(ref attackOrb);
    }

    private void ApplyBoost()
    {
        if (applied || playerStatus == null) return;
        hpOrb = CreateRuntimeOrb(HWJ_StatOrbType.MaxHp, maxHpBonus);
        attackOrb = CreateRuntimeOrb(HWJ_StatOrbType.AttackPower, attackPowerBonus);
        playerStatus.ApplyStatOrbBonus(hpOrb, false);
        playerStatus.ApplyStatOrbBonus(attackOrb, true);
        playerStatus.SetCurrentHpForDebug(playerStatus.MaxHp);
        applied = true;
        Debug.Log(
            $"[hys MidBoss2 Test] 임시 플레이어 강화 적용 - HP {playerStatus.MaxHp:0}, 공격력 {playerStatus.AttackPower:0}",
            this);
    }

    private void RemoveBoost()
    {
        if (!applied || playerStatus == null) return;
        HWJ_StatOrbDataSO hpRollback = CreateRuntimeOrb(HWJ_StatOrbType.MaxHp, -maxHpBonus);
        HWJ_StatOrbDataSO attackRollback = CreateRuntimeOrb(
            HWJ_StatOrbType.AttackPower,
            -attackPowerBonus);
        playerStatus.ApplyStatOrbBonus(hpRollback, true);
        playerStatus.ApplyStatOrbBonus(attackRollback, true);
        DestroyRuntimeOrb(ref hpRollback);
        DestroyRuntimeOrb(ref attackRollback);
        applied = false;
    }

    private static HWJ_StatOrbDataSO CreateRuntimeOrb(HWJ_StatOrbType type, float amount)
    {
        HWJ_StatOrbDataSO orb = ScriptableObject.CreateInstance<HWJ_StatOrbDataSO>();
        orb.hideFlags = HideFlags.HideAndDontSave;
        string json = "{\"orbType\":" + (int)type
            + ",\"amount\":" + amount.ToString(CultureInfo.InvariantCulture) + "}";
        JsonUtility.FromJsonOverwrite(json, orb);
        return orb;
    }

    private static string GetActiveSceneKey()
    {
        Scene scene = SceneManager.GetActiveScene();
        return !string.IsNullOrWhiteSpace(scene.path) ? scene.path : scene.name;
    }

    private static void DestroyRuntimeOrb(ref HWJ_StatOrbDataSO orb)
    {
        if (orb == null) return;
        Destroy(orb);
        orb = null;
    }
}
