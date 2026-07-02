using System;

[Serializable]
public class HWJ_ControlData
{
    public float acceleration;
    public float deceleration;
    public float jumpPower;
    public bool canFreeFly;
}

[Serializable]
public class HWJ_SoulStateData
{
    public float possessionDeadlineSeconds = 10f;
    public bool isInvincible = true;
    public bool canPassThroughWalls = true;
    public bool canFreeFly = true;
}

[Serializable]
public class HWJ_BodyDecayData
{
    public float maxDecayValue;
    public float decayTickSeconds = 0.5f;
    public float decayAmountPerTick;
    public float hitDecayPenalty;
}
