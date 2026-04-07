using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyEnums;
public class ButtonSceneToggler : MonoBehaviour
{
    public SceneLoadEventSO loadEventSO;
    [Tooltip("如果不指定场景，则默认重载当前场景")]
    public GameSceneSO sceneToLoad;//如果不指定场景，则默认重载当前场景
    public CanvasGroup ButtonCanvas;
    public Vector3 newPosition;

    public bool isToFade = true;
    public void RaiseLoadRequestEvent()
    {
        ButtonCanvas.alpha = 0;
        ButtonCanvas.interactable = false;
        ButtonCanvas.blocksRaycasts = false;

        if (sceneToLoad != null)
        {
            loadEventSO.RaiseLoadRequestEvent(sceneToLoad, newPosition, isToFade);
            if (sceneToLoad.sceneType == SceneType.Menu)
            {
                TimeManager.instance.ForceResumeGame();
            }
        }
        else
        {
            ReloadCurrentScene();
        }
    }
    public void ReloadCurrentScene()
    {
        sceneToLoad = SceneChanger.instance.GetCurrentSceneSO();
        loadEventSO.RaiseLoadRequestEvent(sceneToLoad, newPosition, isToFade);
    }
}
