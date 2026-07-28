namespace SmilingEclipse.STMImporter
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Skill Tree Maker Converter/Node Data")]
    public class SkillNodeData : ScriptableObject
    {
        public string skillName;
        public string description;
        public Sprite icon;
        public Vector2 position;
        public float cost = 10;
        public float costPerSkillLevel = 1;
        [Range(0, 10)] public int startLevel = 0;
        [Range(0, 10)] public int maxLevel = 1;
        public int nodeIndex;
        public float scale = 1f;

        // Ligacoes com outras skills
        public List<SkillNodeData> childNodes;
        public List<SkillNodeData> parentNodes;

        [Header("HWJ Integration")]
        [Tooltip("HWJ 스킬 데이터 연동")]
        public HWJ_SkillNodeDataSO hwjSkillData;
    }
}