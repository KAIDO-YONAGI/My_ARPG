using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "OpenSaveLoadPanelEventSO", menuName = "Events/OpenSaveLoadPanelEventSO", order = 0)]
public class OpenSaveLoadPanelEventSO : ScriptableObject
{
    public UnityAction<MyEnums.SaveLoadPanelType> OpenSaveLoadPanelEvent;

    public void RaiseOpenSaveLoadPanelEvent(MyEnums.SaveLoadPanelType panelType)
    {
        OpenSaveLoadPanelEvent?.Invoke(panelType);
    }
}
