using UnityEngine;

public class BackpackCanvasManager : MonoBehaviour, ICanvasManager
{
    [SerializeField] private CanvasGroup currentCanvas;
    [SerializeField] private Canvas canvas;
    [SerializeField] private ToggleCanvasEventSO toggleBackpackCanvasEventSO;
    [SerializeField] private SceneLoadedEventSO sceneLoadedEvent;

    public ToggleCanvasEventSO ToggleCanvasEvent => toggleBackpackCanvasEventSO;
    public SceneLoadedEventSO SceneLoadedEvent => sceneLoadedEvent;

    private void OnEnable()
    {
        toggleBackpackCanvasEventSO.toggleCanvasEvent += OnToggleBackpack;
        toggleBackpackCanvasEventSO.focusEvent += OnFocus;
        sceneLoadedEvent.SceneLoadedEvent += OnSceneLoaded;
    }

    private void OnDisable()
    {
        toggleBackpackCanvasEventSO.toggleCanvasEvent -= OnToggleBackpack;
        toggleBackpackCanvasEventSO.focusEvent -= OnFocus;
        sceneLoadedEvent.SceneLoadedEvent -= OnSceneLoaded;
    }

    private void OnSceneLoaded(GameSceneSO _)
    {
        ((ICanvasManager)this).SetCanvaInactive(currentCanvas, MyEnums.CanvasToToggle.Backpack);
    }

    private void OnToggleBackpack(bool state)
    {
        ((ICanvasManager)this).ToggleCanvas(currentCanvas, canvas, MyEnums.CanvasToToggle.Backpack, state);
    }

    private void OnFocus()
    {
        ((ICanvasManager)this).RefreshCanvaOrder(canvas, MyEnums.CanvasToToggle.Backpack, true);
    }
}
