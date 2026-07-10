namespace SmilingEclipse.STMImporter
{
    using UnityEngine;

    public class BuyableState : IState<SkillNode>
    {
        public void Enter(SkillNode owner)
        {
            owner.Visual.BuyableVisual();
            owner.DebugState = "Buyable";
        }

        public void Exit(SkillNode owner)
        {
        }

        public void Update(SkillNode owner)
        {
        }

    }
}