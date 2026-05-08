using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class OpenSaveLoadCanvasButton : MonoBehaviour
{
    public OpenSaveLoadPanelEventSO openSaveLoadSceneEvent;
    public ToggleCanvasEventSO toggleSaveLoadCanvasEvent;
    public Button OpenButton;
    public MyEnums.SaveLoadPanelType saveLoadPanelType;
    private void OnEnable()
    {
        OpenButton.onClick.AddListener(

            () =>
            {
                openSaveLoadSceneEvent.RaiseOpenSaveLoadPanelEvent(saveLoadPanelType);
                toggleSaveLoadCanvasEvent.RaiseToggleCanvasEvent(true);
            }
            );
    }
    private void OnDisable()
    {
        OpenButton.onClick.RemoveAllListeners();
    }
}
