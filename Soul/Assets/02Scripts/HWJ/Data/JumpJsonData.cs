using UnityEngine;
using System;

[Serializable]
public class JumpJsonData
{
    public string id; // 점프 데이터 고유 ID
    public float jumpPower; // 점프 힘
    public int maxJumpCount; // 최대 점프 횟수 1이면 한번, 2면 더블 점프
    public float coyoteTime; // 발판에서 떨어진 직후에도 점프 가능한 짧은 시간
    public float jumpBufferTime; // 점프 키를 일찍 눌러도 착지 후 점프되게 하는 시간
    public float fallGravityMultiplier; // 낙하 중력을 강하게 적용
}
