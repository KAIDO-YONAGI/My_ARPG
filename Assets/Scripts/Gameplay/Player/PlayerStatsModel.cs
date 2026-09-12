using System;
using UnityEngine;

/// <summary>
/// 玩家数值的运行时状态与规则。
///
/// 这是普通 C# 类型（不继承 MonoBehaviour / ScriptableObject），所以 EditMode 测试里
/// 可以直接 new 出来跑，不需要进 Play、不需要场景。
///
/// 职责边界：
/// - 配置（初始值、经验曲线）由 PlayerStatsSO 资产提供，启动时拷一份进来；
/// - 运行时状态只存在这里，SO 资产不会被写；
/// - 存档用 PlayerStatsData 作为传输格式，只通过 ToData() / LoadFrom() 进出，且都是拷贝，
///   不与运行时状态共享同一个引用；
/// - 数值的唯一写入口是本类的方法，外部不要直接改 data 的字段。
///
/// 经验曲线是**累加式**：每次升级时，把"当前阈值先截断到 10 的整数倍，乘算子，再除以 4"
/// 加到阈值上。
/// <code>
///     expToUpgrade = expToUpgrade + ((expToUpgrade / 10) * 10 * expMultiplier) / 4
/// </code>
[Serializable]
public class PlayerStatsModel
{
    /// <summary>生命值或上限变化（HealthCanvasManager 订阅）。</summary>
    public event Action HealthChanged;
    /// <summary>速度、伤害等属性变化（StatsCanvasManager 订阅）。</summary>
    public event Action StatsChanged;
    /// <summary>升级时触发一次，参数是本次升的级数（ExpManager 转发给技能树）。</summary>
    public event Action<int> LevelUp;

    /// <summary>升级所需经验的保底值。曲线算出 0 或负数时必须被顶到这里，
    /// 否则 <c>currentExp &gt;= expToUpgrade</c> 会恒真，变成"每次获得经验都升一级"。</summary>
    public const int MinExpToUpgrade = 1;

    /// <summary>阈值截断的粒度：整除这个数再乘回去（公式里的 10）。</summary>
    public const int ExpStepLevels = 10;

    /// <summary>公式末尾的除数（4）：把台阶压小，避免曲线涨得过快。</summary>
    public const float ExpGrowthDivisor = 4f;

    private PlayerStatsData data;

    public PlayerStatsModel() : this(null) { }

    public PlayerStatsModel(PlayerStatsData initial)
    {
        data = initial != null ? initial.Clone() : new PlayerStatsData();
        Sanitize();
    }

    // ---- 只读访问器（UI 读这些，不要拿 data 去改）----

    public int Damage => data.damage;
    public float WeaponRange => data.weaponRange;
    public float KnockBackForce => data.knockBackForce;
    public float KnockBackTime => data.knockBackTime;
    public float StunTime => data.stunTime;
    public float CoolDown => data.coolDown;
    public float Speed => data.speed;
    public int MaxHealth => data.maxHealth;
    public int CurrentHealth => data.currentHealth;
    public int SkillPoints => data.skillPoints;
    public int Level => data.level;
    public int CurrentExp => data.currentExp;
    public int ExpToUpgrade => data.expToUpgrade;
    public float ExpMultiplier => data.expMultiplier;
    /// <summary>等级上限；0 表示不限制。</summary>
    public int MaxLevel => data.maxLevel;

    // ---- 存档边界 ----

    /// <summary>导出当前状态的一份拷贝，供存档使用。返回的是拷贝，改它不影响运行时状态。</summary>
    public PlayerStatsData ToData() => data.Clone();

    /// <summary>
    /// 读档：用存档数据整体替换运行时状态，并通知 UI 刷新。
    /// 阈值是累加出来的状态，直接采信存档值（只做 &gt;= 1 的下限兜底），不从等级重算。
    /// </summary>
    public void LoadFrom(PlayerStatsData source)
    {
        if (source == null) return;

        data = source.Clone();
        Sanitize();

        HealthChanged?.Invoke();
        StatsChanged?.Invoke();
    }

    // ---- 规则：唯一写入口 ----

    public void UpdateMaxHealth(int amount)
    {
        data.maxHealth += amount;
        if (data.maxHealth < 1) data.maxHealth = 1;
        HealthChanged?.Invoke();
    }

    public void UpdateHealth(int amount)
    {
        data.currentHealth = Mathf.Clamp(data.currentHealth + amount, 0, data.maxHealth);
        HealthChanged?.Invoke();
    }

    public void SetCurrentHealth(int value)
    {
        data.currentHealth = Mathf.Clamp(value, 0, data.maxHealth);
        HealthChanged?.Invoke();
    }

    public void UpdateSpeed(float amount)
    {
        data.speed += amount;
        StatsChanged?.Invoke();
    }

    public void UpdateDamage(int amount)
    {
        data.damage += amount;
        StatsChanged?.Invoke();
    }

    /// <summary>技能点按原实现不发事件（UI 由 SkillTreeManager 自己刷新）。</summary>
    public void UpdateSkillPoints(int amount) => data.skillPoints += amount;

    /// <summary>
    /// 增加经验并结算升级。升几次由曲线决定，升完通过 <see cref="LevelUp"/> 广播总级数。
    /// 非正数直接忽略（并留下警告）——负数扣经验是历史遗留行为，已按"不接受"处理。
    /// </summary>
    public void AddExp(int amount)
    {
        if (amount <= 0)
        {
            if (amount < 0) Debug.LogWarning($"[PlayerStatsModel] 忽略非正数经验：{amount}");
            return;
        }

        data.currentExp += amount;

        int levelsGained = 0;
        while (data.expToUpgrade >= MinExpToUpgrade
               && data.currentExp >= data.expToUpgrade
               && (data.maxLevel <= 0 || data.level < data.maxLevel))
        {
            data.currentExp -= data.expToUpgrade;
            data.level++;

            GrowExpToUpgrade();

            levelsGained++;
        }

        if (levelsGained > 0) LevelUp?.Invoke(levelsGained);
    }

    /// <summary>
    /// 升一级时抬高阈值：<c>expToUpgrade += ((expToUpgrade / 10) * 10 * expMultiplier) / 4</c>。
    /// <c>(expToUpgrade / 10) * 10</c> 是整数运算，等于"截断到 10 的整数倍"；
    /// 末尾按 int 截断，与 C# 里 <c>int += float</c> 的语义一致。
    /// </summary>
    private void GrowExpToUpgrade()
    {
        int truncated = (data.expToUpgrade / ExpStepLevels) * ExpStepLevels;
        int step = (int)(truncated * data.expMultiplier / ExpGrowthDivisor);

        data.expToUpgrade = Mathf.Max(MinExpToUpgrade, data.expToUpgrade + step);
    }

    /// <summary>
    /// 兜底：把不合法的配置/存档值纠正回来，并留下警告。
    /// 阈值必须 &gt;= 1，否则 <c>currentExp &gt;= expToUpgrade</c> 会恒真（实测过的坑）；
    /// 算子为负会让阈值越升越小，同样按 0 处理。
    /// </summary>
    private void Sanitize()
    {
        if (data.expToUpgrade < MinExpToUpgrade)
        {
            data.expToUpgrade = MinExpToUpgrade;
        }

        if (data.expMultiplier < 0f)
        {
            Debug.LogWarning($"[PlayerStatsModel] expMultiplier = {data.expMultiplier} 为负数，" +
                             "升级所需经验会越升越小；已按 0 处理。请检查 PlayerStatsSO 资产。");
            data.expMultiplier = 0f;
        }
    }
}
