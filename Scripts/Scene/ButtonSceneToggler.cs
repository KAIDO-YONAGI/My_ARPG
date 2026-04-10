using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyEnums;
public class ButtonSceneToggler : MonoBehaviour
{
    public SceneLoadEventSO loadEventSO;
    public GameSceneSO sceneToLoad;
    public CanvasGroup ButtonCanvas;
    public Vector3 newPosition;
    public bool isToFade = true;
    [Header("Retry Event")]
    public VoidEventSO retryEventSO;
    public void RaiseLoadRequestEvent()
    {
        ButtonCanvas.alpha = 0;
        ButtonCanvas.interactable = false;
        ButtonCanvas.blocksRaycasts = false;

        if (sceneToLoad.sceneName != "Retry")
        {
            loadEventSO.RaiseLoadRequestEvent(sceneToLoad, newPosition, isToFade);
            if (sceneToLoad.sceneType == SceneType.Menu)
            {
                TimeManager.instance.ForceResumeGame();
            }
        }
        else
        {
            retryEventSO.OnEventRaised();

        }
    }

}
