using Gameplay.Player.Models;
using UnityEngine;

namespace Gameplay.Player.Services
{
    /// <summary>
    /// 玩家数值的服务层。持有运行时 <see cref="PlayerStatsModel"/>，对外提供读取与修改玩家数值的方法，
    /// 内部把调用转发给模型。
    ///
    /// 调用方通过 StatsService.Instance 使用这些方法。数值的状态、规则与事件位于 PlayerStatsModel，
    /// 它是普通 C# 类型，EditMode 测试可以直接创建。存档传输格式是 PlayerStatsData。
    /// PlayerStatsSO 提供初始值，运行时的改动留在模型里。
    /// </summary>
    public class StatsService : YSingleton<StatsService>, ISaveable
    {
        [SerializeField] private PlayerStatsSO statsConfig;

        private bool registeredInSaveRegistry;

        /// <summary>
        /// 当前玩家状态：血量、速度、伤害、等级、经验、技能点。由 <see cref="OnSingletonInitialized"/>
        /// 创建一次，之后引用保持不变。
        /// </summary>
        private PlayerStatsModel model;

        /// 运行时数值模型的只读入口。UI、Controller、技能树从这里取得模型，订阅它的事件并读取数值。
        public PlayerStatsModel Model => model; 

        /// <summary>
        /// 单例注册完成后的初始化钩子，由 <see cref="YSingleton{T}"/> 在 Awake 中调用。
        /// 模型在这里建立，单例一就绪数值即可使用。
        /// </summary>
        protected override void OnSingletonInitialized()
        {
            model = new PlayerStatsModel(statsConfig.CreateInitialData());
            RegisterSelf();
        }

        private void Start() => RegisterSelf();

        /// <summary>登记进 DataManager 的存档注册表，重复调用只生效一次。</summary>
        private void RegisterSelf()
        {
            if (registeredInSaveRegistry || DataManager.Instance == null) return;

            registeredInSaveRegistry = true;
            DataManager.Instance.RegisterSaveableData(this);
        }

        protected override void OnDestroy()
        {
            if (registeredInSaveRegistry && DataManager.Instance != null)
            {
                DataManager.Instance.UnRegisterSaveableData(this);
            }

            registeredInSaveRegistry = false;
            base.OnDestroy();
        }

        /// <summary>取当前状态的一份快照，供存档使用。返回拷贝，不是运行时状态本身。</summary>
        public PlayerStatsData GetStats() => Model.ToData();

        /// <summary>读档：用存档数据整体替换运行时状态。</summary>
        public void LoadStats(PlayerStatsData data) => Model.LoadFrom(data);

        public void Respawn()
        {
            if (Model.CurrentHealth <= 0)
                Model.SetCurrentHealth(Model.MaxHealth);
        }

        public void UpdateMaxHealth(int amount) => Model.UpdateMaxHealth(amount);
        public void UpdateHealth(int amount) => Model.UpdateHealth(amount);
        public void UpdateSpeed(float amount) => Model.UpdateSpeed(amount);
        public void UpdateDamage(int amount) => Model.UpdateDamage(amount);
        public void UpdateSkillPoints(int amount) => Model.UpdateSkillPoints(amount);

        /// <summary>增加经验并结算升级。升级规则见 PlayerStatsModel.AddExp。</summary>
        public void AddExp(int amount) => Model.AddExp(amount);

        /// <summary>单例没有场景身份，存档注册表按实例登记。</summary>
        public DataDefinition GetDataID() => null;

        /// <summary>把当前数值写进存档。</summary>
        public void SaveData(Data data) => data.playerStatsData = GetStats();

        /// <summary>从存档恢复数值。存档里没有数值段时保留当前状态。</summary>
        public void LoadData(Data data)
        {
            if (data.playerStatsData == null) return;

            LoadStats(data.playerStatsData);
        }
    }
}
