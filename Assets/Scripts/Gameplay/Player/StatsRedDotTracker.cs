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

        // 事件是否已经订阅；订阅与退订都先看它，避免同一委托挂两次或重复退订。
        private bool subscribed;
        // 叶子是否已经挂到红点管理器；挂接成功后 LateUpdate 不再重试。
        private bool initialized;
        // 已查看的伤害基线；作为判断本次变化是否为新提升的比较起点。
        private int baselineDamage;
        // 已查看的速度基线；与伤害分别比较，任意一项变大都要提醒。
        private float baselineSpeed;

        /// <summary>组件启用时先尝试初始化；此时依赖可能尚未就绪，未完成的部分由 Start 与 LateUpdate 继续补齐。</summary>
        private void OnEnable()
        {
            TryInitialize();
        }

        /// <summary>在所有 Awake 之后补一次初始化，覆盖 OnEnable 时数值服务与红点管理器尚未就绪的情况。</summary>
        private void Start()
        {
            TryInitialize();
        }

        /// <summary>初始化完成前逐帧重试，等到数值服务与红点管理器就绪后建立叶子并登记画布可用性。</summary>
        private void LateUpdate()
        {
            if (initialized) return;

            TryInitialize();
        }

        /// <summary>组件停用时释放订阅；重新启用后由 OnEnable 重新订阅，并把当时的数值重新记为基线。</summary>
        private void OnDisable()
        {
            ReleaseSubscriptions();
        }

        /// <summary>销毁前退订并把叶子从红点树摘除，避免已销毁实例的委托与树节点残留。</summary>
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

        /// <summary>依赖就绪时补上事件订阅并建立红点叶子；两项依赖就绪顺序不固定，缺一项先返回，由后续生命周期回调重试。</summary>
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

        /// <summary>数值服务就绪后订阅变化与读档事件，并在订阅前把当前数值记为已查看基线，保证首次触发有比较起点。</summary>
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

        /// <summary>对实际订阅的数值视图成对退订，避免静态事件继续指向已停用或已销毁的组件。</summary>
        private void ReleaseSubscriptions()
        {
            if (!subscribed) return;

            subscribed = false;
            stats.StatsChanged -= OnStatsChanged;
            stats.StatsLoaded -= OnStatsLoaded;
            stats = null;
        }

        /// <summary>数值变化时比较当前值与已查看基线：只有伤害或速度变大才标记未查看；基线始终跟进当前值，下降不提醒、后续再次提升仍能提醒。</summary>
        private void OnStatsChanged()
        {
            if (stats == null) return;

            bool increased = stats.Damage > baselineDamage || stats.Speed > baselineSpeed;
            SyncBaseline();

            if (!increased) return;

            if (leaf != null) leaf.MarkUnread();
        }

        /// <summary>读档重建数值后把基线同步为恢复后的值并清除未查看标记，避免读档被当成新增提升。</summary>
        private void OnStatsLoaded()
        {
            SyncBaseline();

            if (leaf != null) leaf.ClearUnread();
        }

        /// <summary>把当前伤害与速度写为已查看基线供后续比较；数值视图尚未就绪时保持原值。</summary>
        private void SyncBaseline()
        {
            if (stats == null) return;

            baselineDamage = stats.Damage;
            baselineSpeed = stats.Speed;
        }

        /// <summary>属性画布的可用性判定：数值服务就绪才允许红点导航把玩家带到属性面板。</summary>
        private static bool IsStatsAvailable()
        {
            return StatsService.Instance != null;
        }
    }
}
