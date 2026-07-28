using UnityEngine;

/// <summary>
/// 체력바와 빙의체 정신력/영혼 상태 표시 규칙을 관리하는 ScriptableObject입니다.
/// HealthBarSystem이 현재 상태에 맞는 색상과 표시 방식을 읽습니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_HealthBarData", menuName = "HWJ/Data/System/Health Bar")]
public class HWJ_HealthBarDataSO : ScriptableObject
{
    [Header("체력바")]
    [Tooltip("체력바 설정을 구분하는 고정 ID입니다.")]
    [InspectorName("체력바 ID")]
    [SerializeField] private string healthBarId;
    [Tooltip("육신 상태에서 사용할 체력바 색상입니다.")]
    [InspectorName("육신 상태 색상")]
    [SerializeField] private Color bodyStateColor = Color.red;
    [Tooltip("영혼 상태에서 사용할 체력바 색상입니다.")]
    [InspectorName("영혼 상태 색상")]
    [SerializeField] private Color soulStateColor = Color.cyan;
    [Tooltip("빙의 상태에서 사용할 체력바 색상입니다.")]
    [InspectorName("빙의 상태 색상")]
    [SerializeField] private Color possessedStateColor = new Color(0.6f, 0.2f, 1f);
    [Tooltip("켜면 사망 상태에서 체력바를 숨깁니다.")]
    [InspectorName("사망 시 숨김")]
    [SerializeField] private bool hideWhenDead = true;
    [Tooltip("켜면 영혼 상태 제한 시간을 표시합니다.")]
    [InspectorName("영혼 타이머 표시")]
    [SerializeField] private bool showSoulTimer = true;

    public string HealthBarId => healthBarId;
    public Color BodyStateColor => bodyStateColor;
    public Color SoulStateColor => soulStateColor;
    public Color PossessedStateColor => possessedStateColor;
    public bool HideWhenDead => hideWhenDead;
    public bool ShowSoulTimer => showSoulTimer;
}
