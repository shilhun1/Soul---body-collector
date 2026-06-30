//using UnityEngine;

//public static class SkillBehaviourFactory // 스킬 behaviourId를 실제 컴포넌트로 바꿔주는 팩토리
//{
//    public static SkillBehaviourBase Create(string behaviourId, GameObject owner) // behaviourId에 맞는 스킬 컴포넌트를 생성합니다.
//    {
//        if (owner == null) // 컴포넌트를 붙일 오브젝트가 없는지 확인
//        {
//            Debug.LogError("SkillBehaviourFactory owner가 null입니다."); // 오류 로그를 출력
//            return null; // 생성 실패를 반환
//        }
//        //switch (behaviourId) // behaviourId에 따라 분기
//        //{
//        //    case "melee_attack": // 근접 공격 스킬일 때 실행
//        //        return owner.AddComponent<MeleeAttackSkill>(); // 근접 공격 컴포넌트를 붙여 반환

//        //    case "magic_projectile": // 마법 투사체 스킬일 때 실행
//        //        return owner.AddComponent<MagicProjectileSkill>(); // 마법 투사체 컴포넌트를 붙여 반환

//        //    default: // 등록되지 않은 ID일 때 실행합니다.
//        //        Debug.LogError("Unknown skill behaviourId: " + behaviourId); // 오류 로그를 출력
//        //        return null; // 생성 실패를 반환
//        //}
//    }
//}