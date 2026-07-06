namespace SmilingEclipse.STMImporter
{
    using UnityEngine;

    public class UnlockedState : IState<SkillNode>
    {
        public void Enter(SkillNode owner)
        {
            owner.Visual.UnlockedVisual();
            owner.DebugState = "Unlocked";
            
        }

        public void Exit(SkillNode owner)
        {
        }

        public void Update(SkillNode owner)
        {
        }

    }
}