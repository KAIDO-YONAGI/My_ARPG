using System;

/// <summary>
/// 玩家数值的存档传输格式（DTO，属 Model 层）：字段就是存档 JSON 的键，
/// 所以改字段名会让旧存档读不出来（改类名不影响：Json.NET 默认不写类型名）。
/// 运行时状态不在这个类里，在 PlayerStatsModel；本类只负责“SO 模板 → Model → 存档”的搬运，
/// 并靠 Clone() 保证两边不共享同一个引用。
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
    /// <summary>等级上限；0 表示不限制（旧存档没有这个字段，反序列化后为 0，即不限制）。</summary>
    public int maxLevel;

    /// <summary>
    /// 字段级拷贝。字段全是值类型，浅拷贝就够。
    /// 存档边界用它，避免"运行时状态"和"存档数据"变成同一个对象引用。
    /// </summary>
    public PlayerStatsData Clone() => (PlayerStatsData)MemberwiseClone();
}
