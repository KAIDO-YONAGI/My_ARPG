using System;
using UnityEngine;

public class BackCanvasManager : MonoBehaviour
{
    public CanvasGroup currentCanvas;
    public ToggleCanvasEventSO toggleBackpackCanvasEventSO;

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
    }
    private void SetCanvaState(CanvasGroup canva, bool state)
    {
        canva.alpha = state ? 1 : 0;
        canva.blocksRaycasts = state;
        canva.interactable = state;
    }
}
