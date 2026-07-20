using UnityEngine;
using SmilingEclipse.STMImporter;

/// <summary>
/// 에셋 스토어의 Smiling Eclipse - Skill Tree Maker Importer 데이터를 
/// 안전하게 초기화(Reset)해주는 HSH 전용 유틸리티 스크립트입니다.
/// UI 버튼 등에 연결하여 사용할 수 있습니다.
/// </summary>
public class HSH_SkillResetter : MonoBehaviour
{
    [Header("스킬트리 데이터 (Smiling Eclipse)")]
    [Tooltip("초기화할 스킬트리의 Database 에셋을 끌어다 넣으세요.")]
    public SkillNodeDatabase targetDatabase;

    [Header("스킬 포인트 데이터 (Smiling Eclipse)")]
    [Tooltip("초기화할 CurrencyData(보통 Skill Points) 에셋을 끌어다 넣으세요.")]
    public CurrencyData skillPoints;

    [Header("스킬트리 새로고침 (옵션)")]
    [Tooltip("초기화 후 즉시 화면을 갱신하려면 씬에 배치된 SkillTreeController를 끌어다 넣으세요.")]
    public SkillTreeController skillTreeController;

    /// <summary>
    /// 버튼의 OnClick 이벤트에 연결하여 스킬과 포인트를 동시에 초기화합니다.
    /// </summary>
    public void ResetSkillsAndPoints()
    {
        // 1. 스킬 진행도(PlayerPrefs) 안전하게 초기화
        if (targetDatabase != null)
        {
            string treeName = targetDatabase.skillTreeName;
            foreach (var node in targetDatabase.datas)
            {
                if (node == null) continue;
                
                // 해당 스킬 노드의 레벨과 해금 상태 키를 찾아 개별 삭제
                PlayerPrefs.DeleteKey(treeName + "level" + node.nodeIndex);
                PlayerPrefs.DeleteKey(treeName + "isUnlocked" + node.nodeIndex);
            }
            PlayerPrefs.Save();
            Debug.Log("[HSH_SkillResetter] 스킬트리 진행도가 안전하게 초기화되었습니다.");
        }
        else
        {
            Debug.LogWarning("[HSH_SkillResetter] Target Database가 연결되지 않아 스킬 초기화를 건너뜁니다.");
        }

        // 2. 스킬 포인트 초기화
        if (skillPoints != null)
        {
            // 에셋의 내장 ResetPoints()는 UI 갱신 이벤트를 발생시키지 않는 구조적 문제가 있어서,
            // 프로퍼티(Points)에 직접 값을 넣어 강제로 UI 갱신 이벤트를 트리거시킵니다.
            skillPoints.Points = skillPoints.basePoints;
            Debug.Log("[HSH_SkillResetter] 스킬 포인트가 기본값으로 초기화되었습니다.");
        }
        else
        {
            Debug.LogWarning("[HSH_SkillResetter] Skill Points 에셋이 연결되지 않아 포인트 초기화를 건너뜁니다.");
        }

        // 3. UI 즉시 새로고침 (화면 반영)
        if (skillTreeController != null)
        {
            // 컨트롤러가 노드를 파괴하고 다시 스폰하도록 Load 코루틴을 강제로 실행합니다.
            skillTreeController.StartCoroutine(skillTreeController.Load());
            Debug.Log("[HSH_SkillResetter] 스킬트리 UI가 새로고침 되었습니다.");
        }
    }

    /// <summary>
    /// (옵션) 에셋 내장 함수를 사용한 극단적 초기화.
    /// 경고: 게임 내의 모든 PlayerPrefs 저장 데이터(옵션 등)가 통째로 삭제됩니다!
    /// </summary>
    public void HardResetAllSaveData()
    {
        ProvisorySave.DeleteSave();
        Debug.LogWarning("[HSH_SkillResetter] 주의! 게임의 모든 저장 데이터(PlayerPrefs)가 삭제되었습니다.");
    }
}
