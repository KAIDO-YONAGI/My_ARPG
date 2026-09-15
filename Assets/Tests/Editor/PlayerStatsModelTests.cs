using System.Text.RegularExpressions;
using Gameplay.Player.Models;
using Gameplay.Player.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gameplay.Tests
{
    /// <summary>
    /// PlayerStatsModel 的单元测试：钳制规则、事件广播、经验曲线、等级上限与拷贝语义。
    /// 整套用例在 EditMode 下运行。
    ///
    /// 曲线：expToUpgrade += ((expToUpgrade / 10) * 10 * expMultiplier) / 4
    /// 先按 (x / 10) * 10 把阈值截断到 10 的整数倍，再除以 4 并按 int 截断。
    /// </summary>
    public class PlayerStatsModelTests
    {
        private const int BaseExp = 10;

        private PlayerStatsModel model;
        private int healthChangedCount;
        private int statsChangedCount;
        private int lastLevelsGained;

        private static PlayerStatsData MakeData(int maxHealth = 8, int expToUpgrade = BaseExp,
            float expMultiplier = 1f, int maxLevel = 0)
        {
            // 显式给合法值：默认 new PlayerStatsData() 的阈值和算子都是 0
            return new PlayerStatsData
            {
                maxHealth = maxHealth,
                currentHealth = maxHealth,
                expToUpgrade = expToUpgrade,
                expMultiplier = expMultiplier,
                maxLevel = maxLevel,
            };
        }

        [SetUp]
        public void SetUp()
        {
            model = new PlayerStatsModel(MakeData());
            healthChangedCount = 0;
            statsChangedCount = 0;
            lastLevelsGained = 0;
            model.HealthChanged += () => healthChangedCount++;
            model.StatsChanged += () => statsChangedCount++;
            model.LevelUp += levels => lastLevelsGained = levels;
        }

        // ---- 从 PlayerStatsSOTests 迁移过来的钳制与事件规则 ----

        [Test]
        public void UpdateHealth_ClampsToZeroAndMax()
        {
            model.UpdateHealth(-99);
            Assert.AreEqual(0, model.CurrentHealth);

            model.UpdateHealth(99);
            Assert.AreEqual(model.MaxHealth, model.CurrentHealth);
        }

        [Test]
        public void UpdateHealth_FiresHealthChangedOnly()
        {
            model.UpdateHealth(-1);
            Assert.AreEqual(1, healthChangedCount);
            Assert.AreEqual(0, statsChangedCount);
        }

        // ---- ExpChanged：加经验与读档完成后触发，ExperienceController 订阅它刷新经验条 ----

        [Test]
        public void AddExp_FiresExpChanged()
        {
            int expChangedCount = 0;
            model.ExpChanged += () => expChangedCount++;

            model.AddExp(BaseExp);

            Assert.AreEqual(1, expChangedCount, "加了经验就要通知经验条刷新");
        }

        [Test]
        public void AddExp_IgnoredAmount_DoesNotFireExpChanged()
        {
            int expChangedCount = 0;
            model.ExpChanged += () => expChangedCount++;

            model.AddExp(0);
            model.AddExp(-5); // 负数入口是历史遗留，按"忽略"处理，不算状态变化

            Assert.AreEqual(0, expChangedCount);
        }

        [Test]
        public void LoadFrom_FiresExpChanged_SoTheBarFollowsTheSave()
        {
            int expChangedCount = 0;
            model.ExpChanged += () => expChangedCount++;

            var saveData = MakeData();
            saveData.level = 5;
            saveData.currentExp = 20;
            model.LoadFrom(saveData);

            Assert.AreEqual(1, expChangedCount, "读档换了经验与等级，经验条必须跟着刷");
            Assert.AreEqual(5, model.Level);
            Assert.AreEqual(20, model.CurrentExp);
        }

        [Test]
        public void UpdateMaxHealth_NeverGoesBelowOne()
        {
            model.UpdateMaxHealth(-100);
            Assert.AreEqual(1, model.MaxHealth);
        }

        [Test]
        public void UpdateSpeedAndDamage_FireStatsChangedOnly()
        {
            float speedBefore = model.Speed;
            int damageBefore = model.Damage;

            model.UpdateSpeed(1.5f);
            model.UpdateDamage(2);

            Assert.AreEqual(speedBefore + 1.5f, model.Speed);
            Assert.AreEqual(damageBefore + 2, model.Damage);
            Assert.AreEqual(0, healthChangedCount);
            Assert.AreEqual(2, statsChangedCount);
        }

        [Test]
        public void UpdateSkillPoints_AccumulatesWithoutEvents()
        {
            model.UpdateSkillPoints(3);
            model.UpdateSkillPoints(-1);
            Assert.AreEqual(2, model.SkillPoints);
            Assert.AreEqual(0, healthChangedCount);
            Assert.AreEqual(0, statsChangedCount);
        }

        [Test]
        public void Model_CanBeViewedThroughReadOnlyStatsInterface()
        {
            IPlayerStatsReadOnly stats = model;
            int healthChangedThroughInterface = 0;
            stats.HealthChanged += () => healthChangedThroughInterface++;

            Assert.AreEqual(model.Damage, stats.Damage);
            Assert.AreEqual(model.CurrentHealth, stats.CurrentHealth);
            Assert.AreEqual(model.MaxHealth, stats.MaxHealth);

            model.UpdateHealth(-1);

            Assert.AreEqual(1, healthChangedThroughInterface);
            Assert.AreEqual(model.CurrentHealth, stats.CurrentHealth);
        }

        [Test]
        public void StatsService_ExposesOnlyReadOnlyStatsView()
        {
            var statsProperty = typeof(StatsService).GetProperty("Stats");
            var modelProperty = typeof(StatsService).GetProperty("Model");

            Assert.IsNotNull(statsProperty);
            Assert.AreEqual(typeof(IPlayerStatsReadOnly), statsProperty.PropertyType);
            Assert.IsNull(modelProperty, "运行时 Model 不应再通过 StatsService 对外暴露");
        }

        [Test]
        public void SetCurrentHealth_ClampsToMax()
        {
            model.SetCurrentHealth(999);
            Assert.AreEqual(model.MaxHealth, model.CurrentHealth);
        }

        [Test]
        public void LoadFrom_ReplacesStateAndFiresBothEvents()
        {
            model.LoadFrom(new PlayerStatsData
            {
                damage = 7, maxHealth = 20, currentHealth = 10,
                expToUpgrade = BaseExp, expMultiplier = 1f,
            });

            Assert.AreEqual(7, model.Damage);
            Assert.AreEqual(10, model.CurrentHealth);
            Assert.AreEqual(1, healthChangedCount);
            Assert.AreEqual(1, statsChangedCount);
        }

        // ---- 经验曲线：累加 + 截断 + 除以 4 ----

        [Test]
        public void AddExp_LevelsUpWhenReachingThreshold_AndReportsLevelCount()
        {
            model.AddExp(9);
            Assert.AreEqual(0, model.Level);
            Assert.AreEqual(9, model.CurrentExp);
            Assert.AreEqual(0, lastLevelsGained);

            model.AddExp(1);
            Assert.AreEqual(1, model.Level);
            Assert.AreEqual(0, model.CurrentExp);
            Assert.AreEqual(1, lastLevelsGained, "升级事件应带上本次升的级数");
        }

        [Test]
        public void ExpCurve_FollowsAdditiveFormula_DividedByFour()
        {
            // 起始阈值 10、算子 1.5，步长按 int 截断：
            //   10 → ((10/10)*10*1.5)/4 = 3，阈值变 13
            //   13 → ((13/10)*10*1.5)/4 = 3，阈值变 16
            //   16 → ((16/10)*10*1.5)/4 = 3，阈值变 19
            //   19 → ((19/10)*10*1.5)/4 = 3，阈值变 22
            model = new PlayerStatsModel(MakeData(expMultiplier: 1.5f));

            model.AddExp(10);
            Assert.AreEqual(1, model.Level);
            Assert.AreEqual(0, model.CurrentExp);
            Assert.AreEqual(13, model.ExpToUpgrade);

            model.AddExp(13);
            Assert.AreEqual(2, model.Level);
            Assert.AreEqual(16, model.ExpToUpgrade, "阈值 13 先截断成 10 再算");

            model.AddExp(16);
            Assert.AreEqual(3, model.Level);
            Assert.AreEqual(19, model.ExpToUpgrade);

            model.AddExp(19);
            Assert.AreEqual(4, model.Level);
            Assert.AreEqual(22, model.ExpToUpgrade);
        }

        [Test]
        public void ExpCurve_BelowTen_StopsGrowing()
        {
            // 阈值小于 10 时 (x / 10) * 10 == 0，步长为 0，阈值保持不变。
            var small = new PlayerStatsModel(MakeData(expToUpgrade: 3, expMultiplier: 1.5f));

            small.AddExp(3);
            Assert.AreEqual(1, small.Level);
            Assert.AreEqual(3, small.ExpToUpgrade);

            small.AddExp(3);
            Assert.AreEqual(2, small.Level);
            Assert.AreEqual(3, small.ExpToUpgrade);
        }

        [Test]
        public void ExpCurve_HugeGain_GainsSeveralLevels_AndTerminates()
        {
            model = new PlayerStatsModel(MakeData(expMultiplier: 1.5f));

            // 曲线 10 / 13 / 16 / 19 / 22 / 29 / 36 / 47 / 62 / 84 / 114 / 155 / 211 / 289 …
            // 1000 点经验升 13 级，剩 182，下一级要 289
            model.AddExp(1000);

            Assert.AreEqual(13, model.Level);
            Assert.AreEqual(182, model.CurrentExp);
            Assert.AreEqual(289, model.ExpToUpgrade);
            Assert.AreEqual(13, lastLevelsGained);
        }

        [Test]
        public void ExpCurve_WithZeroMultiplier_KeepsThresholdAtBase_NoRunaway()
        {
            // 算子为 0 时步长为 0，阈值恒为基准值：
            // 只是"永远每 10 点升一级"，不会退化成每次获得经验都升级。
            model = new PlayerStatsModel(MakeData(expMultiplier: 0f));

            Assert.AreEqual(BaseExp, model.ExpToUpgrade);

            model.AddExp(2);
            Assert.AreEqual(0, model.Level);
            Assert.AreEqual(2, model.CurrentExp);
        }

        [Test]
        public void ExpCurve_WithZeroBase_IsFlooredToKeepCurveAlive()
        {
            var repaired = new PlayerStatsModel(MakeData(expToUpgrade: 0, expMultiplier: 1f));

            Assert.AreEqual(PlayerStatsModel.MinExpToUpgrade, repaired.ExpToUpgrade);

            repaired.AddExp(3); // 阈值为 1，所以连升 3 级，但一定终止
            Assert.AreEqual(3, repaired.Level);
            Assert.AreEqual(0, repaired.CurrentExp);
        }

        [Test]
        public void AddExp_NonPositiveAmount_IsIgnored_AndWarns()
        {
            LogAssert.Expect(LogType.Warning, new Regex("忽略非正数经验"));

            model.AddExp(-5);

            Assert.AreEqual(0, model.CurrentExp, "负数不应该扣经验");
            Assert.AreEqual(0, model.Level);
        }

        [Test]
        public void AddExp_StopsAtMaxLevel()
        {
            model = new PlayerStatsModel(MakeData(expMultiplier: 1f, maxLevel: 1));

            model.AddExp(100);

            Assert.AreEqual(1, model.Level, "达到 maxLevel 后不再升级");
        }

        // ---- 存档边界：拷贝语义与阈值持久化 ----

        [Test]
        public void ToData_ReturnsCopy_SoMutatingItDoesNotAffectRuntimeState()
        {
            int damageBefore = model.Damage;

            PlayerStatsData snapshot = model.ToData();
            snapshot.damage = 999;

            Assert.AreEqual(damageBefore, model.Damage);
        }

        [Test]
        public void LoadFrom_DoesNotAliasTheIncomingData()
        {
            var saveData = MakeData();
            saveData.damage = 7;
            model.LoadFrom(saveData);

            saveData.damage = 999; // 模拟"存档数据之后又被改"

            Assert.AreEqual(7, model.Damage, "LoadFrom 应该拷贝，而不是持有同一个引用");
        }

        [Test]
        public void LoadFrom_KeepsSavedThreshold_BecauseCurveIsStateful()
        {
            var saveData = MakeData(expMultiplier: 1.5f);
            saveData.level = 10;
            saveData.expToUpgrade = 325; // 累加出来的值，不能从等级反推

            model.LoadFrom(saveData);

            Assert.AreEqual(325, model.ExpToUpgrade);

            model.AddExp(325);
            // 325 → 325 + ((325/10)*10*1.5)/4 = 325 + (320*1.5)/4 = 325 + 120 = 445
            Assert.AreEqual(445, model.ExpToUpgrade);
        }

        [Test]
        public void LoadFrom_FloorsGarbageThresholdToAtLeastOne()
        {
            var saveData = MakeData();
            saveData.expToUpgrade = 0; // 旧档/坏档

            model.LoadFrom(saveData);

            Assert.AreEqual(PlayerStatsModel.MinExpToUpgrade, model.ExpToUpgrade);
        }
    }
}
