using Gameplay.Player.Models;
using UnityEngine;

namespace Gameplay.Player.Services
{
    /// <summary>
    /// 玩家数值的服务层。持有运行时 <see cref="PlayerStatsModel"/>，负责它的生命周期与存档；
    /// 注册/注销机制由 <see cref="SaveableService{TSelf}"/> 基类提供（静态注册表，无时序依赖）。
    ///
    /// 数值的读写统一走 <see cref="Model"/>：规则（钳制、事件广播）都在模型方法内部。
    /// Respawn 留在服务层，因为它带业务规则：仅死亡时复活回满血。
    /// 数值的状态、规则与事件位于 PlayerStatsModel，它是普通 C# 类型，EditMode 测试可以直接创建。
    /// 存档传输格式是 PlayerStatsData。PlayerStatsSO 提供初始值，运行时的改动留在模型里。
    /// </summary>
    public class StatsService : SaveableService<StatsService>
    {
        [SerializeField] private PlayerStatsSO statsConfig;

        /// <summary>
        /// 当前玩家状态：血量、速度、伤害、等级、经验、技能点。
        /// 建模型与注册存档都在这里完成，单例一就绪数值即可用。
        /// </summary>
        private PlayerStatsModel model;

        /// <summary>运行时数值模型的唯一入口。UI、Controller、技能树从这里取得模型，订阅它的事件并读写数值。</summary>
        public PlayerStatsModel Model => model;

        protected override void OnSingletonInitialized()
        {
            base.OnSingletonInitialized(); // 注册进 SaveRegistry
            model = new PlayerStatsModel(statsConfig.CreateInitialData());
        }

        public void Respawn()
        {
            if (Model.CurrentHealth <= 0)
                Model.SetCurrentHealth(Model.MaxHealth);
        }

        /// <summary>取当前状态的一份快照，供存档使用。返回拷贝，不是运行时状态本身。</summary>
        private PlayerStatsData GetStats() => Model.ToData();

        /// <summary>读档：用存档数据整体替换运行时状态。</summary>
        private void LoadStats(PlayerStatsData data) => Model.LoadFrom(data);

        /// <summary>把当前数值写进存档。</summary>
        public override void SaveData(Data data) => data.playerStatsData = GetStats();

        /// <summary>从存档恢复数值。存档里没有数值段时保留当前状态。</summary>
        public override void LoadData(Data data)
        {
            if (data.playerStatsData == null) return;

            LoadStats(data.playerStatsData);
        }
    }
}
