namespace SmilingEclipse.STMImporter
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Skill Tree Maker Converter/Node Database")]
    public class SkillNodeDatabase : ScriptableObject
    {
        public string skillTreeName = string.Empty;
        public List<SkillNodeData> datas = new();
    }
}