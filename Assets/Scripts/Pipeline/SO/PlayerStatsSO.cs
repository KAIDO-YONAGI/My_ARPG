using UnityEngine;
using Gameplay.Player.Models;

/// 这个资产保存玩家数值的初始值。运行时通过 CreateInitialData() 取一份拷贝放进
/// <see cref="PlayerStatsModel"/>，Play 期间资产内容保持不变。
/// 数值的状态、规则与事件位于 PlayerStatsModel。
[CreateAssetMenu(fileName = "PlayerStatsSO", menuName = "Data/PlayerStatsSO", order = 0)]
public class PlayerStatsSO : ScriptableObject
{
    
    [SerializeField] private PlayerStatsData stats = new();

    /// <summary>资产中保存的初始值。运行时使用 CreateInitialData() 导出的拷贝。</summary>
    public PlayerStatsData Data => stats;

    /// <summary>导出初始值的一份拷贝，供运行时模型使用。</summary>
    public PlayerStatsData CreateInitialData() => stats.Clone();
}
