using Gameplay.Player.Models;
using Gameplay.Player.Services;
using Gameplay.Player.Views;

namespace Gameplay.Player.Controllers
{
    /// <summary>
    /// 属性面板的控制器，纯 C# 类，由 <see cref="StatsCanvasView"/> 创建并托管。
    /// 订阅 <see cref="IPlayerStatsReadOnly.StatsChanged"/>，把当前伤害和速度推给视图。
    /// </summary>
    /// <remarks>
    /// 控制器只在界面打开期间存活；保存构造时实际订阅的只读模型，
    /// 关闭时对同一对象退订，避免缓存重建后出现重复订阅或空引用。
    /// </remarks>
    public class StatsCanvasController
    {
        private readonly StatsCanvasView view;
        private readonly IPlayerStatsReadOnly stats;

        /// <summary>订阅属性变化；由视图在打开时调用。</summary>
        public StatsCanvasController(StatsCanvasView view)
        {
            this.view = view;
            stats = StatsService.Instance.Stats;
            stats.StatsChanged += Refresh;
        }

        /// <summary>退订属性变化；由视图在关闭时调用。</summary>
        public void Dispose()
        {
            stats.StatsChanged -= Refresh;
        }

        /// <summary>把最新伤害和速度推给面板。</summary>
        public void Refresh()
        {
            view.SetStats(stats.Damage, stats.Speed);
        }
    }
}
