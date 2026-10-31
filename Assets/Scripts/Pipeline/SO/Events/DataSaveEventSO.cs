using System;
using Contracts;
using UnityEngine;

namespace Pipeline.SO.Events
{
    [CreateAssetMenu(fileName = "DataSaveEventSO", menuName = "Events/DataSaveEventSO", order = 0)]

    public class DataSaveEventSO : ScriptableObject
    {
        public event Action<SaveType> DataSaveEvent;
        public void RaiseDataSaveEvent(SaveType saveType)
        {
            DataSaveEvent?.Invoke(saveType);
        }
    }
}
