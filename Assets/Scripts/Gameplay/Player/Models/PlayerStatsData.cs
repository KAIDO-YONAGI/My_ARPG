using System;

namespace Gameplay.Player.Models
{
    /// <summary>
    /// 玩家数值的存档传输格式。字段名就是存档 JSON 的键，改名会让已有存档读不出来；
    /// 类名可以自由改动。
    /// 运行时的数值状态位于 PlayerStatsModel，这个类在资产模板、运行时模型与存档之间传递数据，
    /// 每次传递都通过 Clone() 复制，两边各持有独立的对象。
    /// </summary>
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
        /// <summary>等级上限。值为 0 时不限制等级。</summary>
        public int maxLevel;

        /// <summary>
        /// 字段级拷贝。字段全是值类型，浅拷贝即可。
        /// 存档边界使用它，让运行时状态与存档数据成为彼此独立的对象。
        /// </summary>
        public PlayerStatsData Clone() => (PlayerStatsData)MemberwiseClone();
    }
}
