using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 经验与等级的门面：订阅击杀事件、把经验交给 <see cref="PlayerStatsModel"/> 结算，
/// 再刷新经验条与等级文本。
///
/// 升级规则（阈值、曲线、上限）不在这里，全部在 PlayerStatsModel.AddExp；
/// 本类只做"转发 + 显示"，并且把 Model 的升级事件转成对外的静态事件
/// <see cref="OnLevelUp"/>（SkillTreeManager 订阅它发技能点）。
/// </summary>
public class ExpManager : YSingleton<ExpManager>
{
    [SerializeField] private Slider expSlider;
    [SerializeField] private TMP_Text currentLevelText;

    /// <summary>升级广播：参数为本次升的级数。SkillTreeManager 订阅它来发技能点。</summary>
    public static event Action<int> OnLevelUp;

    private bool listeningToModel;

    private void OnEnable()
    {
        EnemyHealth.OnDefeated += GainExp;
        TrySubscribeModel();
    }

    private void OnDisable()
    {
        EnemyHealth.OnDefeated -= GainExp;
        UnsubscribeModel();
    }

    private void Start()
    {
        // OnEnable 可能早于 StatsManager.Awake，所以这里再补一次订阅；
        // 所有 Awake 都跑完之后 Start 才会执行，此时 StatsManager 必然已就绪。
        TrySubscribeModel();
        UpdateUI();
    }

    private void OnDestroy()
    {
        UnsubscribeModel();
    }

    private void TrySubscribeModel()
    {
        if (listeningToModel || StatsManager.Instance == null) return;

        listeningToModel = true;
        StatsManager.Instance.Model.LevelUp += HandleLevelUp;
    }

    private void UnsubscribeModel()
    {
        if (!listeningToModel) return;
        listeningToModel = false;

        if (StatsManager.Instance == null) return;
        StatsManager.Instance.Model.LevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int levelsGained) => OnLevelUp?.Invoke(levelsGained);

    public void GainExp(int amount)
    {
        StatsManager stats = StatsManager.Instance;
        if (stats == null) return;

        stats.AddExp(amount); // 升级判定与曲线在 Model 里
        UpdateUI();
    }

    public void UpdateUI()
    {
        StatsManager stats = StatsManager.Instance;
        if (stats == null) return;

        expSlider.maxValue = stats.Model.ExpToUpgrade;
        expSlider.value = stats.Model.CurrentExp;
        currentLevelText.text = "Level:" + stats.Model.Level;
    }
}
