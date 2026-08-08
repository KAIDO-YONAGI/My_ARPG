using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyEnums;
public class ButtonSceneToggler : MonoBehaviour
{
    [SerializeField] private SceneLoadEventSO loadEventSO;
    [SerializeField] private GameSceneSO sceneToLoad;
    [SerializeField] private CanvasGroup ButtonCanvas;
    [SerializeField] private Vector3 newPosition;
    [SerializeField] private bool isToFade = true;
    [Header("Retry Event")]
    [SerializeField] private VoidEventSO retryEventSO;
    public void HandleSceneToggle()//editor内由button组件绑定
    {
        ButtonCanvas.alpha = 0;
        ButtonCanvas.interactable = false;
        ButtonCanvas.blocksRaycasts = false;

        if (sceneToLoad.sceneType != MyEnums.SceneType.Retry)
        {
            loadEventSO.RaiseLoadRequestEvent(sceneToLoad, newPosition, isToFade);
        }
        else
        {
            TimeManager.instance.ForceResumeGame();
            retryEventSO.OnEventRaised();//事件引用位于每个场景内RetryManager
        }
    }

}
