using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleSkillTree : MonoBehaviour
{
    public CanvasGroup skillsCanvas;
    public ToggleCanvasEventSO toggleSkillEvent;
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
    }
}
