using UnityEngine;

/// <summary>
/// 可挂接的菜单场景画布屏蔽组件。
/// 订阅场景加载完成事件（组合事件系统）：Menu 类场景（StartingMenu）中屏蔽目标 CanvasGroup
/// （alpha=0 + 不可交互 + 不拦截射线），非 Menu 场景恢复显示。
/// 与 ICanvasManager 解耦：需要屏蔽的画布对象自行挂接本组件并接好引用即可。
/// </summary>
public class MenuSceneCanvasHider : MonoBehaviour
{
    [Tooltip("要屏蔽的 CanvasGroup 组件（通常挂在本物体上）")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private SceneLoadEventSO sceneLoadEvent;
    [SerializeField] private VoidEventSO sceneLoadedEvent;

    private GameSceneSO sceneToLoad;

    private void OnEnable()
    {
        if (sceneLoadEvent != null)
            sceneLoadEvent.LoadRequestEvent += OnSceneLoadRequested;
        if (sceneLoadedEvent != null)
            sceneLoadedEvent.VoidEvent += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (sceneLoadEvent != null)
            sceneLoadEvent.LoadRequestEvent -= OnSceneLoadRequested;
        if (sceneLoadedEvent != null)
            sceneLoadedEvent.VoidEvent -= OnSceneLoaded;
    }

    private void OnSceneLoadRequested(GameSceneSO scene, Vector3 position, bool isToFade)
    {
        sceneToLoad = scene;
        Debug.Log("is menu:"+ (sceneToLoad.sceneType == MyEnums.SceneType.Menu));
    }

    private void OnSceneLoaded()
    {
        if (canvasGroup == null || sceneToLoad == null) return;

        bool visible = sceneToLoad.sceneType != MyEnums.SceneType.Menu;
        canvasGroup.alpha = visible ? 1 : 0;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
