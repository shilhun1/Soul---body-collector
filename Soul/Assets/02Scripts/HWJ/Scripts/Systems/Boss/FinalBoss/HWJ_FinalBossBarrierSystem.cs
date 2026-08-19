using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 최종보스의 1·2페이즈 원형 방어막 내구도를 런타임에서 관리합니다.
/// 방어막이 켜진 동안 받은 공격은 보스 HP에 전달되지 않습니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class HWJ_FinalBossBarrierSystem : MonoBehaviour, HWJ_IDamageAbsorber
{
    [Header("연결")]
    [Tooltip("방어막 체력 비율을 계산할 보스 런타임 상태입니다.")]
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;

    [Tooltip("방어막 시각 프리팹을 붙일 위치입니다. 비어 있으면 보스 루트를 사용합니다.")]
    [SerializeField] private Transform visualRoot;

    [Header("런타임 상태")]
    [SerializeField] private bool barrierActive;
    [SerializeField] private float currentBarrierHp;
    [SerializeField] private float maximumBarrierHp;

    private Coroutine durationRoutine;
    private GameObject activeVisual;

    public event Action<float, float, bool> BarrierChanged;

    public bool IsBarrierActive => barrierActive;
    public float CurrentBarrierHp => currentBarrierHp;
    public float MaximumBarrierHp => maximumBarrierHp;
    public float BarrierRatio => maximumBarrierHp > 0f
        ? Mathf.Clamp01(currentBarrierHp / maximumBarrierHp)
        : 0f;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnDisable()
    {
        DeactivateBarrier();
    }

    public bool ActivateBarrier(
        float bossMaximumHpRatio,
        float durationSeconds,
        GameObject visualPrefab)
    {
        ResolveReferences();

        if (runtimeStatus == null || runtimeStatus.MaxHp <= 0f)
        {
            return false;
        }

        DeactivateBarrier();
        maximumBarrierHp = runtimeStatus.MaxHp * Mathf.Clamp01(bossMaximumHpRatio);
        currentBarrierHp = maximumBarrierHp;
        barrierActive = maximumBarrierHp > 0f;

        if (!barrierActive)
        {
            return false;
        }

        activeVisual = HWJ_FinalBossRuntimeVisualFactory.Create(
            visualPrefab,
            "HWJ_FinalBoss_ActiveBarrier",
            transform.position,
            visualRoot != null ? visualRoot : transform,
            new Vector2(3.2f, 3.2f),
            new Color(0.55f, 0.2f, 0.9f, 0.45f),
            25);

        if (durationSeconds > 0f)
        {
            durationRoutine = StartCoroutine(DeactivateAfter(durationSeconds));
        }

        RaiseChanged();
        return true;
    }

    public bool TryAbsorbDamage(
        float incomingDamage,
        Component source,
        HWJ_DamageData sourceDamage,
        out float remainingDamage)
    {
        remainingDamage = Mathf.Max(0f, incomingDamage);

        if (!barrierActive || remainingDamage <= 0f)
        {
            return false;
        }

        currentBarrierHp = Mathf.Max(0f, currentBarrierHp - remainingDamage);

        // 방어막을 파괴한 한 번의 공격은 남는 피해까지 모두 막습니다.
        remainingDamage = 0f;

        if (currentBarrierHp <= 0f)
        {
            DeactivateBarrier();
        }
        else
        {
            RaiseChanged();
        }

        return true;
    }

    public void DeactivateBarrier()
    {
        if (durationRoutine != null)
        {
            StopCoroutine(durationRoutine);
            durationRoutine = null;
        }

        bool changed = barrierActive || activeVisual != null;
        barrierActive = false;
        currentBarrierHp = 0f;
        maximumBarrierHp = 0f;

        if (activeVisual != null)
        {
            Destroy(activeVisual);
            activeVisual = null;
        }

        if (changed)
        {
            RaiseChanged();
        }
    }

    private IEnumerator DeactivateAfter(float seconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, seconds));
        durationRoutine = null;
        DeactivateBarrier();
    }

    private void ResolveReferences()
    {
        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }
    }

    private void RaiseChanged()
    {
        BarrierChanged?.Invoke(currentBarrierHp, maximumBarrierHp, barrierActive);
    }
}
