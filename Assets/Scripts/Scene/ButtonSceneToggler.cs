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
            Debug.Log("1");
        }
        else
        {
            TimeManager.instance.ForceResumeGame();
            retryEventSO.OnEventRaised();//RetryManager位于每个场景内
            Debug.Log("2");
        }
    }

}
