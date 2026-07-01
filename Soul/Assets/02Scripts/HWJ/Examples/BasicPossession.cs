public class BasicPossession : PossessionBase // 기본 빙의 행동입니다.
{
    public override void UsePossessionSkill(int skillIndex) // 빙의 중 스킬을 사용합니다.
    {
        SkillController skillController = GetComponent<SkillController>(); // 같은 오브젝트에서 SkillController를 찾습니다.

        if (skillController == null) return; // 스킬 컨트롤러가 없으면 종료합니다.

        skillController.UseSkill(skillIndex); // 해당 인덱스의 스킬을 사용합니다.
    }
}
