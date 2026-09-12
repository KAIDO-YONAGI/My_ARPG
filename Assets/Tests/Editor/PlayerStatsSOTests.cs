using Gameplay.Player.Models;
using NUnit.Framework;
using UnityEngine;

namespace Gameplay.Tests
{
    /// <summary>
    /// PlayerStatsSO 的单元测试：验证 CreateInitialData() 返回拷贝，
    /// 修改拷贝之后资产内容保持不变。
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
}
