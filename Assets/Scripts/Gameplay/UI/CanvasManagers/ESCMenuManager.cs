using UnityEngine;

public class ESCMenuManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup ESCGroup;
    [SerializeField] private ToggleCanvasEventSO toggleESCEvent;
    [SerializeField] private SceneLoadedEventSO sceneLoadedEvent;

    private void OnEnable()
    {
        toggleESCEvent.toggleCanvasEvent += OnESC;
        if (sceneLoadedEvent != null)
            sceneLoadedEvent.SceneLoadedEvent += OnSceneLoaded;
    }
    private void OnDisable()
    {
        toggleESCEvent.toggleCanvasEvent -= OnESC;
        if (sceneLoadedEvent != null)
            sceneLoadedEvent.SceneLoadedEvent -= OnSceneLoaded;
    }

    private void OnSceneLoaded(GameSceneSO _)
    {
        OnESC(false);
    }

    private void OnESC(bool state)
    {

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
