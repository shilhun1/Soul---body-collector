namespace SmilingEclipse.STMImporter
{
    using System.Text;
    using TMPro;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.UI;

    public class SkillTooltipUI : MonoBehaviour
    {
        [SerializeField] SkillTreeController controller;
        [SerializeField] private GameObject window;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI requiredTMP;
        [SerializeField] private TextMeshProUGUI descriptionTMP;
        [SerializeField] private TextMeshProUGUI costTMP;
        [SerializeField] private TextMeshProUGUI levelTMP;

        [SerializeField] private Transform priceContainer;
        [SerializeField] private Transform requiredContainer;
        private RectTransform rect;


        private SkillNode node;

        private void Start()
        {
            Hide();
            rect = window.GetComponent<RectTransform>();
        }
        public void Show(SkillNode node)
        {
            this.node = node;
            window.SetActive(true);
            UpdateInfo();

        }
        public void Hide()
        {
            window.SetActive(false);
        }
        public void UpdateInfo()
        {
            if (node == null) { Hide(); return; }
            SkillNodeData data = node.NodeData;


            titleTMP.text = node.NodeData.skillName;
            descriptionTMP.text = data.description;
            costTMP.text = $"{node.RealCost} {controller.skillPoints.currencyName}";
            costTMP.color = node.CanBuy ? Color.white : Color.red;
            levelTMP.text = $"{node.level}/{data.maxLevel}";


            bool isUnlocked = node.UnlockedInfo.isUnlocked;
            requiredContainer.gameObject.SetActive(!isUnlocked);
            priceContainer.gameObject.SetActive(isUnlocked && !node.isMaxed);
            if (isUnlocked == false)
            {

                StringBuilder requiredNodesSB = new();
                foreach (SkillNode pNode in node.parentNodes)
                {
                    if (requiredNodesSB.Length > 0) { requiredNodesSB.Append(", "); }
                    requiredNodesSB.Append(pNode.NodeData.skillName);
                }
                requiredTMP.text = "Requires: " + requiredNodesSB.ToString();
            }






        }
        private void Update()
        {
            if ((node == null)) { return; }

            Vector3 mousePos = Mouse.current.position.ReadValue();
            Vector3 mousePosZFix = new Vector3(mousePos.x, mousePos.y, -Camera.main.transform.position.z);
            window.transform.position = node.transform.position;
            //Camera.main.ScreenToWorldPoint(mousePosZFix);


            ResolveCorner();
        }

        void ResolveCorner()
        {
            float screenWidth = Screen.width;
            float sizeX = rect.rect.width;
            float halfSizeX = sizeX / 2f;
            float mouseX = Mouse.current.position.ReadValue().x;

            float cornerL = halfSizeX;
            float cornerR = screenWidth - halfSizeX;

            if (mouseX < cornerL)
            {
                rect.pivot = new Vector2(0, rect.pivot.y);
            }
            else if (mouseX > cornerR)
            {
                rect.pivot = new Vector2(1f, rect.pivot.y);
            }
            else
            {
                rect.pivot = new Vector2(0.5f, rect.pivot.y);
            }
        }
    }
}