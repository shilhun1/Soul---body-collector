namespace SmilingEclipse.STMImporter
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class SkillTreeController : MonoBehaviour
    {
        public SkillNodeDatabase database;
        private SkillNodeDatabase activeDatabase;

        public Transform nodeHolder;
        public Transform lineHolder;
        public SkillNode nodePrefab;
        public SkillNodeLine linePrefab;
        public SkillTooltipUI skillTooltipUI;
        public CurrencyData skillPoints;

        private Dictionary<SkillNodeData, SkillNode> dataXnode = new();
        private Dictionary<SkillNode, SkillNodeData> nodeXdata = new();
        private List<SkillNode> nodes = new();

        private SkillNode activeSkillNode = null;
        public SkillNode ActiveSkillNode => activeSkillNode;

        public Vector2 positionOffset = Vector2.zero;

        //public NodeInformationUI nodeInformationUI;

        public Action OnNodeBuyed;


        [Header("Save")]
        public bool saveAndLoad = true;

        [Header("Try Load Timer")]
        private float tryLoadTimer = 0f;

        private void Start()
        {
            ProvisorySave.saveAndLoad = saveAndLoad;
            skillPoints.saveAndLoad = saveAndLoad;
            TryLoad();
        }
        private void Update()
        {
            tryLoadTimer += Time.deltaTime;
            if (tryLoadTimer > 1f)
            {
                TryLoad();
            }
        }
        public void TryLoad()
        {
            if (database != null && activeDatabase != database) { StartCoroutine(Load()); }
        }
        public IEnumerator Load()
        {
            activeDatabase = database;
            for (int i = nodeHolder.childCount - 1; i >= 0; i--)
            {
                Destroy(nodeHolder.GetChild(i).gameObject);
            }
            yield return null;
            for (int x = lineHolder.childCount - 1; x >= 0; x--)
            {
                Destroy(lineHolder.GetChild(x).gameObject);
            }
            yield return null;
            SpawnNodes();

        }


        public void SpawnNodes()
        {
            dataXnode.Clear();
            nodeXdata.Clear();
            nodes.Clear();
            if (database == null) { Debug.LogError("Database is Missing"); }
            // ----- Step 1: Spawn Nodes and fill dictionaries ----
            for (int i = 0; i < database.datas.Count; i++)
            {
                SkillNodeData data = database.datas[i];
                if (data == null) continue;
                SkillNode node = Instantiate(nodePrefab, nodeHolder);
                node.name = $"Skill Node: {data.skillName}";
                nodes.Add(node);


                dataXnode.Add(data, node);
                nodeXdata.Add(node, data);
            }
            // ----- Step 2: Populate Child and Parent References from Dictionaries ----
            for (int i = 0; i < database.datas.Count; i++)
            {
                SkillNodeData data = database.datas[i];
                if (data == null) continue;
                SkillNode node = dataXnode[data];
                foreach (var childData in data.childNodes)
                {
                    SkillNode childNode = dataXnode[childData];
                    node.childNodes.Add(childNode);

                }
                foreach (var parentData in data.parentNodes)
                {
                    SkillNode parentNode = dataXnode[parentData];
                    node.parentNodes.Add(parentNode);
                }
                node.Initialize(this, data);
            }
            // ----- Step 3: Initialize nodes----
            for (int i = 0; i < database.datas.Count; i++)
            {
                SkillNodeData data = database.datas[i];
                if (data == null) continue;
                SkillNode node = dataXnode[data];


            }
            // ----- Step 4: Spawn Lines to the child nodes----
            for (int i = 0; i < nodes.Count; i++)
            {
                SkillNode node = nodes[i];
                node.SetupLinks();
            }

            // ----- Step 5: Update Info of all child nodes ----
            for (int i = 0; i < nodes.Count; i++)
            {
                SkillNode node = nodes[i];
                node.UpdateInfo();
            }
        }
        public SkillNodeLine SpawnLine()
        {
            SkillNodeLine line = Instantiate(linePrefab, lineHolder);
            return line;
        }
        public void UpdateInfo()
        {
            skillTooltipUI.UpdateInfo();
        }
        public void HandleNodeSelection(SkillNode newNodeSelected)
        {
            if (activeSkillNode != null) { activeSkillNode.DeselectMe(); }
            activeSkillNode = newNodeSelected;
            skillTooltipUI.Show(newNodeSelected);
        }
        public void HandleNodeDeselection(SkillNode newNodeSelected)
        {
            if (activeSkillNode != null) { activeSkillNode = null; }
            skillTooltipUI.Hide();
        }

    }
}