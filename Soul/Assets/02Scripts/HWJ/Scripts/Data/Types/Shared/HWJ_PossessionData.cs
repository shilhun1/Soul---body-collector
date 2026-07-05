using System;
using UnityEngine;

/// <summary>
/// 빙의하는 쪽과 빙의당하는 몸이 공통으로 사용하는 데이터입니다.
/// PlayerTypeDataSO에서는 canPossess를, Enemy/Boss TypeData에서는 canBePossessed와 심장 이펙트 정보를 주로 사용합니다.
/// </summary>
[Serializable]
public class HWJ_PossessionData
{
    public bool canPossess;
    public bool canBePossessed;
    public GameObject possessableHeartEffectPrefab;
    public string heartSocketName;
    public float possessionRange;
    public bool requiresDefeatedState = true;
    public bool transfersControlToBody = true;
    public bool loadsBodyStatsToPlayer = true;
}
