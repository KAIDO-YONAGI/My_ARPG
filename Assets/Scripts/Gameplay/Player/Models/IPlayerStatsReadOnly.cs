using System;

namespace Gameplay.Player.Models
{
    /// <summary>
    /// 玩家数值的只读视图。外部系统可以查询状态并订阅变化，
    /// 但不能通过这个接口修改运行时数值。
    /// </summary>
    public interface IPlayerStatsReadOnly
    {
        event Action HealthChanged;
        event Action StatsChanged;
        event Action<int> LevelUp;
        event Action ExpChanged;

        int Damage { get; }
        float WeaponRange { get; }
        float KnockBackForce { get; }
        float KnockBackTime { get; }
        float StunTime { get; }
        float CoolDown { get; }
        float Speed { get; }
        int MaxHealth { get; }
        int CurrentHealth { get; }
        int SkillPoints { get; }
        int Level { get; }
        int CurrentExp { get; }
        int ExpToUpgrade { get; }
        float ExpMultiplier { get; }
        int MaxLevel { get; }
    }
}
