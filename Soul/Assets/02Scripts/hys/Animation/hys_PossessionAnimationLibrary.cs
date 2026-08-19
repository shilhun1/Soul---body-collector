using UnityEngine;

/// <summary>
/// HWJ 무기 타입별 플레이어 빙의 애니메이션 클립을 공용으로 제공합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "hys_PossessionAnimationLibrary",
    menuName = "hys/Animation/Possession Animation Library")]
public sealed class hys_PossessionAnimationLibrary : ScriptableObject
{
    [SerializeField] private AnimationClip swordPossessionClip;
    [SerializeField] private AnimationClip axePossessionClip;
    [SerializeField] private AnimationClip bowPossessionClip;
    [SerializeField] private AnimationClip lancePossessionClip;
    [SerializeField] private AnimationClip shieldPossessionClip;
    [SerializeField] private AnimationClip swordSoulExitClip;
    [SerializeField] private AnimationClip axeSoulExitClip;
    [SerializeField] private AnimationClip bowSoulExitClip;
    [SerializeField] private AnimationClip lanceSoulExitClip;
    [SerializeField] private AnimationClip shieldSoulExitClip;
    [SerializeField] private RuntimeAnimatorController swordPlayerController;
    [SerializeField] private RuntimeAnimatorController axePlayerController;
    [SerializeField] private RuntimeAnimatorController bowPlayerController;
    [SerializeField] private RuntimeAnimatorController lancePlayerController;
    [SerializeField] private RuntimeAnimatorController shieldPlayerController;

    // 빙의한 몬스터의 HWJ 무기 타입과 일치하는 플레이어 전용 클립을 반환합니다.
    public AnimationClip GetPossessionClip(HWJ_WeaponType weaponType)
    {
        switch (weaponType)
        {
            case HWJ_WeaponType.Axe:
                return axePossessionClip;
            case HWJ_WeaponType.Bow:
                return bowPossessionClip;
            case HWJ_WeaponType.Lance:
                return lancePossessionClip;
            case HWJ_WeaponType.Shield:
                return shieldPossessionClip;
            case HWJ_WeaponType.Sword:
                return swordPossessionClip;
            default:
                return null;
        }
    }

    // HWJ가 빙의 정보를 지운 뒤에도 마지막 육체에 맞는 해제 모션을 선택합니다.
    public AnimationClip GetSoulExitClip(HWJ_WeaponType weaponType)
    {
        switch (weaponType)
        {
            case HWJ_WeaponType.Axe:
                return axeSoulExitClip;
            case HWJ_WeaponType.Bow:
                return bowSoulExitClip;
            case HWJ_WeaponType.Lance:
                return lanceSoulExitClip;
            case HWJ_WeaponType.Shield:
                return shieldSoulExitClip;
            case HWJ_WeaponType.Sword:
                return swordSoulExitClip;
            default:
                return null;
        }
    }

    // 빙의 시작 연출이 끝난 뒤 사용할 무기별 플레이어 Controller를 반환합니다.
    public RuntimeAnimatorController GetPlayerController(HWJ_WeaponType weaponType)
    {
        switch (weaponType)
        {
            case HWJ_WeaponType.Axe:
                return axePlayerController;
            case HWJ_WeaponType.Bow:
                return bowPlayerController;
            case HWJ_WeaponType.Lance:
                return lancePlayerController;
            case HWJ_WeaponType.Shield:
                return shieldPlayerController;
            case HWJ_WeaponType.Sword:
                return swordPlayerController;
            default:
                return null;
        }
    }
}
