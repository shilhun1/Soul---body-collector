using UnityEngine; // GameObject와 Debug를 사용하기 위해 필요합니다.

public static class SkillBehaviourFactory // 스킬 behaviourId를 실제 스킬 컴포넌트로 연결합니다.
{
    public static SkillBehaviourBase Create(string behaviourId, GameObject owner) // 스킬 행동 컴포넌트를 생성합니다.
    {
        if (owner == null) // 컴포넌트를 붙일 대상이 없으면
        {
            Debug.LogError("SkillBehaviourFactory owner is null."); // 오류를 출력합니다.
            return null; // 실패를 반환합니다.
        }

        switch (behaviourId) // behaviourId에 따라 생성할 스킬을 선택합니다.
        {
            case "melee_attack": // 근접 공격 스킬입니다.
                return owner.AddComponent<MeleeAttackSkill>(); // 근접 공격 컴포넌트를 붙이고 반환합니다.

            case "magic_projectile": // 마법 투사체 스킬입니다.
                return owner.AddComponent<MagicProjectileSkill>(); // 마법 투사체 컴포넌트를 붙이고 반환합니다.

            default: // 등록되지 않은 ID입니다.
                Debug.LogError("Unknown skill behaviourId: " + behaviourId); // 오류를 출력합니다.
                return null; // 실패를 반환합니다.
        }
    }
}
