using NUnit.Framework;
using UnityEngine;

/// <summary>
/// PlayerStatsSO（4.3 数值 SO 化）单元测试：钳制规则与事件广播。
/// </summary>
public class PlayerStatsSOTests
{
    private PlayerStatsSO so;
    private int healthChangedCount;
    private int statsChangedCount;

    [SetUp]
    public void SetUp()
    {
        so = ScriptableObject.CreateInstance<PlayerStatsSO>();
        healthChangedCount = 0;
        statsChangedCount = 0;
        so.HealthChanged += () => healthChangedCount++;
        so.StatsChanged += () => statsChangedCount++;
        so.UpdateMaxHealth(8);  // maxHealth: 1 -> 8（默认 new() 的 maxHealth 为 0）
        so.SetCurrentHealth(8); // 满血
        healthChangedCount = 0;
        statsChangedCount = 0;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(so);
    }

    [Test]
    public void UpdateHealth_ClampsToZeroAndMax()
    {
        so.UpdateHealth(-99);
        Assert.AreEqual(0, so.Data.currentHealth);

        so.UpdateHealth(99);
        Assert.AreEqual(so.Data.maxHealth, so.Data.currentHealth);
    }

    [Test]
    public void UpdateHealth_FiresHealthChangedOnly()
    {
        so.UpdateHealth(-1);
        Assert.AreEqual(1, healthChangedCount);
        Assert.AreEqual(0, statsChangedCount);
    }

    [Test]
    public void UpdateMaxHealth_NeverGoesBelowOne()
    {
        so.UpdateMaxHealth(-100);
        Assert.AreEqual(1, so.Data.maxHealth);
    }

    [Test]
    public void UpdateSpeedAndDamage_FireStatsChangedOnly()
    {
        float speedBefore = so.Data.speed;
        int damageBefore = so.Data.damage;

        so.UpdateSpeed(1.5f);
        so.UpdateDamage(2);

        Assert.AreEqual(speedBefore + 1.5f, so.Data.speed);
        Assert.AreEqual(damageBefore + 2, so.Data.damage);
        Assert.AreEqual(0, healthChangedCount);
        Assert.AreEqual(2, statsChangedCount);
    }

    [Test]
    public void UpdateSkillPoints_AccumulatesWithoutEvents()
    {
        so.UpdateSkillPoints(3);
        so.UpdateSkillPoints(-1);
        Assert.AreEqual(2, so.Data.skillPoints);
        Assert.AreEqual(0, healthChangedCount);
        Assert.AreEqual(0, statsChangedCount);
    }

    [Test]
    public void SetCurrentHealth_ClampsToMax()
    {
        so.SetCurrentHealth(999);
        Assert.AreEqual(so.Data.maxHealth, so.Data.currentHealth);
    }

    [Test]
    public void LoadStats_ReplacesDataAndFiresBothEvents()
    {
        var data = new PlayerStatsData { damage = 7, maxHealth = 20, currentHealth = 10 };
        so.LoadStats(data);
        Assert.AreEqual(7, so.Data.damage);
        Assert.AreEqual(10, so.Data.currentHealth);
        Assert.AreEqual(1, healthChangedCount);
        Assert.AreEqual(1, statsChangedCount);
    }
}
