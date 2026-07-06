
namespace SmilingEclipse.STMImporter
{
    using UnityEngine;

    public class MaxedState : IState<SkillNode>
    {
        public void Enter(SkillNode owner)
        {
            owner.Visual.MaxedVisual();
            owner.DebugState = "Maxed";
        }

        public void Exit(SkillNode owner)
        {
        }

        public void Update(SkillNode owner)
        {
        }

    }
}