using UnityEngine;

public abstract class HWJ_ObjectTypeDataSO : ScriptableObject
{
    [SerializeField] private HWJ_ObjectType objectType;
    [SerializeField] private string typeId;

    public HWJ_ObjectType ObjectType => objectType;
    public string TypeId => typeId;
}
