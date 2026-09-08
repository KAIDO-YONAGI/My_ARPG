using UnityEngine;

public class ToggleSkillTree : MonoBehaviour, ICanvasManager
{
    [SerializeField] private CanvasGroup skillsCanvas;
    [SerializeField] private Canvas canvas;
    [SerializeField] private ToggleCanvasEventSO toggleSkillEvent;
    [SerializeField] private VoidEventSO sceneLoadedEvent;
    public ToggleCanvasEventSO ToggleCanvasEvent => toggleSkillEvent;
    public VoidEventSO SceneLoadedEvent => sceneLoadedEvent;

    private void OnEnable()
    {
        toggleSkillEvent.toggleCanvasEvent += OnToggleSkillEvent;
        toggleSkillEvent.focusEvent += OnFocus;
        if (sceneLoadedEvent != null)
            sceneLoadedEvent.VoidEvent += OnSceneLoaded;
    }
    private void OnDisable()
    {
        toggleSkillEvent.toggleCanvasEvent -= OnToggleSkillEvent;
        toggleSkillEvent.focusEvent -= OnFocus;
        if (sceneLoadedEvent != null)
            sceneLoadedEvent.VoidEvent -= OnSceneLoaded;

    }
    private void OnSceneLoaded()
    {
        ((ICanvasManager)this).SetCanvaInactive(skillsCanvas, MyEnums.CanvasToToggle.Skills);
    }
    private void OnToggleSkillEvent(bool state)
    {
        ((ICanvasManager)this).ToggleCanvas(skillsCanvas, canvas, MyEnums.CanvasToToggle.Skills, state);
    }
    private void OnFocus()
    {
        ((ICanvasManager)this).RefreshCanvaOrder(canvas, MyEnums.CanvasToToggle.Skills, true);
    }
}
