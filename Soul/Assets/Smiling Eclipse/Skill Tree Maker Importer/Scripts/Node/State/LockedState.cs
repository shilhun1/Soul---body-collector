
namespace SmilingEclipse.STMImporter
{
    using UnityEngine;

    public class LockedState : IState<SkillNode>
    {
        public void Enter(SkillNode owner)
        {
            owner.Visual.LockedVisual();
            owner.DebugState = "Locked";
        }

        public void Exit(SkillNode owner)
        {
        }

        public void Update(SkillNode owner)
        {
        }

    }
}