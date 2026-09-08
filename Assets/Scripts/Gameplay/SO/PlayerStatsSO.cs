using System;
using UnityEngine;

/// <summary>
/// 玩家数值唯一数据源。数值变化时广播事件，UI 订阅被动刷新；
/// StatsManager 只作为对外兼容的转发层（重构清单 4.3）。
/// </summary>
[CreateAssetMenu(fileName = "PlayerStatsSO", menuName = "Data/PlayerStatsSO", order = 0)]
public class PlayerStatsSO : ScriptableObject
{
    [SerializeField] private PlayerStatsData stats = new();

    /// <summary>血量/上限变化（HealthCanvasManager 订阅）。</summary>
    public event Action HealthChanged;
    /// <summary>速度/伤害等属性变化（StatsCanvasManager 订阅）。</summary>
    public event Action StatsChanged;

    public PlayerStatsData Data => stats;

    public void LoadStats(PlayerStatsData data)
    {
        stats = data;
        HealthChanged?.Invoke();
        StatsChanged?.Invoke();
    }

    public void UpdateMaxHealth(int amount)
    {
        stats.maxHealth += amount;
        if (stats.maxHealth < 1) stats.maxHealth = 1;
        HealthChanged?.Invoke();
    }

    public void UpdateHealth(int amount)
    {
        stats.currentHealth = Mathf.Clamp(stats.currentHealth + amount, 0, stats.maxHealth);
        HealthChanged?.Invoke();
    }

    public void SetCurrentHealth(int value)
    {
        stats.currentHealth = Mathf.Clamp(value, 0, stats.maxHealth);
        HealthChanged?.Invoke();
    }

    public void UpdateSpeed(float amount)
    {
        stats.speed += amount;
        StatsChanged?.Invoke();
    }

    public void UpdateDamage(int amount)
    {
        stats.damage += amount;
        StatsChanged?.Invoke();
    }

    public void UpdateSkillPoints(int amount) => stats.skillPoints += amount;
}
