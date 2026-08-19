using UnityEngine;

/// <summary>
/// RuntimeStatus의 HP 계산 전에 부착된 보호막 컴포넌트에 피해 흡수 기회를 제공합니다.
/// </summary>
public partial class HWJ_RuntimeStatusSystem
{
    private bool TryApplyDamageAbsorbers(
        float incomingDamage,
        Component source,
        HWJ_DamageData sourceDamage,
        out float remainingDamage)
    {
        remainingDamage = Mathf.Max(0f, incomingDamage);
        MonoBehaviour[] localBehaviours = GetComponents<MonoBehaviour>();
        bool handled = false;

        for (int i = 0; i < localBehaviours.Length && remainingDamage > 0f; i++)
        {
            if (!(localBehaviours[i] is HWJ_IDamageAbsorber absorber))
            {
                continue;
            }

            handled |= absorber.TryAbsorbDamage(
                remainingDamage,
                source,
                sourceDamage,
                out remainingDamage);
            remainingDamage = Mathf.Max(0f, remainingDamage);
        }

        return handled;
    }
}
