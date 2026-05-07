using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StatsUI : MonoBehaviour, ICanvasManager
{
    public GameObject[] statsSlots;
    public CanvasGroup statsCanvas;

    public ToggleCanvasEventSO toggleStatsEvent;

    public ToggleCanvasEventSO ToggleCanvasEvent => toggleStatsEvent;
    private Canvas canvas;

    private void Awake()// Runs on instantiation, before Start().
    {
        statsCanvas.alpha = 0;
        canvas = statsCanvas.GetComponent<Canvas>();
    }

    private void Start()
    {
        UpdateAllStats();
    }

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
        UpdateAllStats();
        ((ICanvasManager)this).ToggleCanvas(
            statsCanvas,
            canvas,
            MyEnums.CanvasToToggle.Stats,
            state);
    }

    public void UpdateDamage()
    {
        statsSlots[0].GetComponentInChildren<TMP_Text>().text = "Damage:" + StatsManager.instance.GetDamage();
        // Note: Components is plural and returns an array.
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
