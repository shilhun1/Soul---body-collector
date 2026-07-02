using UnityEngine;

public class HWJ_RootObjectDataResolver : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataSO rootObjectData;

    public HWJ_RootObjectDataSO RootObjectData => rootObjectData;
    public HWJ_StatusData Status => rootObjectData != null ? rootObjectData.Status : null;
    public HWJ_ModelData Model => rootObjectData != null ? rootObjectData.Model : null;
    public HWJ_DamageData Damage => rootObjectData != null ? rootObjectData.Damage : null;
    public HWJ_ReceivedDamageData ReceivedDamage => rootObjectData != null ? rootObjectData.ReceivedDamage : null;
    public HWJ_ObjectTypeDataSO TypeData => rootObjectData != null ? rootObjectData.SelectedTypeData : null;

    public bool TryGetTypeData<T>(out T typedData) where T : HWJ_ObjectTypeDataSO
    {
        typedData = TypeData as T;
        return typedData != null;
    }
}
