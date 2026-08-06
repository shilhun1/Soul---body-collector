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
            if (data == null) return;

            if (titleTMP != null) titleTMP.text = data.skillName;
            if (descriptionTMP != null) descriptionTMP.text = data.description;
            if (costTMP != null)
            {
                string currencyStr = (controller != null && controller.skillPoints != null) ? controller.skillPoints.currencyName : "Points";
                costTMP.text = $"{node.RealCost} {currencyStr}";
                costTMP.color = node.CanBuy ? Color.white : Color.red;
            }
            if (levelTMP != null) levelTMP.text = $"{node.level}/{data.maxLevel}";

            if (node.UnlockedInfo != null)
            {
                bool isUnlocked = node.UnlockedInfo.isUnlocked;
                if (requiredContainer != null) requiredContainer.gameObject.SetActive(!isUnlocked);
                if (priceContainer != null) priceContainer.gameObject.SetActive(isUnlocked && !node.isMaxed);
                if (isUnlocked == false && requiredTMP != null)
                {
                    StringBuilder requiredNodesSB = new();
                    foreach (SkillNode pNode in node.parentNodes)
                    {
                        if (pNode == null || pNode.NodeData == null) continue;
                        if (requiredNodesSB.Length > 0) { requiredNodesSB.Append(", "); }
                        requiredNodesSB.Append(pNode.NodeData.skillName);
                    }
                    requiredTMP.text = "Requires: " + requiredNodesSB.ToString();
                }
            }
        }

        private void Update()
        {
            if (node == null || window == null) { return; }

            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = Camera.current;

            var mouse = Mouse.current;
            if (mouse != null && mainCam != null)
            {
                Vector3 mousePos = mouse.position.ReadValue();
                Vector3 mousePosZFix = new Vector3(mousePos.x, mousePos.y, -mainCam.transform.position.z);
            }

            window.transform.position = node.transform.position;

            ResolveCorner();
        }

        void ResolveCorner()
        {
            if (rect == null) return;

            float screenWidth = Screen.width;
            float sizeX = rect.rect.width;
            float halfSizeX = sizeX / 2f;

            var mouse = Mouse.current;
            float mouseX = mouse != null ? mouse.position.ReadValue().x : Input.mousePosition.x;

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