using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Gameplay.Player.Models;
using Gameplay.Player.Services;

public class StatsCanvasManager : YSingleton<StatsCanvasManager>, ICanvasManager
{
    [SerializeField] private TMP_Text[] statTexts;
    [SerializeField] private CanvasGroup statsCanvas;
    [SerializeField] private Canvas canvas;

    [SerializeField] private ToggleCanvasEventSO toggleStatsEvent;
    [SerializeField] private SceneLoadedEventSO sceneLoadedEvent;

    public ToggleCanvasEventSO ToggleCanvasEvent => toggleStatsEvent;
    public SceneLoadedEventSO SceneLoadedEvent => sceneLoadedEvent;

    // 运行时绑定 StatsService 的数值模型（事件在模型上）；初始绑定放在 Start：
    // 面板可能随 GamePlay 根节点延迟激活，那时 StatsService 必然已就绪。
    private PlayerStatsModel model;

    protected override void OnSingletonInitialized()
    {
        statsCanvas.alpha = 0;
    }

    private void Start()
    {
        model = StatsService.Instance.Model;
        model.StatsChanged += UpdateAllStats;
        UpdateAllStats();
    }

    private void OnEnable()
    {
        // 重激活时补一次刷新：失活期间错过的事件没有累积通知
        if (model != null)
            UpdateAllStats();

        toggleStatsEvent.toggleCanvasEvent += OnToggleStatsEvent;
        toggleStatsEvent.focusEvent += OnFocus;
        if (sceneLoadedEvent != null)
            sceneLoadedEvent.SceneLoadedEvent += OnSceneLoaded;
    }

    private void OnDisable()
    {
        toggleStatsEvent.toggleCanvasEvent -= OnToggleStatsEvent;
        toggleStatsEvent.focusEvent -= OnFocus;
        if (sceneLoadedEvent != null)
            sceneLoadedEvent.SceneLoadedEvent -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (model != null)
            model.StatsChanged -= UpdateAllStats;
    }

    private void OnSceneLoaded(GameSceneSO _)
    {
        ((ICanvasManager)this).SetCanvaInactive(statsCanvas, MyEnums.CanvasToToggle.Stats);
    }

    private void OnToggleStatsEvent(bool state)
    {
        UpdateAllStats();
        ((ICanvasManager)this).ToggleCanvas(statsCanvas, canvas, MyEnums.CanvasToToggle.Stats, state);
    }

    private void OnFocus()
    {
        if (!canvasIsActive()) return;
        ((ICanvasManager)this).RefreshCanvaOrder(canvas, MyEnums.CanvasToToggle.Stats, true);
    }

    private bool canvasIsActive()
    {
        return statsCanvas.alpha > 0;
    }

    public void UpdateDamage()
    {
        if (model == null || statTexts == null || statTexts.Length < 1 || statTexts[0] == null) return;
        statTexts[0].text = "Damage:" + model.Damage;
    }

    public void UpdateSpeed()
    {
        if (model == null || statTexts == null || statTexts.Length < 2 || statTexts[1] == null) return;
        statTexts[1].text = "Speed:" + model.Speed;
    }

    public void UpdateAllStats()
    {
        if (model == null) return;
        UpdateDamage();
        UpdateSpeed();
    }
}
