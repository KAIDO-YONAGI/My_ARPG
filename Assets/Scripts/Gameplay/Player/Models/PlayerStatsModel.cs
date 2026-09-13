using System;
using UnityEngine;

namespace Gameplay.Player.Models
{
    /// 玩家数值的运行时状态与规则。
    /// 职责边界：
    /// - 初始值与经验曲线由 PlayerStatsSO 资产提供，启动时拷一份进来；
    /// - 运行时状态只存在这里，资产内容保持不变；
    /// - 存档用 PlayerStatsData 作为传输格式，通过 ToData() / LoadFrom() 以拷贝进出；
    /// - 数值的唯一写入口是本类的方法，data 字段由这些方法维护。
    /// 经验曲线是**累加式**：每次升级时，把"当前阈值先截断到 10 的整数倍，乘算子，再除以 4"
    /// 加到阈值上。
    /// 公式：   expToUpgrade = expToUpgrade + ((expToUpgrade / 10) * 10 * expMultiplier) / 4
    [Serializable]
    public class PlayerStatsModel
    {
        /// <summary>生命值或上限变化时触发。HealthController 订阅后推给 HealthView。</summary>
        public event Action HealthChanged;
        /// <summary>速度、伤害等属性变化时触发。StatsPanelController 订阅后推给 StatsPanelView。</summary>
        public event Action StatsChanged;
        /// <summary>升级时触发一次，参数是本次升的级数。SkillTreeManager 订阅它发放技能点。</summary>
        public event Action<int> LevelUp;
        /// <summary>当前经验、升级阈值或等级变化时触发。AddExp 结算完成后与 LoadFrom 读档完成后都会触发。
        /// ExperienceController 订阅它刷新经验条。</summary>
        public event Action ExpChanged;

        public const int MinExpToUpgrade = 1;
        public const int ExpStepLevels = 10;
        public const float ExpGrowthDivisor = 4f;

        private PlayerStatsData data;

        /// <summary>用一份全零的默认数据建立模型。</summary>
        public PlayerStatsModel() : this(new PlayerStatsData()) { }

        public PlayerStatsModel(PlayerStatsData initial)
        {
            data = initial.Clone();
        }

        // ---- 只读访问器：界面读取这些属性，数据的改动走本类的写方法 ----

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
        /// 读档：用存档数据整体替换运行时状态，并通知界面刷新。
        /// 阈值是累加得到的状态，读档时直接采用存档值，不从等级重算。
        /// </summary>
        public void LoadFrom(PlayerStatsData source)
        {
            if (source == null) return;

            data = source.Clone();

            HealthChanged?.Invoke();
            StatsChanged?.Invoke();
            ExpChanged?.Invoke(); // 读档换了经验/等级，经验条必须跟着刷
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

        /// <summary>技能点变化不触发事件，界面由 SkillTreeManager 刷新。</summary>
        public void UpdateSkillPoints(int amount) => data.skillPoints += amount;

        /// <summary>
        /// 增加经验并结算升级。升级次数由经验曲线决定，结算完成后通过 <see cref="LevelUp"/> 广播本次升的级数。
        /// 非正数直接忽略，负数会留下警告。
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

            ExpChanged?.Invoke(); // 经验值发生变化即通知，无论本轮是否升级

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
    }
}
