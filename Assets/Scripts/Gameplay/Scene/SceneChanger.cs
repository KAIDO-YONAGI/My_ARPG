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

    [SerializeField] private SceneLoadedEventSO sceneLoadedEvent;
    [SerializeField] private Animator[] transitionImagesDuringFade;
    [SerializeField] private Object[] objectsToUnableWhileMenuOrReset;

    private GameSceneSO currentScene;

    /// <summary>已加载的场景对象</summary>
    private Scene loadedScene;

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
        SetPlayerPostion(initialPosition);
    }

    /// <summary>
    /// 启用时订阅场景加载事件
    /// </summary>
    private void OnEnable()
    {
        loadEventSO.LoadRequestEvent += OnLoadRequestEvent;
        loadEventSO.RaiseLoadRequestEvent(initScene, Vector3.zero, false);
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
        if (scene == null)
        {
            Debug.LogWarning("[SceneChanger] 收到空场景加载请求，已忽略。");
            return;
        }

        // 加载窗口内拒绝新请求，避免同一时间启动多条卸载/加载协程。
        if (isLoading)
        {
            Debug.LogWarning($"[SceneChanger] 加载进行中，忽略新的加载请求: {scene.name}");
            return;
        }

        isLoading = true;

        ForbidInput();
        TimeManager.Instance.PauseGame();

        StatsManager.Instance.Respawn(); //回血

        Vector3 targetPosition = newPosition == Vector3.zero ? scene.initialPosition : newPosition;
        //如果传入位置为零向量，则使用场景预设的初始位置
        if (isToFade && !isInitialScene)
        {
            PlayLoadingAnimation("FadeIn");
        }

        StartCoroutine(UnloadAndLoadNew(scene, targetPosition, isToFade)); //卸载当前场景
    }

    /// <summary>
    /// 卸载当前场景协程
    /// 等待淡入动画完成后卸载旧场景，然后加载新场景
    /// </summary>
    /// <param name="targetScene">要加载的目标场景</param>
    /// <param name="targetPosition">玩家在目标场景中的位置</param>
    /// <param name="targetFade">是否播放过渡动画</param>
    private IEnumerator UnloadAndLoadNew(
        GameSceneSO targetScene,
        Vector3 targetPosition,
        bool targetFade)
    {
        yield return new WaitForSecondsRealtime(fadeDuration);

        if (currentScene != null)
            yield return currentScene.sceneReference.UnLoadScene();
        LoadScene(targetScene, targetFade); //这里是事件响应的合法调用 并非裸调用
        SetPlayerPostion(targetPosition);
    }

    /// <summary>
    /// 异步加载场景，禁止裸调用 需要走事件 否则订阅者收不到信息
    /// 使用 Addressables 加载场景，以 additive 模式添加
    /// </summary>
    /// <param name="targetScene">要加载的目标场景</param>
    /// <param name="targetFade">是否播放过渡动画</param>
    private void LoadScene(GameSceneSO targetScene, bool targetFade)
    {
        if (targetScene == null) return;

        if (targetScene.sceneType == MyEnums.SceneType.Menu)
            SetObjects(false);

        else if (targetScene.sceneType == MyEnums.SceneType.Location)
            SetObjects(true);
        // 闭包捕获本次目标（参数）：防止加载期间字段被覆写导致完成回调记录错场景
        var loadingOption = targetScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive);
        loadingOption.Completed += handle => OnLoadCompleted(handle, targetScene, targetFade);
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
    /// <param name="loadedTarget">本次实际加载的场景</param>
    /// <param name="targetFade">本次加载是否需要播放淡出动画</param>
    private void OnLoadCompleted(
        AsyncOperationHandle<SceneInstance> handle,
        GameSceneSO loadedTarget,
        bool targetFade)
    {
        // 不变量：currentScene 赋值必须在 sceneLoadedEvent 广播之前——
        // 订阅方（画布管理器/DataManager/MenuSceneCanvasHider 等）经 GetCurrentGameScene() 回读
        currentScene = loadedTarget;
        loadedScene = handle.Result.Scene;
        if (targetFade && !isInitialScene)
        {
            PlayLoadingAnimation("FadeOut");
        }

        isInitialScene = false;
        sceneLoadedEvent?.RaiseSceneLoadedEvent(currentScene);
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
