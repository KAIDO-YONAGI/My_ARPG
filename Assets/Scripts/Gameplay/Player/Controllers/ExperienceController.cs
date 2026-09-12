using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 经验与等级这条线的控制器（Controller）：订阅击杀事件 → 把经验交给 <see cref="StatsService"/> 结算
/// → 驱动 <see cref="ExperienceView"/> 刷新经验条与等级文本。
///
/// 升级规则（阈值、曲线、上限）全在 PlayerStatsModel.AddExp，本类不判定；
/// 发技能点也不经过这里：SkillTreeManager 直接订阅 PlayerStatsModel.LevelUp。
/// </summary>
public class ExperienceController : YSingleton<ExperienceController>
{
    [SerializeField] private Slider expSlider;
    [SerializeField] private TMP_Text currentLevelText;

    private ExperienceView view;

    private void OnEnable()
    {
        EnemyHealth.OnDefeated += GainExp;
    }

    private void OnDisable()
    {
        EnemyHealth.OnDefeated -= GainExp;
    }

    private void Start()
    {
        // OnEnable 可能早于 StatsService.Awake，但所有 Awake 跑完才会进 Start，
        // 那时 StatsService 必然已就绪，可以安全刷一次初始显示。
        UpdateUI();
    }

    public void GainExp(int amount)
    {
        StatsService stats = StatsService.Instance;
        if (stats == null) return;

        stats.AddExp(amount); // 升级判定与曲线在 Model 里
        UpdateUI();
    }

    public void UpdateUI()
    {
        StatsService stats = StatsService.Instance;
        if (stats == null) return;

        // 面板可能延迟激活、字段也可能没接线，所以视图用到时才构造（内部对 null 字段有兜底）。
        view ??= new ExperienceView(expSlider, currentLevelText);
        view.SetExp(stats.Model.CurrentExp, stats.Model.ExpToUpgrade, stats.Model.Level);
    }
}