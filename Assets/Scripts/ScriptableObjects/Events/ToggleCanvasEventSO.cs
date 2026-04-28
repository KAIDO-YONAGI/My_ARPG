using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[CreateAssetMenu(fileName = "ToggleCanvasEventSO", menuName = "Events/ToggleCanvasEventSO", order = 0)]

public class ToggleCanvasEventSO : ScriptableObject
{
    public UnityAction< bool> toggleCanvasEvent;
    public MyEnums.CanvasToToggle canvasToToggle;
    public void RaiseToggleCanvasEvent(bool state)
    {
        toggleCanvasEvent?.Invoke(state);
    }
}
