using UnityEngine;

public class ButtonSceneToggler : MonoBehaviour
{
    [SerializeField] private GameSceneSO sceneToLoad;
    [SerializeField] private CanvasGroup ButtonCanvas;
    [SerializeField] private Vector3 newPosition;
    [SerializeField] private bool isToFade = true;

    public void HandleSceneToggle()//editor内由button组件绑定
    {
        ButtonCanvas.alpha = 0;
        ButtonCanvas.interactable = false;
        ButtonCanvas.blocksRaycasts = false;

        // 场景切换走 SceneChanger 的唯一入口；直接 Raise 事件只会通知订阅方，不会切场景
        SceneChanger.Instance.RequestSceneLoad(sceneToLoad, newPosition, isToFade);
    }
}
