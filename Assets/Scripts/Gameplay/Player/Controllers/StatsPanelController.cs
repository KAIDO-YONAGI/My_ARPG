using Gameplay.Player.Models;
using Gameplay.Player.Services;
using Gameplay.Player.Views;

namespace Gameplay.Player.Controllers
{
    /// <summary>
    /// 属性面板的控制器，纯 C# 类，由 StatsPanelView 创建并托管。
    /// 订阅 PlayerStatsModel.StatsChanged，把当前 damage/speed 推给 StatsPanelView.SetStats。
    /// 只在界面打开期间存活；保留实际订阅的模型，关闭时对同一对象退订。
    /// </summary>
    public class StatsPanelController
    {
        private readonly StatsPanelView view;
        private readonly IPlayerStatsReadOnly stats;

        public StatsPanelController(StatsPanelView view)
        {
            this.view = view;
            stats = StatsService.Instance.Stats;
            stats.StatsChanged += Refresh;
        }

        public void Dispose()
        {
            stats.StatsChanged -= Refresh;
        }

        public void Refresh()
        {
            view.SetStats(stats.Damage, stats.Speed);
        }
    }
}
