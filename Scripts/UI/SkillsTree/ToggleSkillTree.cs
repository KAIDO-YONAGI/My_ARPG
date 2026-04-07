using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleSkillTree : MonoBehaviour
{
    public CanvasGroup statsCanvas;
    private bool skillTreeIsOpen=false;

    private void Update()
    {
        if (Input.GetButtonDown("ToggleSkillTree"))
        {
            if(skillTreeIsOpen)
            {
                TimeManager.instance.ResumeGame();
                statsCanvas.alpha = 0;
                statsCanvas.blocksRaycasts = false; 
                skillTreeIsOpen=false;
            }
            else
            {
                TimeManager.instance.PauseGame();
                statsCanvas.alpha = 1;
                statsCanvas.blocksRaycasts = true;
                skillTreeIsOpen=true;

            }
        }
    }
}
