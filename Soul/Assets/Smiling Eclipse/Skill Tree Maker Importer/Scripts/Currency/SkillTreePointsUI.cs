namespace SmilingEclipse.STMImporter
{
    using TMPro;
    using UnityEngine;

    public class SkillTreePointsUI : MonoBehaviour
    {
        public CurrencyData skillPoints;
        public TextMeshProUGUI tmp;
        private HWJ_LevelUpSystem levelUpSystem;

        private void Start()
        {
            // HWJ 연동: 레벨업 시스템 찾기
            levelUpSystem = Object.FindAnyObjectByType<HWJ_LevelUpSystem>();

            if (levelUpSystem != null)
            {
                HWJ_GameplayEvents.SkillPointChanged += OnSkillPointChanged;
                HWJ_GameplayEvents.PlayerLevelChanged += OnPlayerLevelChanged;
                UpdateInfoHWJ();
            }
            else
            {
                // 기존 로직
                if (skillPoints != null)
                {
                    skillPoints.ResetSave();
                    skillPoints.OnPointsChanged += UpdateInfo;
                    UpdateInfo();
                }
            }
        }

        private void OnSkillPointChanged(HWJ_SkillPointChangedEvent evt)
        {
            UpdateInfoHWJ();
        }

        private void OnPlayerLevelChanged(HWJ_PlayerLevelChangedEvent evt)
        {
            UpdateInfoHWJ();
        }

        private void UpdateInfoHWJ()
        {
            if (tmp != null && levelUpSystem != null)
            {
                string prefix = skillPoints != null ? skillPoints.prefix : "SP: ";
                tmp.text = prefix + levelUpSystem.SkillPoint.ToString();
            }
        }

        void UpdateInfo()
        {
            if (tmp != null && skillPoints != null)
            {
                tmp.text = skillPoints.prefix + skillPoints.Points.ToString();
            }
        }

        private void OnDestroy()
        {
            if (levelUpSystem != null)
            {
                HWJ_GameplayEvents.SkillPointChanged -= OnSkillPointChanged;
                HWJ_GameplayEvents.PlayerLevelChanged -= OnPlayerLevelChanged;
            }
            
            if (skillPoints != null)
            {
                skillPoints.OnPointsChanged -= UpdateInfo;
            }
        }
    }
}