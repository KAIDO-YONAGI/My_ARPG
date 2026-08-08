using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "ToggleCanvasEventSO", menuName = "Events/ToggleCanvasEventSO", order = 0)]
//创建好之后不要忘了绑定到UIManager如果有键盘按键输入，可以一块绑定，没有也不影响
public class ToggleCanvasEventSO : ScriptableObject
{
    public event Action<bool> toggleCanvasEvent;
    public MyEnums.CanvasToToggle canvasToToggle;
    public void RaiseToggleCanvasEvent(bool state)
    {
        toggleCanvasEvent?.Invoke(state);
    }
}
