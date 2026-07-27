using UnityEngine;

/// <summary>
/// 앞으로 씬과 프리팹에 붙일 빙의체 정신력 시스템입니다.
/// 기존 HWJ_BodyDecaySystem과 같은 런타임 데이터를 사용하되, 기획 의미는 부패가 아니라 빙의 유지 정신력입니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HWJ_RootObjectDataResolver))]
[RequireComponent(typeof(HWJ_SoulSystem))]
[AddComponentMenu("HWJ/Systems/Possession Mental System")]
public class HWJ_PossessionMentalSystem : HWJ_BodyDecaySystem
{
}
