using System;
using UnityEngine;

[Serializable]
public class HWJ_StatusData
{
    public float maxHp;
    public float moveSpeed;
    public float attackPower;
    public float defense;
}

[Serializable]
public class HWJ_ModelData
{
    public GameObject modelPrefab;
    public RuntimeAnimatorController animatorController;
    public string heartEffectSocketName;
}

[Serializable]
public class HWJ_DamageData
{
    public HWJ_DamageType damageType;
    public float baseDamage;
    public float criticalChance;
    public float criticalMultiplier = 1.5f;
}

[Serializable]
public class HWJ_ReceivedDamageData
{
    public float damageMultiplier = 1f;
    public float invincibleSecondsAfterHit;
    public bool isInvincible;
}
