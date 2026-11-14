using System;

namespace Gameplay.Player.Models
{
    /// <summary>
    /// 玩家数值的只读视图。外部系统可以查询状态并订阅变化，
    /// 但不能通过这个接口修改运行时数值。
    /// </summary>
    public interface IPlayerStatsReadOnly
    {
        /// <summary>生命值或生命上限变化时触发。</summary>
        event Action HealthChanged;
        /// <summary>速度、伤害等属性变化时触发，界面据此重绘属性。</summary>
        event Action StatsChanged;
        /// <summary>升级时触发一次，参数是本次升的级数。</summary>
        event Action<int> LevelUp;
        /// <summary>经验、升级阈值或等级变化时触发。</summary>
        event Action ExpChanged;
        /// <summary>读档整体替换数值并完成阈值修复后触发一次，此时接口上的数值已是恢复后的结果。</summary>
        event Action StatsLoaded;

        /// <summary>近战伤害数值。</summary>
        int Damage { get; }
        /// <summary>武器攻击距离。</summary>
        float WeaponRange { get; }
        /// <summary>攻击击退力度。</summary>
        float KnockBackForce { get; }
        /// <summary>受击硬直时长。</summary>
        float KnockBackTime { get; }
        /// <summary>受击眩晕时长。</summary>
        float StunTime { get; }
        /// <summary>攻击冷却时间。</summary>
        float CoolDown { get; }
        /// <summary>移动速度。</summary>
        float Speed { get; }
        /// <summary>生命值上限。</summary>
        int MaxHealth { get; }
        /// <summary>当前生命值。</summary>
        int CurrentHealth { get; }
        /// <summary>当前可用技能点。</summary>
        int SkillPoints { get; }
        /// <summary>当前等级。</summary>
        int Level { get; }
        /// <summary>当前经验值。</summary>
        int CurrentExp { get; }
        /// <summary>升到下一级所需的经验值。</summary>
        int ExpToUpgrade { get; }
        /// <summary>经验阈值成长倍率。</summary>
        float ExpMultiplier { get; }
        /// <summary>等级上限；0 表示不限制。</summary>
        int MaxLevel { get; }
    }
}
