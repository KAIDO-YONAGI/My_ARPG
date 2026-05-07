using System;
using Unity.VisualScripting;
using UnityEngine;

public class BackpackCanvasManager : MonoBehaviour, ICanvasManager
{
    public CanvasGroup currentCanvas;
    public ToggleCanvasEventSO toggleBackpackCanvasEventSO;

    public ToggleCanvasEventSO ToggleCanvasEvent => toggleBackpackCanvasEventSO;
    private int order = 0;
    private Canvas canvas;
    private void Start()
    {
        canvas = currentCanvas.GetComponent<Canvas>();
    }
    private void OnEnable()
    {
        toggleBackpackCanvasEventSO.toggleCanvasEvent += OnToggleBackpack;
    }
    private void OnDisable()
    {
        toggleBackpackCanvasEventSO.toggleCanvasEvent -= OnToggleBackpack;

    }
    private void OnToggleBackpack(bool state)
    {
        SetCanvaState(currentCanvas, state);

        int order = state && UIManager.instance != null &&
                    UIManager.instance.IsCanvasFocused(MyEnums.CanvasToToggle.Backpack)
            ? UIManager.FocusOrder
            : UIManager.DefaultOrder;
        ((ICanvasManager)this).SetCanvaOrder(canvas, order);
    }
    private void SetCanvaState(CanvasGroup canva, bool state)
    {
        canva.alpha = state ? 1 : 0;
        canva.blocksRaycasts = state;
        canva.interactable = state;

        if (UIManager.instance != null)
        {
            UIManager.instance.ReportCanvasState(MyEnums.CanvasToToggle.Backpack, state);
        }
    }
}
