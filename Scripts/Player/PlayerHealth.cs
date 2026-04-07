using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using TMPro;
public class PlayerHealth : MonoBehaviour
{
    public CanvasGroup GameOverCanvas;
    void Start()
    {
        StatsManager.instance.UpdateHealth(StatsManager.instance.GetMaxHealth());
    }
    public void ChangeHealth(int amount)
    {
        StatsManager.instance.UpdateHealth(amount);

        if (StatsManager.instance.GetCurrentHealth() <= 0)
        {
            gameObject.SetActive(false);
            GameOverCanvas.alpha = 1;
            GameOverCanvas.interactable = true;
            GameOverCanvas.blocksRaycasts = true;
        }
    }
}
