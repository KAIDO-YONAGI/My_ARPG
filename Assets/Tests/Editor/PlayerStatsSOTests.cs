using NUnit.Framework;
using UnityEngine;

/// <summary>
/// PlayerStatsSO 现在只是"初始值模板"：规则与事件都搬到了 PlayerStatsModel
/// （钳制、事件、经验曲线、守卫的用例见 PlayerStatsModelTests）。
///
/// 这里只验证模板的拷贝语义——运行时拿到的是拷贝，改它不会反过来污染资产。
/// </summary>
public class PlayerStatsSOTests
{
    private PlayerStatsSO so;

    [SetUp]
    public void SetUp()
    {
        so = ScriptableObject.CreateInstance<PlayerStatsSO>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(so);
    }

    [Test]
    public void CreateInitialData_ReflectsConfiguredValues()
    {
        so.Data.damage = 7;
        so.Data.expToUpgrade = 10;
        so.Data.expMultiplier = 1.5f;

        PlayerStatsData initial = so.CreateInitialData();

        Assert.AreEqual(7, initial.damage);
        Assert.AreEqual(10, initial.expToUpgrade);
        Assert.AreEqual(1.5f, initial.expMultiplier);
    }

    [Test]
    public void CreateInitialData_ReturnsCopy_SoRuntimeWritesDoNotTouchTheTemplate()
    {
        so.Data.damage = 7;

        PlayerStatsData initial = so.CreateInitialData();
        initial.damage = 999; // 模拟运行时改了模型里的那份数据

        Assert.AreEqual(7, so.Data.damage, "模板不应该被运行时改动");
    }
}
