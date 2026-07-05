namespace SmilingEclipse.STMImporter
{
    using TMPro;
    using UnityEngine;

    public class SkillTreePointsUI : MonoBehaviour
    {
        public CurrencyData skillPoints;
        public TextMeshProUGUI tmp;
        private void Start()
        {
            skillPoints.ResetSave();
            skillPoints.OnPointsChanged += UpdateInfo;
            UpdateInfo();
        }

        void UpdateInfo()
        {
            tmp.text = skillPoints.prefix + skillPoints.Points.ToString();
        }

        private void OnDestroy()
        {
            skillPoints.OnPointsChanged -= UpdateInfo;
        }
    }
}