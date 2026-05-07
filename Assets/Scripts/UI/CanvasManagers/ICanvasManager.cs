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

        if (UIManager.instance != null)
        {
            UIManager.instance.ReportCanvasState(canvasToToggle, state);
        }
    }

    public void RefreshCanvaOrder(
        Canvas canvas,
        MyEnums.CanvasToToggle canvasToToggle,
        bool state)
    {
        int order = state && UIManager.instance != null &&
                    UIManager.instance.IsCanvasFocused(canvasToToggle)
            ? UIManager.FocusOrder
            : UIManager.DefaultOrder;
        if (canvas == null)
        {
            return;
        }

        canvas.sortingOrder = order;
    }

    public void ToggleCanvas(
        CanvasGroup canvasGroup,
        Canvas canvas,
        MyEnums.CanvasToToggle canvasToToggle,
        bool state)
    {
        SetCanvaState(canvasGroup, canvasToToggle, state);
        RefreshCanvaOrder(canvas, canvasToToggle, state);
    }

}
