using Contracts.Base;
using Contracts.Enums;
using Gameplay.Player.Models;
using Gameplay.Player.Services;
using Pipeline.UI.RedDot;

namespace Gameplay.Player
{
    /// <summary>
    /// 属性画布红点的跟踪器。常驻场景组件，把伤害与速度的提升折算成属性画布的未查看状态，
    /// 面板绘制完成后由 <see cref="ConfirmDisplayed"/> 清除。
    /// </summary>
    /// <remarks>
    /// 只跟踪属性面板实际展示的伤害与速度两项，回血、受伤与技能点变化都不产生提醒。
    /// 基线是上一次看到的数值：订阅建立时取当前值，每次变化都跟进到当前值，
    /// 只有伤害或速度变大时才留下未查看标记。连续提升只保留一个标记，计数不叠加；
    /// 数值下降（增益到期）只下移基线，不产生提醒，后续再次提升仍能提醒。
    /// 读档恢复的数值是一次整体替换，<see cref="PlayerStatsModel.StatsLoaded"/> 会先把基线同步为恢复后的数值
    /// 再清除未查看状态，读档因此不算新增提醒。
    /// 数值服务与红点管理器都由常驻场景持有，Awake 顺序不保证，初始化在 Start 与 LateUpdate 重试。
    /// </remarks>
    public sealed class StatsRedDotTracker : YSingleton<StatsRedDotTracker>
    {
        // 属性画布的红点叶子；与红点管理器同时就绪后建立。
        private StatsRedDotLeaf leaf;

        // 订阅的只读数值视图；退订时对同一对象操作。
        private IPlayerStatsReadOnly stats;

        private bool subscribed;
        private bool initialized;
        private int baselineDamage;
        private float baselineSpeed;

        private void OnEnable()
        {
            TryInitialize();
        }

        private void Start()
        {
            TryInitialize();
        }

        private void LateUpdate()
        {
            if (initialized) return;

            TryInitialize();
        }

        private void OnDisable()
        {
            ReleaseSubscriptions();
        }

        protected override void OnDestroy()
        {
            ReleaseSubscriptions();

            RedDotManager manager = RedDotManager.Instance;
            if (initialized && manager != null)
                manager.RemoveLeaf(leaf);

            base.OnDestroy();
        }

        /// <summary>面板已经画出最新数值之后调用：把当前值记为已查看并清除未查看标记。</summary>
        public void ConfirmDisplayed()
        {
            if (leaf != null) leaf.ClearUnread();

            SyncBaseline();
        }

        // 数值服务与红点管理器先后就绪，两项都可用后才建立订阅与叶子。
        private void TryInitialize()
        {
            TrySubscribe();
            if (initialized) return;

            RedDotManager manager = RedDotManager.Instance;
            if (manager == null || stats == null) return;

            if (leaf == null) leaf = new StatsRedDotLeaf();
            if (!manager.AddLeaf(CanvasType.Stats, leaf, 0)) return;

            manager.RegisterCanvas(CanvasType.Stats, IsStatsAvailable);
            initialized = true;
        }

        // 数值服务就绪后订阅变化事件；订阅前把当前数值记为已查看基线。
        private void TrySubscribe()
        {
            if (subscribed) return;

            StatsService service = StatsService.Instance;
            if (service == null) return;

            stats = service.Stats;
            if (stats == null) return;

            SyncBaseline();
            stats.StatsChanged += OnStatsChanged;
            stats.StatsLoaded += OnStatsLoaded;
            subscribed = true;
        }

        // 退订时对实际订阅的对象操作，避免静态事件残留。
        private void ReleaseSubscriptions()
        {
            if (!subscribed) return;

            subscribed = false;
            stats.StatsChanged -= OnStatsChanged;
            stats.StatsLoaded -= OnStatsLoaded;
            stats = null;
        }

        // 伤害或速度实际提升时留下一个未查看标记；基线始终跟进当前值，
        // 数值下降（受伤、增益到期）不推进未查看标记，但基线随之下移，后续再次提升仍能提醒。
        private void OnStatsChanged()
        {
            if (stats == null) return;

            bool increased = stats.Damage > baselineDamage || stats.Speed > baselineSpeed;
            SyncBaseline();

            if (!increased) return;

            if (leaf != null) leaf.MarkUnread();
        }

        // 读档恢复的数值直接算作已查看，避免读档被当成新增提醒。
        private void OnStatsLoaded()
        {
            SyncBaseline();

            if (leaf != null) leaf.ClearUnread();
        }

        // 用当前数值刷新已查看基线；数值服务尚未就绪时保持不变。
        private void SyncBaseline()
        {
            if (stats == null) return;

            baselineDamage = stats.Damage;
            baselineSpeed = stats.Speed;
        }

        // 属性画布的可用性：数值服务就绪才能打开属性面板。
        private static bool IsStatsAvailable()
        {
            return StatsService.Instance != null;
        }
    }
}
