using System;

[Serializable]
public class HWJ_TrackingData
{
    public float trackingRange;
    public float loseTargetRange;
}

[Serializable]
public class HWJ_AIData
{
    public string aiProfileId;
    public float decisionIntervalSeconds;
}

[Serializable]
public class HWJ_NavigationData
{
    public float stoppingDistance;
    public float pathRefreshSeconds;
}
