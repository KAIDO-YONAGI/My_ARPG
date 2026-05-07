using UnityEngine;

public class ToggleSkillTree : MonoBehaviour, ICanvasManager
{
    public CanvasGroup skillsCanvas;
    public ToggleCanvasEventSO toggleSkillEvent;
    public ToggleCanvasEventSO ToggleCanvasEvent => toggleSkillEvent;
    private Canvas canvas;

    private void Awake()
    {
        canvas = skillsCanvas.GetComponent<Canvas>();
    }

    private void OnEnable()
    {
        toggleSkillEvent.toggleCanvasEvent += OnToggleSkillEvent;
    }
    private void OnDisable()
    {
        toggleSkillEvent.toggleCanvasEvent -= OnToggleSkillEvent;

    }
    private void OnToggleSkillEvent(bool state)
    {
        ((ICanvasManager)this).ToggleCanvas(
            skillsCanvas,
            canvas,
            MyEnums.CanvasToToggle.Skills,
            state);
    }
}
