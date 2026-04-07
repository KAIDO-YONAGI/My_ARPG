using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ESCMenuManager : MonoBehaviour
{
    public CanvasGroup ESCGroup;
    void Update()
    {
        if (Input.GetButtonDown("ESC"))
        {
            if (TimeManager.instance.IsGamePaused())
            {
                TimeManager.instance.ResumeGame();
                ESCGroup.alpha = 0;
                ESCGroup.interactable = false;
                ESCGroup.blocksRaycasts = false;
            }
            else
            {
                TimeManager.instance.PauseGame();
                ESCGroup.alpha = 1;
                ESCGroup.interactable = true;
                ESCGroup.blocksRaycasts = true;
            }
        }
    }
}
