using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

/// <summary>
/// 场景切换管理器
/// 负责管理场景的加载、卸载和过渡动画
/// 使用单例模式，通过事件响应场景切换请求
/// </summary>
public class SceneChanger : YSingleton<SceneChanger>
{

    /// <summary>玩家初始位置</summary>
    [SerializeField] private Vector3 initialPosition = Vector3.zero;

    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private GameSceneSO initScene;
    [SerializeField] private GameObject player;
    [SerializeField] private CanvasGroup fadeCanva;

    /// <summary>过渡动画播放器数组</summary>
    ///     
    [Header("Events")] [SerializeField] private SceneLoadEventSO loadEventSO;

    [SerializeField] private VoidEventSO sceneLoadedEvent;
    [SerializeField] private Animator[] transitionImagesDuringFade;
    [SerializeField] private Object[] objectsToUnableWhileMenuOrReset;
    private GameSceneSO sceneToLoad;

    private GameSceneSO currentScene;

    /// <summary>已加载的场景对象</summary>
    private Scene loadedScene;

    /// <summary>玩家新位置</summary>
    private Vector3 newPosition;

    /// <summary>是否需要淡入淡出</summary>
    private bool isToFade;

    private bool isInitialScene = true;

    /// <summary>加载进行中标记：请求→淡入→卸载→加载→完成的整个窗口内为 true，防止双请求竞态。</summary>
    private bool isLoading;

    /// <summary>
    /// 获取当前活动场景
    /// </summary>
    /// <returns>当前场景对象</returns>
    public Scene GetCurrentScene()
    {
        return loadedScene != null ? loadedScene : SceneManager.GetActiveScene();
    }

    // 供存档系统在切场前读取当前场景 SO。
    public GameSceneSO GetCurrentGameScene()
    {
        return currentScene;
    }

    /// <summary>
    /// 唤醒时初始化单例并加载首个场景
    /// </summary>
    protected override void OnSingletonInitialized()
    {
        sceneToLoad = initScene;
        SetPlayerPostion(initialPosition);
        LoadScene(sceneToLoad);
    }

    /// <summary>
    /// 启用时订阅场景加载事件
    /// </summary>
    private void OnEnable()
    {
        loadEventSO.LoadRequestEvent += OnLoadRequestEvent;
    }

    /// <summary>
    /// 禁用时取消订阅场景加载事件
    /// </summary>
    private void OnDisable()
    {
        loadEventSO.LoadRequestEvent -= OnLoadRequestEvent;
    }

    /// <summary>
    /// 播放过渡动画
    /// </summary>
    /// <param name="name">动画状态名称（FadeIn/FadeOut）</param>
    private void PlayLoadingAnimation(string name)
    {
        fadeCanva.alpha = 1;
        foreach (Animator transitionImage in transitionImagesDuringFade)
        {
            if (transitionImage != null)
            {
                transitionImage.Play(name);
            }
        }
    }

    /// <summary>
    /// 设置玩家位置
    /// </summary>
    /// <param name="newPosition">新位置坐标</param>
    private void SetPlayerPostion(Vector3 newPosition)
    {
        player.transform.position = newPosition;
    }

    /// <summary>
    /// 场景加载请求事件回调
    /// </summary>
    /// <param name="scene">目标场景</param>
    /// <param name="newPosition">玩家新位置</param>
    /// <param name="isToFade">是否显示过渡动画</param>
    private void OnLoadRequestEvent(GameSceneSO scene, Vector3 newPosition, bool isToFade)
    {
        // 加载窗口内拒绝新请求：否则第二个请求会覆写 sceneToLoad 字段、
        // 并在首个流程未结束时再启动一条卸载协程（双协程 + 完成时记录错场景）
        if (isLoading)
        {
            Debug.LogWarning($"[SceneChanger] 加载进行中，忽略新的加载请求: {scene.name}");
            return;
        }
        isLoading = true;

        ForbidInput();
        TimeManager.Instance.PauseGame();
        sceneToLoad = scene;

        StatsManager.Instance.Respawn(); //回血


        this.newPosition = newPosition == Vector3.zero ? sceneToLoad.initialPosition : newPosition;
        //如果传入位置为零向量，则使用场景预设的初始位置
        this.isToFade = isToFade;
        if (isToFade && !isInitialScene)
        {
            PlayLoadingAnimation("FadeIn");
        }

        StartCoroutine(UnloadCurrentScene(sceneToLoad)); //卸载当前场景
    }

    /// <summary>
    /// 卸载当前场景协程
    /// 等待淡入动画完成后卸载旧场景，然后加载新场景
    /// </summary>
    /// <param name="sceneToLoad">要加载的目标场景</param>
    private IEnumerator UnloadCurrentScene(GameSceneSO sceneToLoad)
    {
        yield return new WaitForSecondsRealtime(fadeDuration);

        if (currentScene != null)
            yield return currentScene.sceneReference.UnLoadScene();
        LoadScene(sceneToLoad);
        SetPlayerPostion(newPosition);
    }

    /// <summary>
    /// 异步加载场景
    /// 使用 Addressables 加载场景，以 additive 模式添加
    /// </summary>
    /// <param name="sceneToLoad">要加载的场景</param>
    private void LoadScene(GameSceneSO sceneToLoad)
    {
        if (sceneToLoad.sceneType == MyEnums.SceneType.Menu)
        {
            SetObjects(false);
        }

        else if (sceneToLoad.sceneType == MyEnums.SceneType.Location)
        {
            SetObjects(true);
        }

        if (sceneToLoad != null)
        {
            // 闭包捕获本次目标（参数）：防止加载期间字段被覆写导致完成回调记录错场景
            var target = sceneToLoad;
            var loadingOption = target.sceneReference.LoadSceneAsync(LoadSceneMode.Additive);
            loadingOption.Completed += handle => OnLoadCompleted(handle, target);
        }
    }

    private void SetObjects(bool state)
    {
        foreach (Object obj in objectsToUnableWhileMenuOrReset)
        {
            if (obj is GameObject go)
            {
                go.SetActive(state);
            }
        }
    }

    /// <summary>
    /// 场景加载完成回调
    /// 更新当前场景引用，播放淡出动画
    /// </summary>
    /// <param name="handle">异步操作句柄</param>
    private void OnLoadCompleted(AsyncOperationHandle<SceneInstance> handle, GameSceneSO loadedTarget)
    {
        // 不变量：currentScene 赋值必须在 sceneLoadedEvent 广播之前——
        // 订阅方（画布管理器/DataManager/MenuSceneCanvasHider 等）经 GetCurrentGameScene() 回读
        currentScene = loadedTarget;
        loadedScene = handle.Result.Scene;
        if (isToFade && !isInitialScene)
        {
            PlayLoadingAnimation("FadeOut");
        }

        isInitialScene = false;
        sceneLoadedEvent?.OnEventRaised();
        AllowInput();
        TimeManager.Instance.ForceResumeGame();
        isLoading = false; // 全部完成后才解锁，允许下一次加载请求
    }

    private void ForbidInput()
    {
        if (player == null) return;
        var movement = PlayerMovement.Main;
        if (movement != null) movement.enabled = false;
    }

    private void AllowInput()
    {
        if (player == null) return;
        var movement = PlayerMovement.Main;
        if (movement != null) movement.enabled = true;
    }
}
