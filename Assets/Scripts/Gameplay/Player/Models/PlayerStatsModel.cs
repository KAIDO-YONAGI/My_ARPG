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
    public class PlayerStatsModel : IPlayerStatsReadOnly
    {
        /// <summary>生命值或上限变化时触发。HealthController 订阅后推给 HealthView。</summary>
        public event Action HealthChanged;
        /// <summary>速度、伤害等属性变化时触发。StatsCanvasController 订阅后推给 StatsCanvasView。</summary>
        public event Action StatsChanged;
        /// <summary>升级时触发一次，参数是本次升的级数。SkillTreeManager 订阅它发放技能点。</summary>
        public event Action<int> LevelUp;
        /// <summary>当前经验、升级阈值或等级变化时触发。AddExp 结算完成后与 LoadFrom 读档完成后都会触发。
        /// ExperienceController 订阅它刷新经验条。</summary>
        public event Action ExpChanged;
        /// <summary>读档替换运行时数值并完成阈值修复后触发一次，此时模型已经处于恢复后的状态。</summary>
        public event Action StatsLoaded;

        /// <summary>升级所需经验的下限；低于该值的阈值会被修回本值，避免永远升不了级。</summary>
        public const int MinExpToUpgrade = 1;
        /// <summary>升级时抬高阈值前用于截断的步长。</summary>
        public const int ExpStepLevels = 10;
        /// <summary>升级时抬高阈值的除数。</summary>
        public const float ExpGrowthDivisor = 4f;

        // 运行时数值本体。构造与读档时整体替换为拷贝，之后只由本类的写方法修改。
        private PlayerStatsData data;

        /// <summary>用一份全零的默认数据建立模型。</summary>
        public PlayerStatsModel() : this(new PlayerStatsData()) { }

        /// <summary>用指定数据建立模型；构造时拷贝，之后与传入对象互不影响。</summary>
        /// <param name="initial">初始数值模板，构造时复制一份。</param>
        public PlayerStatsModel(PlayerStatsData initial)
        {
            data = initial.Clone();
            RepairExpThreshold();
        }

        // ---- 只读访问器：界面读取这些属性，数据的改动走本类的写方法 ----

        /// <summary>近战伤害数值。</summary>
        public int Damage => data.damage;
        /// <summary>武器攻击距离。</summary>
        public float WeaponRange => data.weaponRange;
        /// <summary>攻击击退力度。</summary>
        public float KnockBackForce => data.knockBackForce;
        /// <summary>受击硬直时长。</summary>
        public float KnockBackTime => data.knockBackTime;
        /// <summary>受击眩晕时长。</summary>
        public float StunTime => data.stunTime;
        /// <summary>攻击冷却时间。</summary>
        public float CoolDown => data.coolDown;
        /// <summary>移动速度。</summary>
        public float Speed => data.speed;
        /// <summary>生命值上限。</summary>
        public int MaxHealth => data.maxHealth;
        /// <summary>当前生命值。</summary>
        public int CurrentHealth => data.currentHealth;
        /// <summary>当前可用技能点。</summary>
        public int SkillPoints => data.skillPoints;
        /// <summary>当前等级。</summary>
        public int Level => data.level;
        /// <summary>当前经验值。</summary>
        public int CurrentExp => data.currentExp;
        /// <summary>升到下一级所需的经验值。</summary>
        public int ExpToUpgrade => data.expToUpgrade;
        /// <summary>经验阈值成长倍率。</summary>
        public float ExpMultiplier => data.expMultiplier;
        /// <summary>等级上限；0 表示不限制。</summary>
        public int MaxLevel => data.maxLevel;

        // ---- 存档边界 ----

        /// <summary>导出当前状态的一份拷贝，供存档使用。返回的是拷贝，改它不影响运行时状态。</summary>
        public PlayerStatsData ToData() => data.Clone();

        /// <summary>
        /// 读档：用存档数据整体替换运行时状态，并通知界面刷新。
        /// 阈值是累加得到的状态，读档时直接采用存档值，不从等级重算；
        /// 低于下限的坏档值由 <see cref="RepairExpThreshold"/> 兜底。
        /// 事件顺序固定为 StatsLoaded → HealthChanged → StatsChanged → ExpChanged。
        /// </summary>
        /// <param name="source">存档数据；方法内克隆一份，为 null 时直接返回、运行时状态不变。</param>
        public void LoadFrom(PlayerStatsData source)
        {
            if (source == null) return;

            data = source.Clone();
            RepairExpThreshold();

            StatsLoaded?.Invoke(); // 数值已就位，旁路先同步基线，后续变化事件按新值比较
            HealthChanged?.Invoke();
            StatsChanged?.Invoke();
            ExpChanged?.Invoke(); // 读档换了经验/等级，经验条必须跟着刷
        }

        /// <summary>
        /// 阈值下限兜底：低于 <see cref="MinExpToUpgrade"/> 的阈值会让升级循环条件恒不成立，
        /// 玩家永远升不了级，因此构造与读档两个入口都把阈值修回下限。
        /// <see cref="GrowExpToUpgrade"/> 抬高阈值时受同一个下限约束。
        /// </summary>
        private void RepairExpThreshold()
        {
            if (data.expToUpgrade < MinExpToUpgrade)
                data.expToUpgrade = MinExpToUpgrade;
        }

        // ---- 规则：唯一写入口 ----

        /// <summary>调整生命上限，结果最低为 1。</summary>
        /// <param name="amount">上限增量，可为负。</param>
        public void UpdateMaxHealth(int amount)
        {
            data.maxHealth += amount;
            if (data.maxHealth < 1) data.maxHealth = 1;
            HealthChanged?.Invoke();
        }

        /// <summary>调整当前生命，结果钳制在 0 与生命上限之间。</summary>
        /// <param name="amount">生命增量，负数表示受伤。</param>
        public void UpdateHealth(int amount)
        {
            data.currentHealth = Mathf.Clamp(data.currentHealth + amount, 0, data.maxHealth);
            HealthChanged?.Invoke();
        }

        /// <summary>把当前生命设为指定值，结果钳制在 0 与生命上限之间。</summary>
        /// <param name="value">目标生命值。</param>
        public void SetCurrentHealth(int value)
        {
            data.currentHealth = Mathf.Clamp(value, 0, data.maxHealth);
            HealthChanged?.Invoke();
        }

        /// <summary>调整移动速度。</summary>
        /// <param name="amount">速度增量，可为负。</param>
        public void UpdateSpeed(float amount)
        {
            data.speed += amount;
            StatsChanged?.Invoke();
        }

        /// <summary>调整攻击伤害。</summary>
        /// <param name="amount">伤害增量，可为负。</param>
        public void UpdateDamage(int amount)
        {
            data.damage += amount;
            StatsChanged?.Invoke();
        }

        /// <summary>技能点变化不触发事件，界面由 SkillTreeManager 刷新。</summary>
        /// <param name="amount">技能点增量，可为负。</param>
        public void UpdateSkillPoints(int amount) => data.skillPoints += amount;

        /// <summary>
        /// 增加经验并结算升级。升级次数由经验曲线决定，结算完成后通过 <see cref="LevelUp"/> 广播本次升的级数。
        /// 非正数直接忽略，负数会留下警告。
        /// </summary>
        /// <param name="amount">增加的经验值，非正数不生效。</param>
        public void AddExp(int amount)
        {
            if (amount <= 0)
            {
#if UNITY_EDITOR
                if (amount < 0) Debug.LogWarning($"[PlayerStatsModel] 忽略非正数经验：{amount}");
#endif
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
