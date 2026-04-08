using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleSkillTree : MonoBehaviour
{
    public CanvasGroup skillsCanvas;
    private bool skillTreeIsOpen=false;

    private void Update()
    {
        if (Input.GetButtonDown("ToggleSkillTree"))
        {
            if(skillTreeIsOpen)
            {
                TimeManager.instance.ResumeGame();
                skillsCanvas.alpha = 0;
                skillsCanvas.blocksRaycasts = false; 
                skillsCanvas.interactable = false;
                skillTreeIsOpen=false;
            }
            else
            {
                TimeManager.instance.PauseGame();
                skillsCanvas.alpha = 1;
                skillsCanvas.blocksRaycasts = true;
                skillsCanvas.interactable = true;
                skillTreeIsOpen=true;

            }
        }
    }
}
