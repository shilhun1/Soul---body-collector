using UnityEngine;

public class HWJ_RewardClaimState : MonoBehaviour
{
    [SerializeField] private bool isClaimed;

    public bool IsClaimed => isClaimed;

    public bool TryClaim()
    {
        if (isClaimed)
        {
            return false;
        }

        isClaimed = true;
        return true;
    }
}
