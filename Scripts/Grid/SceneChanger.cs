using UnityEngine.AddressableAssets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class SceneChanger : MonoBehaviour
{
    public Vector3 initialPosition = Vector3.zero;
    public float fadeDuration = 1f;
    public GameSceneSO firstScene;
    public GameObject player;
    public CanvasGroup fadeCanva;
    public Animator[] transitionImages;
    [Header("Events")]
    public SceneLoadEventSO loadEventSO;

    private GameSceneSO sceneToLoad;

    private GameSceneSO currentScene;

    private Vector3 newPosition;
    private bool isToFade;
    private bool isInitialScene = true;

    private void Awake()
    {
        sceneToLoad = firstScene;
        SetPlayerPostion(initialPosition);
        LoadScene(sceneToLoad);
    }
    private void OnEnable()
    {
        loadEventSO.LoadRequestEvent += OnLoadRequestEvent;
    }
    private void OnDisable()
    {
        loadEventSO.LoadRequestEvent -= OnLoadRequestEvent;
    }
    private void PlayLoadingAnimation(string name)
    {
        fadeCanva.alpha=1;
        foreach (Animator transitionImage in transitionImages)
        {
            if (transitionImage != null)
            {
                transitionImage.Play(name);
            }
        }
    }
    private void SetPlayerPostion(Vector3 newPosition)
    {
        player.GetComponent<Transform>().position = newPosition;
    }
    private void OnLoadRequestEvent(GameSceneSO scene, Vector3 newPosition, bool isToFade)
    {
        sceneToLoad = scene;
        this.newPosition = newPosition;
        this.isToFade = isToFade;
        if (isToFade && !isInitialScene)
        {
            PlayLoadingAnimation("FadeIn");
        }
        StartCoroutine(UnloadCurrentScene(sceneToLoad));//卸载当前场景
    }

    private IEnumerator UnloadCurrentScene(GameSceneSO sceneToLoad)
    {

        yield return new WaitForSeconds(fadeDuration);

        if (currentScene != null)
            yield return currentScene.sceneReference.UnLoadScene();
        LoadScene(sceneToLoad);
        SetPlayerPostion(newPosition);

    }

    private void LoadScene(GameSceneSO sceneToLoad)
    {
        if (sceneToLoad != null)
        {
            var loadingOption = sceneToLoad.sceneReference.LoadSceneAsync(LoadSceneMode.Additive);
            loadingOption.Completed += OnLoadCompleted;
        }
    }

    /// <summary>
    /// 场景加载完成
    /// </summary>
    /// <param name="newPosition"></param>
    private void OnLoadCompleted(AsyncOperationHandle<SceneInstance> handle)
    {
        currentScene = sceneToLoad;
        if (isToFade && !isInitialScene)
        {
            PlayLoadingAnimation("FadeOut");
        }
        isInitialScene = false;
    }


}
