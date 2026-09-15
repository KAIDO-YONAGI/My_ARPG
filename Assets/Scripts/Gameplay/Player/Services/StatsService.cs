using Gameplay.Player.Models;
using UnityEngine;

namespace Gameplay.Player.Services
{
    /// <summary>
    /// 玩家数值的服务层。持有运行时 <see cref="PlayerStatsModel"/>，负责它的生命周期与存档；
    /// 注册与注销由 <see cref="SaveableService{TSelf}"/> 基类经静态 SaveRegistry 完成。
    ///
    /// 外部通过只读的 <see cref="Stats"/> 查询状态或订阅事件，写操作通过本服务进入模型；
    /// 规则与事件的具体实现仍在模型方法内部。
    /// Respawn 带业务规则，留在服务层：仅死亡时复活回满血。
    /// PlayerStatsModel 是普通 C# 类型，EditMode 测试可以直接创建；
    /// 存档传输格式是 PlayerStatsData；PlayerStatsSO 提供初始值，运行时的改动留在模型里。
    /// </summary>
    public class StatsService : SaveableService<StatsService>
    {
        [SerializeField] private PlayerStatsSO statsConfig;

        /// <summary>当前玩家状态：血量、速度、伤害、等级、经验、技能点。单例就绪后即可用。</summary>
        private PlayerStatsModel model;

        /// <summary>运行时数值的只读入口。外部不能通过它调用模型的写方法。</summary>
        public IPlayerStatsReadOnly Stats => model;

        protected override void OnSingletonInitialized()
        {
            base.OnSingletonInitialized(); // 注册进 SaveRegistry
            model = new PlayerStatsModel(statsConfig.CreateInitialData());
        }

        public void Respawn()
        {
            if (model.CurrentHealth <= 0)
                model.SetCurrentHealth(model.MaxHealth);
        }

        // ---- 对外写入口：服务负责边界，模型负责数值规则 ----

        public void UpdateMaxHealth(int amount) => model.UpdateMaxHealth(amount);

        public void UpdateHealth(int amount) => model.UpdateHealth(amount);

        public void UpdateSpeed(float amount) => model.UpdateSpeed(amount);

        public void UpdateDamage(int amount) => model.UpdateDamage(amount);

        public void UpdateSkillPoints(int amount) => model.UpdateSkillPoints(amount);

        public void AddExperience(int amount) => model.AddExp(amount);

        /// <summary>取当前状态的一份快照，供存档使用。返回拷贝，不是运行时状态本身。</summary>
        private PlayerStatsData GetStats() => model.ToData();

        /// <summary>读档：用存档数据整体替换运行时状态。</summary>
        private void LoadStats(PlayerStatsData data) => model.LoadFrom(data);

        /// <summary>把当前数值写进存档。</summary>
        public override void SaveData(SaveData data) => data.playerStatsData = GetStats();

        /// <summary>从存档恢复数值。存档里没有数值段时保留当前状态。</summary>
        public override void LoadData(SaveData data)
        {
            if (data.playerStatsData == null) return;

            LoadStats(data.playerStatsData);
        }
    }
}
