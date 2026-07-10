using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SmilingEclipse.STMImporter
{
    using UnityEngine.UI;

    public class SkillNodeVisual : MonoBehaviour
    {
        private SkillNode skillNode;
        [Header("UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Transform outlineT;
        [SerializeField] private Transform maxedOutlineT;
        [SerializeField] private Button button;



        public Color AvaibleColor;
        public Color LockedColor;
        public Color UnlockedColor;

        public Color lineColor;
        public Color lineUnlockedColor;

        public Image IconImage => iconImage;
        public Button Button => button;
        public void Initialize(SkillNode skillNode)
        {
            this.skillNode = skillNode;

        }
        public void Selected()
        {
            outlineT.gameObject.SetActive(true);
        }
        public void Deselected()
        {
            outlineT.gameObject.SetActive(false);
        }

        public void BuyableVisual()
        {
            iconImage.color = AvaibleColor;
            maxedOutlineT.gameObject.SetActive(false);
        }
        public void LockedVisual()
        {
            iconImage.color = LockedColor;
            maxedOutlineT.gameObject.SetActive(false);
        }
        public void UnlockedVisual()
        {
            iconImage.color = UnlockedColor;
            maxedOutlineT.gameObject.SetActive(false);
        }
        public void MaxedVisual()
        {
            iconImage.color = AvaibleColor;
            maxedOutlineT.gameObject.SetActive(true);
        }

        public void UpdateLineColor()
        {
            bool unlocked =  skillNode.level > 0;

            foreach (var line in skillNode.myLines)
            {
                line.SetColor(unlocked ? lineUnlockedColor : lineColor);
            }

        }

    }
}