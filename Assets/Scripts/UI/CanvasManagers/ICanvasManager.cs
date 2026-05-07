using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICanvasManager
{
    ToggleCanvasEventSO ToggleCanvasEvent { get; }
    private void SetCanvaState(
            CanvasGroup canva,
            MyEnums.CanvasToToggle canvasToToggle,
            bool state)
    {
        canva.alpha = state ? 1 : 0;
        canva.blocksRaycasts = state;
        canva.interactable = state;
        UIManager.instance.ReportCanvasState(canvasToToggle, state);
    }
    private void SetCanvaOrder(Canvas canvas, int number)
    {
        canvas.sortingOrder = number;
    }
}
