using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_RootObjectData", menuName = "HWJ/Data/Root Object Data")]
public class HWJ_RootObjectDataSO : ScriptableObject
{
    [Header("Common Data")]
    [SerializeField] private HWJ_StatusData status;
    [SerializeField] private HWJ_ModelData model;
    [SerializeField] private HWJ_DamageData damage;
    [SerializeField] private HWJ_ReceivedDamageData receivedDamage;

    [Header("Selected Type")]
    [SerializeField] private HWJ_ObjectTypeDataSO selectedTypeData;

    public HWJ_StatusData Status => status;
    public HWJ_ModelData Model => model;
    public HWJ_DamageData Damage => damage;
    public HWJ_ReceivedDamageData ReceivedDamage => receivedDamage;
    public HWJ_ObjectTypeDataSO SelectedTypeData => selectedTypeData;
}
