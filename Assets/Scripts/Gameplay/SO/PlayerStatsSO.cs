using UnityEngine;

/// 运行时不再使用这个资产对象本身：StatsService 启动时把它的数据拷一份进
/// <see cref="PlayerStatsModel"/>，之后所有读写都发生在 Model 上。因此
/// Play 期间不会污染资产落盘，也不需要再"克隆一个 SO 当运行时容器"。
/// 规则与事件见 PlayerStatsModel；数值的唯一写入口也是它。
[CreateAssetMenu(fileName = "PlayerStatsSO", menuName = "Data/PlayerStatsSO", order = 0)]
public class PlayerStatsSO : ScriptableObject
{
    [SerializeField] private PlayerStatsData stats = new();

    /// <summary>模板里的初始值（只读用途；不要把它交给运行时当状态容器）。</summary>
    public PlayerStatsData Data => stats;

    /// <summary>导出初始值的一份拷贝，供运行时模型使用。</summary>
    public PlayerStatsData CreateInitialData() => stats.Clone();
}
