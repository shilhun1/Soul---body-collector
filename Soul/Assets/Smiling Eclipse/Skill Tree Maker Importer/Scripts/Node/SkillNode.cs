namespace SmilingEclipse.STMImporter
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using TMPro;
    using UnityEditor.Experimental.GraphView;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;
    using static UnityEngine.UI.GridLayoutGroup;

    public class SkillNode : MonoBehaviour
    {
        [Header("References")]
        private SkillTreeController controller;
        [SerializeField] private SkillNodeVisual visual;
        [SerializeField] private Button button;

        private SkillNodeData nodeData;

        [SerializeField] public List<SkillNode> parentNodes = new();
        [SerializeField] public List<SkillNode> childNodes = new();
        [HideInInspector] public List<SkillNodeLine> myLines = new();
        public StateMachine<SkillNode> stateMachine;
        public string DebugState;

        [Header("Values")]
        public int level = 0;

        [Header("Helpers")]
        [SerializeField] private UnlockedInfo unlockedInfo;

        [Header("Save & Load")]
        public bool saveAndLoad = true;

        public bool isMaxed = false;
        public bool isSelected = false;

        [Header("Actions")]
        public Action OnClicked;
        public Action OnBuyed;
        public Action OnMaxed;
        public Action OnUnlocked;
        public Action OnSelected;
        public Action OnDeselected;

        [Header("Events")]
        public UnityEvent OnBuyEvent;

        //-------------------------------
        //     Properties & Lambdas
        //-------------------------------

        public bool IsSelected { get { return isSelected; } set { isSelected = value; if (value) { visual.Selected(); } else { visual.Deselected(); } } }
        public SkillNodeData NodeData => nodeData;
        public UnlockedInfo UnlockedInfo => unlockedInfo;
        public SkillNodeVisual Visual => visual;

        public bool CanBuy => stateMachine.CurrentState is BuyableState;
        public float RealCost => nodeData.cost + nodeData.costPerSkillLevel * level;

        //-------------------------------
        //     
        //-------------------------------

        public void Initialize(SkillTreeController controller, SkillNodeData nodeData)
        {
            this.controller = controller;
            this.nodeData = nodeData;

            stateMachine = new(this);
            stateMachine.AddState(new BuyableState());
            stateMachine.AddState(new LockedState());
            stateMachine.AddState(new UnlockedState());
            stateMachine.AddState(new MaxedState());

            visual.Initialize(this);
            visual.Button.onClick.AddListener(HandleClick);

            level = Load(nodeData.startLevel, "level");
            bool initiallyUnlocked = Load(level > 0, "isUnlocked");
            unlockedInfo.Setup(initiallyUnlocked, nodeData.parentNodes.Count);
            TryUnlock();
            controller.skillPoints.OnPointsChanged += UpdateInfo;

            if (unlockedInfo.isUnlocked) { stateMachine.ChangeState<LockedState>(); } else { stateMachine.ChangeState<UnlockedState>(); }
            visual.Deselected();
            visual.IconImage.sprite = nodeData.icon;
            UpdateInfo();
            transform.localPosition = new Vector2(nodeData.position.x, -nodeData.position.y) + controller.positionOffset;
            transform.localScale = Vector3.one * NodeData.scale;


            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => TryBuy());

            controller.OnNodeBuyed += UpdateInfo;



        }
        public void UpdateInfo()
        {
            TryUnlock();
            HandleMaxed();
            HandleAvaibility();
            visual.UpdateLineColor();
            controller.UpdateInfo();
        }
        public void SetupLinks()
        {
            foreach (var childNode in childNodes)
            {
                LinkChild(childNode);
            }
        }
        void LinkChild(SkillNode childNode)
        {
            SkillNodeLine line = controller.SpawnLine();
            myLines.Add(line);
            line.Initialize(this, childNode);
        }
        void HandleClick()
        {
            OnClicked?.Invoke();
            if (TryBuy())
            {
                controller.UpdateInfo();
            }

        }
        public void SelectMe()
        {
            controller.HandleNodeSelection(this);
            IsSelected = true;
            UpdateInfo();
            OnSelected?.Invoke();
        }
        public void DeselectMe()
        {
            controller.HandleNodeDeselection(this);
            IsSelected = false;
            UpdateInfo();
            OnDeselected?.Invoke();
        }
        void TryUnlock()
        {
            int unlockValue = 0;
            foreach (SkillNode parentNode in parentNodes)
            {
                if (parentNode.unlockedInfo.isUnlocked && parentNode.level > 0) { unlockValue++; }
            }
            bool result = unlockedInfo.TryUnlock(unlockValue);
            if (result)
            {
                stateMachine.ChangeState<UnlockedState>();
                OnUnlocked?.Invoke();
            }
            else { stateMachine.ChangeState<LockedState>(); }
            Save(result, "isUnlocked");
        }
        void HandleAvaibility()
        {
            if (isMaxed == true) { return; }
            if (unlockedInfo.isUnlocked == false) { return; }
            bool isBuyable = controller.skillPoints.CanSpentPoints(RealCost); ;
            if (isBuyable) { stateMachine.ChangeState<BuyableState>(); }
        }
        void HandleMaxed()
        {
            if (unlockedInfo.isUnlocked == false) { return; }
            if (level >= nodeData.maxLevel) { isMaxed = true; stateMachine.ChangeState<MaxedState>(); OnMaxed?.Invoke(); }
            else { isMaxed = false; }
        }
        public bool TryBuy()
        {
            if (CanBuy)
            {
                Buy();
                return true;
            }
            return false;
        }
        public void Buy()
        {
            controller.skillPoints.SpentPoints(RealCost);
            level++;
            Save(level, "level");

            controller.OnNodeBuyed?.Invoke();
            OnBuyed?.Invoke();
            OnBuyEvent?.Invoke();
            UpdateInfo();
        }
        //Faça a logica do painel de informaçoes do node, o NodeInformationUIItem

        private void OnDestroy()
        {
            controller.skillPoints.OnPointsChanged -= UpdateInfo;
            controller.OnNodeBuyed -= UpdateInfo;
        }

        void Save<T>(T value, string id)
        {
            ProvisorySave.Save(value, controller.database.skillTreeName + id, nodeData.nodeIndex);
        }

        T Load<T>(T defaultValue, string id)
        {
            return ProvisorySave.Load(defaultValue, controller.database.skillTreeName + id, nodeData.nodeIndex);
        }
    }
}