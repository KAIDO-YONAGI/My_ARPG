using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICanvasManager
{
    ToggleCanvasEventSO ToggleCanvasEvent { get; }
    public void SetCanvaState(
           CanvasGroup canva,
           MyEnums.CanvasToToggle canvasToToggle,
           bool state)
    {
        canva.alpha = state ? 1 : 0;
        canva.blocksRaycasts = state;
        canva.interactable = state;
        UIManager.instance.ReportCanvasState(canvasToToggle, state);
    }
    // private int order = 0;
    // private Canvas canvas;
    // private void Start()
    // {
    //     canvas = currentCanvas.GetComponent<Canvas>();
    // }
    //     int order = state && UIManager.instance != null &&
    //             UIManager.instance.IsCanvasFocused(MyEnums.CanvasToToggle.Backpack)
    //     ? UIManager.FocusOrder
    //     : UIManager.DefaultOrder;
    // ((ICanvasManager)this).SetCanvaOrder(canvas, order);
    public void SetCanvaOrder(Canvas canvas, int order)
    {
        if (canvas == null)
        {
            return;
        }
        canvas.sortingOrder = order;
    }
}
