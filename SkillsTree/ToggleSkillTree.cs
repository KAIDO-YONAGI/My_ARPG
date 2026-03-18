using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleSkillTree : MonoBehaviour
{
    public CanvasGroup statsCanvas;
    private bool skillTreeIsOpen=false;

    private void Update()
    {
        if (Input.GetButtonDown("ToggleSkillTree")) //ÓÃÓÚÇÐ»»×´Ì¬
        {
            if(skillTreeIsOpen)
            {
                Time.timeScale = 1;
                statsCanvas.alpha = 0;
                statsCanvas.blocksRaycasts = false; 
                skillTreeIsOpen=false;
            }
            else
            {
                Time.timeScale = 0;
                statsCanvas.alpha = 1;
                statsCanvas.blocksRaycasts = true;
                skillTreeIsOpen=true;

            }
        }
    }
}
