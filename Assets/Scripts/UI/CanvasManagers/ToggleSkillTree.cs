using System.Collections;
using System.Collections.Generic;
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

        if (state)
        {
            skillsCanvas.alpha = 1;
            skillsCanvas.interactable = true;
            skillsCanvas.blocksRaycasts = true;

        }
        else
        {
            skillsCanvas.alpha = 0;
            skillsCanvas.interactable = false;
            skillsCanvas.blocksRaycasts = false;
        }

        UIManager.instance.ReportCanvasState(MyEnums.CanvasToToggle.Skills, state);
        int order = state && UIManager.instance != null &&
                    UIManager.instance.IsCanvasFocused(MyEnums.CanvasToToggle.Skills)
            ? UIManager.FocusOrder
            : UIManager.DefaultOrder;
        ((ICanvasManager)this).SetCanvaOrder(canvas, order);
    }
}
