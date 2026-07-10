using UnityEngine;

public class HWJ_PossessionBodyState : MonoBehaviour
{
    [SerializeField] private bool isConsumed;

    public bool IsConsumed => isConsumed;

    public void MarkConsumed()
    {
        isConsumed = true;
    }

    public void ResetConsumed()
    {
        isConsumed = false;
    }
}
