using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class StatsUI : MonoBehaviour, ICanvasManager
{
    public GameObject[] statsSlots;
    public CanvasGroup statsCanvas;

    public ToggleCanvasEventSO toggleStatsEvent;

    public ToggleCanvasEventSO ToggleCanvasEvent => toggleStatsEvent;
    private int order = 0;
    private Canvas canvas;
    private void Start()
    {
        canvas = statsCanvas.GetComponent<Canvas>();
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

        if (state)
        {
            statsCanvas.alpha = 1;
            statsCanvas.interactable = true;
            statsCanvas.blocksRaycasts = true;

        }
        else
        {
            statsCanvas.alpha = 0;
            statsCanvas.interactable = false;
            statsCanvas.blocksRaycasts = false;
        }

        UIManager.instance.ReportCanvasState(MyEnums.CanvasToToggle.Stats, state);
        UpdateAllStats();

        int order = state && UIManager.instance != null &&
                    UIManager.instance.IsCanvasFocused(MyEnums.CanvasToToggle.Stats)
            ? UIManager.FocusOrder
            : UIManager.DefaultOrder;
        ((ICanvasManager)this).SetCanvaOrder(canvas, order);
    }
    private void Awake()//对象实例化就会进行，先于Start()
    {
        statsCanvas.alpha = 0; ;
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
