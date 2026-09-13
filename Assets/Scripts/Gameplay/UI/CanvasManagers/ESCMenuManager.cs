using UnityEngine;

public class ESCMenuManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup ESCGroup;
    [SerializeField] private ToggleCanvasEventSO toggleESCEvent;
    [SerializeField] private SceneLoadedEventSO sceneLoadedEvent;

    private GameSceneSO _currentScene;

    private void OnEnable()
    {
        toggleESCEvent.toggleCanvasEvent += OnESC;
        sceneLoadedEvent.SceneLoadedEvent += OnSceneLoaded;
    }

    private void OnDisable()
    {
        toggleESCEvent.toggleCanvasEvent -= OnESC;
        sceneLoadedEvent.SceneLoadedEvent -= OnSceneLoaded;
    }

    private void OnSceneLoaded(GameSceneSO sceneLoaded)
    {
        _currentScene = sceneLoaded;
        OnESC(false);
    }

    private void OnESC(bool state)
    {
        if (_currentScene == null || _currentScene.sceneType == MyEnums.SceneType.Menu) return;
        if (state)
        {
            TimeManager.Instance.PauseGame();
            ESCGroup.alpha = 1;
            ESCGroup.interactable = true;
            ESCGroup.blocksRaycasts = true;
        }
        else
        {
            TimeManager.Instance.ResumeGame();
            ESCGroup.alpha = 0;
            ESCGroup.interactable = false;
            ESCGroup.blocksRaycasts = false;
        }

        UIManager.Instance.ReportCanvasState(MyEnums.CanvasToToggle.ESC, state);
    }
}