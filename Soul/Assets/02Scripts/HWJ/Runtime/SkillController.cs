using UnityEngine;
using System.Collections.Generic;  // List와 Dictionary를 사용하기 위해

public class SkillController : MonoBehaviour // 현재 사용 가능한 스킬과 쿨타임 관리
{
    private readonly List<SkillJsonData> currentSkills = new List<SkillJsonData>(); // 현재 장착된 스킬 목록
    private readonly Dictionary<string, float> cooldownEndTimes = new Dictionary<string, float>(); // 스킬별 쿨타임 종료 시간

    public void SetSkills(List<string> skillIds) // 스킬 ID 목록으로 현재 스킬을 설정
    {
        currentSkills.Clear(); // 기존 스킬 목록을 비움

        if (skillIds == null) // 스킬 ID 목록이 없는지 확인
        {
            return; // 없으면 종료
        }

        foreach (string id in skillIds) // 스킬 ID를 하나씩 순회
        { 
            SkillJsonData skill = GameDataManager.Instance.GetSkill(id); // ID로 스킬 데이터를 가져옴

            if (skill != null) // 스킬 데이터가 있는지 확인
            { 
                currentSkills.Add(skill); // 현재 스킬 목록에 추가
            } 
        } 
    } 

    public void AddSkill(string skillId) // 스킬 하나를 추가
    {
        SkillJsonData skill = GameDataManager.Instance.GetSkill(skillId); // 스킬 데이터를 가져옴

        if (skill != null) // 스킬 데이터가 있는지 확인
        {
            currentSkills.Add(skill); // 현재 스킬 목록에 추가
        }
    } 

    public void UseSkill(int index) // 인덱스로 스킬을 사용
    {
        if (index < 0 || index >= currentSkills.Count) // 인덱스가 범위를 벗어났는지 확인
        {
            return; // 잘못된 인덱스면 종료
        }

        SkillJsonData skill = currentSkills[index]; // 사용할 스킬 데이터를 가져옴

        if (skill == null) // 스킬 데이터가 없는지 확인
        {
            return; // 없으면 종료
        }

        float cooldownEndTime; // 쿨타임 종료 시간을 저장할 변수

        if (cooldownEndTimes.TryGetValue(skill.id, out cooldownEndTime) && Time.time < cooldownEndTime) // 아직 쿨타임인지 확인
        {
            return; // 쿨타임 중이면 스킬을 사용하지 않음
        }

        //SkillBehaviourBase behaviour = SkillBehaviourFactory.Create(skill.behaviourId, gameObject); // 스킬 행동 컴포넌트를 생성

        //if (behaviour == null) // 행동 컴포넌트 생성에 실패했는지 확인
        //{
        //    return; // 실패하면 종료
        //}

        //behaviour.Init(skill); // 스킬 데이터를 행동 컴포넌트에 전달
        //behaviour.Execute(gameObject); // 스킬을 실행
        //cooldownEndTimes[skill.id] = Time.time + skill.cooldown; // 쿨타임 종료 시간을 저장
        //Destroy(behaviour); // 일회성 행동 컴포넌트를 제거
    }
}
