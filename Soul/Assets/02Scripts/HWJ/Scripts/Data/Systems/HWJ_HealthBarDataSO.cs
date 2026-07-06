using UnityEngine;

/// <summary>
/// 체력바와 부패/영혼 상태 표시 규칙을 관리하는 ScriptableObject입니다.
/// HealthBarSystem이 현재 상태에 맞는 색상과 표시 방식을 읽습니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_HealthBarData", menuName = "HWJ/Data/System/Health Bar")]
public class HWJ_HealthBarDataSO : ScriptableObject
{
    [SerializeField] private string healthBarId;
    [SerializeField] private Color bodyStateColor = Color.red;
    [SerializeField] private Color soulStateColor = Color.cyan;
    [SerializeField] private Color possessedStateColor = new Color(0.6f, 0.2f, 1f);
    [SerializeField] private bool hideWhenDead = true;
    [SerializeField] private bool showSoulTimer = true;

    public string HealthBarId => healthBarId;
    public Color BodyStateColor => bodyStateColor;
    public Color SoulStateColor => soulStateColor;
    public Color PossessedStateColor => possessedStateColor;
    public bool HideWhenDead => hideWhenDead;
    public bool ShowSoulTimer => showSoulTimer;
}
