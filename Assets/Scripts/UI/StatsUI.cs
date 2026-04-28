using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StatsUI : MonoBehaviour
{
    public GameObject[] statsSlots;
    public CanvasGroup statsCanvas;

    public ToggleCanvasEventSO toggleStatsEvent;
    private void OnEnable()
    {
        toggleStatsEvent.toggleCanvasEvent += OnToggleStatsEvent;
    }
    private void OnDisable()
    {
        toggleStatsEvent.toggleCanvasEvent -= OnToggleStatsEvent;

    }
    private void OnToggleStatsEvent(bool state)
    {

        if (state)
        {
            TimeManager.instance.PauseGame();
            statsCanvas.alpha = 1;
            statsCanvas.interactable = true;
            statsCanvas.blocksRaycasts = true;

        }
        else
        {
            TimeManager.instance.ResumeGame();
            statsCanvas.alpha = 0;
            statsCanvas.interactable = false;
            statsCanvas.blocksRaycasts = false;
        }
        UpdateAllStats();
    }
    private void Awake()//对象实例化就会进行，先于Start()
    {
        statsCanvas.alpha = 0; ;
    }
    private void Start()
    {
        UpdateAllStats();
    }

    public void UpdateDamage()
    {
        statsSlots[0].GetComponentInChildren<TMP_Text>().text = "Damage:" + StatsManager.instance.GetDamage();
        //注意components是复数，会导致返回一个数组，不要拼错了
    }
    public void UpdateSpeed()
    {
        statsSlots[1].GetComponentInChildren<TMP_Text>().text = "Speed:" + StatsManager.instance.GetSpeed();
    }

    public void UpdateAllStats()
    {
        UpdateDamage();
        UpdateSpeed();
    }
}
