using System.Collections.Generic; // List와 Dictionary를 사용하기 위해 필요합니다.
using UnityEngine; // MonoBehaviour와 Time을 사용하기 위해 필요합니다.

public class SkillController : MonoBehaviour // 현재 사용 가능한 스킬과 쿨타임을 관리합니다.
{
    [SerializeField] private List<string> defaultSkillIds = new List<string>(); // 플레이어 기본 스킬 ID 목록입니다.

    private readonly List<SkillJsonData> currentSkills = new List<SkillJsonData>(); // 현재 장착된 스킬 데이터 목록입니다.
    private readonly Dictionary<string, float> cooldownEndTimes = new Dictionary<string, float>(); // 스킬별 쿨타임 종료 시간입니다.

    private void Start() // 첫 프레임 전에 실행됩니다.
    {
        SetDefaultSkills(); // 기본 스킬을 장착합니다.
    }

    public void SetDefaultSkills() // 기본 스킬 목록으로 되돌립니다.
    {
        SetSkills(defaultSkillIds); // 기본 스킬 ID 목록을 적용합니다.
    }

    public void SetSkills(List<string> skillIds) // 스킬 ID 목록을 현재 스킬 목록으로 설정합니다.
    {
        currentSkills.Clear(); // 기존 스킬 목록을 비웁니다.

        if (skillIds == null) // 전달된 목록이 없으면
        {
            return; // 종료합니다.
        }

        foreach (string id in skillIds) // 스킬 ID를 하나씩 확인합니다.
        {
            SkillJsonData skill = GameDataManager.Instance.GetSkill(id); // ID로 스킬 데이터를 찾습니다.

            if (skill != null) // 스킬 데이터가 있으면
            {
                currentSkills.Add(skill); // 현재 목록에 추가합니다.
            }
        }
    }

    public void AddSkill(string skillId) // 스킬 하나를 추가합니다.
    {
        SkillJsonData skill = GameDataManager.Instance.GetSkill(skillId); // 스킬 데이터를 찾습니다.

        if (skill != null && !currentSkills.Contains(skill)) // 데이터가 있고 중복이 아니라면
        {
            currentSkills.Add(skill); // 현재 목록에 추가합니다.
        }
    }

    public void UseSkill(int index) // 인덱스에 해당하는 스킬을 사용합니다.
    {
        if (index < 0 || index >= currentSkills.Count) return; // 인덱스가 잘못되면 종료합니다.

        SkillJsonData skill = currentSkills[index]; // 사용할 스킬을 가져옵니다.

        if (skill == null) return; // 스킬 데이터가 없으면 종료합니다.

        float cooldownEndTime; // 쿨타임 종료 시간을 담을 변수입니다.

        if (cooldownEndTimes.TryGetValue(skill.id, out cooldownEndTime) && Time.time < cooldownEndTime) return; // 아직 쿨타임이면 종료합니다.

        SkillBehaviourBase behaviour = SkillBehaviourFactory.Create(skill.behaviourId, gameObject); // behaviourId에 맞는 스킬 행동 컴포넌트를 생성합니다.

        if (behaviour == null) return; // 생성에 실패하면 종료합니다.

        behaviour.Init(skill); // 스킬 데이터를 전달합니다.
        behaviour.Execute(gameObject); // 스킬을 실행합니다.
        cooldownEndTimes[skill.id] = Time.time + skill.cooldown; // 다음 사용 가능 시간을 저장합니다.
        Destroy(behaviour); // 일회성 행동 컴포넌트를 제거합니다.
    }
}
