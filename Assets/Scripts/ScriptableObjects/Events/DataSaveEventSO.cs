using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[CreateAssetMenu(fileName = "DataSaveEventSO", menuName = "Events/DataSaveEventSO", order = 0)]

public class DataSaveEventSO : ScriptableObject
{
    public UnityAction<MyEnums.SaveType> DataSaveEvent;
    public void RaiseDataSaveEvent(MyEnums.SaveType saveType)
    {
        DataSaveEvent?.Invoke(saveType);
    }
}
