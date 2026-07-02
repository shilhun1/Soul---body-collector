using System;

[Serializable]
public class HWJ_SkillEntryData
{
    public string skillId;
    public float cooldownSeconds;
    public float useIntervalSeconds;
}

[Serializable]
public class HWJ_SkillSetData
{
    public HWJ_SkillEntryData[] skills;
}
