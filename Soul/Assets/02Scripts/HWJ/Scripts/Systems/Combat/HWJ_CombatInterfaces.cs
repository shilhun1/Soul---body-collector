using UnityEngine;

public interface HWJ_ICombatActor
{
    HWJ_RootObjectDataResolver DataResolver { get; }
    HWJ_RuntimeObjectContext RuntimeContext { get; }
    HWJ_RuntimeStatusSystem RuntimeStatus { get; }
    HWJ_ObjectType ObjectType { get; }
    HWJ_Faction Faction { get; }
    float Defense { get; }
    bool IsCombatDead { get; }
}

public interface HWJ_IDamageDealer
{
    float GetOutgoingDamage();
    float GetOutgoingDamage(float damageMultiplier);
    bool CanDealDamageTo(HWJ_RootObjectDataResolver targetResolver);
    bool TryDealDamageTo(HWJ_RootObjectDataResolver targetResolver, out float finalDamage);
    bool TryDealDamageTo(HWJ_RootObjectDataResolver targetResolver, float damageMultiplier, out float finalDamage);
}

public interface HWJ_IDamageReceiver
{
    bool CanReceiveDamageFrom(Component source);
    float GetDefense();
    float GetReceivedDamage(float incomingDamage);
    float GetReceivedDamage(float incomingDamage, HWJ_ObjectType attackerType);
    float GetReceivedDamage(float incomingDamage, HWJ_ObjectType attackerType, HWJ_DamageData sourceDamage);
    void ReceiveDamage(float damage, Component source, HWJ_DamageData sourceDamage);
}

/// <summary>
/// HP에 적용되기 전에 피해를 흡수하는 런타임 보호막 계약입니다.
/// SO 원본을 수정하지 않고 보호막 컴포넌트가 현재 내구도를 관리합니다.
/// </summary>
public interface HWJ_IDamageAbsorber
{
    bool TryAbsorbDamage(
        float incomingDamage,
        Component source,
        HWJ_DamageData sourceDamage,
        out float remainingDamage);
}
