using UnityEngine;
using System;

[Serializable]
public class PlayerStatsData
{
    public int damage;
    public float weaponRange;
    public float knockBackForce;
    public float knockBackTime;
    public float stunTime;
    public float coolDown;
    public float speed;
    public int maxHealth;
    public int currentHealth;
    public int skillPoints;
    public int level;
    public int currentExp;
    public int expToUpgrade;
    public float expMultiplier;
}

/// <summary>
/// 数值转发层：数据与广播都在 PlayerStatsSO，本类只保留既有对外 API 兼容调用方。
/// 运行时使用资产副本（RuntimeStats）：Inspector 里配置的 statsConfig 资产是初始值模板，
/// 永不在运行中被写入——避免编辑器下 Play 期间改动的数值污染资产落盘。
/// </summary>
public class StatsManager : YSingleton<StatsManager>
{
    [SerializeField] private PlayerStatsSO statsConfig;

    private PlayerStatsSO runtimeStats;

    /// <summary>运行时数据副本（事件也在它身上），UI 订阅这个。</summary>
    public PlayerStatsSO RuntimeStats
    {
        get
        {
            if (runtimeStats == null && statsConfig != null)
            {
                runtimeStats = UnityEngine.Object.Instantiate(statsConfig);
                runtimeStats.name = statsConfig.name + " (Runtime)";
            }
            return runtimeStats;
        }
    }

    protected override void OnSingletonInitialized()
    {
        _ = RuntimeStats; // 单例初始化即建立副本，避免后续访问顺序问题
    }

    public PlayerStatsData GetStats() => RuntimeStats.Data;
    public void LoadStats(PlayerStatsData data) => RuntimeStats.LoadStats(data);
    public int GetDamage() => RuntimeStats.Data.damage;
    public float GetWeaponRange() => RuntimeStats.Data.weaponRange;
    public float GetKnockBackForce() => RuntimeStats.Data.knockBackForce;
    public float GetKnockBackTime() => RuntimeStats.Data.knockBackTime;
    public float GetStunTime() => RuntimeStats.Data.stunTime;
    public float GetCoolDown() => RuntimeStats.Data.coolDown;
    public float GetSpeed() => RuntimeStats.Data.speed;
    public int GetMaxHealth() => RuntimeStats.Data.maxHealth;
    public int GetCurrentHealth() => RuntimeStats.Data.currentHealth;
    public int GetSkillPoints() => RuntimeStats.Data.skillPoints;
    public int GetLevel() => RuntimeStats.Data.level;
    public int GetCurrentExp() => RuntimeStats.Data.currentExp;
    public int GetExpToUpgrade() => RuntimeStats.Data.expToUpgrade;
    public float GetExpMultiplier() => RuntimeStats.Data.expMultiplier;

    public void Respawn()
    {
        if (RuntimeStats.Data.currentHealth <= 0)
            RuntimeStats.SetCurrentHealth(RuntimeStats.Data.maxHealth);
    }

    public void UpdateMaxHealth(int amount) => RuntimeStats.UpdateMaxHealth(amount);
    public void UpdateHealth(int amount) => RuntimeStats.UpdateHealth(amount);
    public void UpdateSpeed(float amount) => RuntimeStats.UpdateSpeed(amount);
    public void UpdateDamage(int amount) => RuntimeStats.UpdateDamage(amount);
    public void UpdateSkillPoints(int amount) => RuntimeStats.UpdateSkillPoints(amount);
}
