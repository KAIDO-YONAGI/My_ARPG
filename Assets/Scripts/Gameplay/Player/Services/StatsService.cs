using UnityEngine;

/// <summary>
/// 玩家数值这一层的服务（Service）：持有运行时 <see cref="PlayerStatsModel"/>，把既有 API 原样转发过去。
/// 对外是唯一入口，调用方（UI / 战斗 / 存档）继续用 StatsService.Instance.xxx()。
///
/// 数据与规则都在 PlayerStatsModel（普通 C#，可在 EditMode 测试里直接 new）；
/// 存档传输格式是 PlayerStatsData（Models/PlayerStatsData.cs）；
/// 本类不再克隆 SO，也没有运行时 SO 副本；PlayerStatsSO 只作为初始值模板使用。
/// </summary>
public class StatsService : YSingleton<StatsService>
{
    [SerializeField] private PlayerStatsSO statsConfig;

    private PlayerStatsModel model;

    /// <summary>运行时数值模型。UI 订阅它的事件（HealthChanged / StatsChanged）。</summary>
    public PlayerStatsModel Model
    {
        get
        {
            EnsureModel();
            return model;
        }
    }

    protected override void OnSingletonInitialized()
    {
        EnsureModel(); // 单例初始化即建立模型，避免后续访问顺序问题
    }

    private void EnsureModel()
    {
        if (model != null) return;

        PlayerStatsData initial = statsConfig != null ? statsConfig.CreateInitialData() : new PlayerStatsData();
        model = new PlayerStatsModel(initial);
    }

    /// <summary>取当前状态的一份快照，供存档使用。返回拷贝，不是运行时状态本身。</summary>
    public PlayerStatsData GetStats() => Model.ToData();

    /// <summary>读档：用存档数据整体替换运行时状态。</summary>
    public void LoadStats(PlayerStatsData data) => Model.LoadFrom(data);

    public int GetDamage() => Model.Damage;
    public float GetWeaponRange() => Model.WeaponRange;
    public float GetKnockBackForce() => Model.KnockBackForce;
    public float GetKnockBackTime() => Model.KnockBackTime;
    public float GetStunTime() => Model.StunTime;
    public float GetCoolDown() => Model.CoolDown;
    public float GetSpeed() => Model.Speed;
    public int GetMaxHealth() => Model.MaxHealth;
    public int GetCurrentHealth() => Model.CurrentHealth;
    public int GetSkillPoints() => Model.SkillPoints;
    public int GetLevel() => Model.Level;
    public int GetCurrentExp() => Model.CurrentExp;
    public int GetExpToUpgrade() => Model.ExpToUpgrade;
    public float GetExpMultiplier() => Model.ExpMultiplier;

    public void Respawn()
    {
        if (Model.CurrentHealth <= 0)
            Model.SetCurrentHealth(Model.MaxHealth);
    }

    public void UpdateMaxHealth(int amount) => Model.UpdateMaxHealth(amount);
    public void UpdateHealth(int amount) => Model.UpdateHealth(amount);
    public void UpdateSpeed(float amount) => Model.UpdateSpeed(amount);
    public void UpdateDamage(int amount) => Model.UpdateDamage(amount);
    public void UpdateSkillPoints(int amount) => Model.UpdateSkillPoints(amount);

    /// <summary>增加经验并结算升级（规则见 PlayerStatsModel.AddExp）。</summary>
    public void AddExp(int amount) => Model.AddExp(amount);
}
