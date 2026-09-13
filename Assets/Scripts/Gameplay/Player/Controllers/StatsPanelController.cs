using Gameplay.Player.Models;
using Gameplay.Player.Services;
using Gameplay.Player.Views;

namespace Gameplay.Player.Controllers
{
    /// <summary>
    /// 属性面板的控制器（纯 C#，由 StatsPanelView 创建并托管，不上场景、无单例）。
    /// 订阅 PlayerStatsModel.StatsChanged，把当前 damage/speed 推给 StatsPanelView.SetStats。
    /// 画布开关/焦点/层级（ICanvasManager）是 UI 共用基础设施，由 View 自理，不经本类。
    /// 数值规则在 PlayerStatsModel，View 只负责显示。
    /// </summary>
    public class StatsPanelController
    {
        private readonly StatsPanelView view;
        private bool listening;

        public StatsPanelController(StatsPanelView view)
        {
            this.view = view;
            listening = true;
            StatsService.Instance.Model.StatsChanged += Refresh;
        }

        public void Dispose()
        {
            if (!listening) return;
            listening = false;

            if (StatsService.Instance != null)
                StatsService.Instance.Model.StatsChanged -= Refresh;
        }

        public void Refresh()
        {
            PlayerStatsModel model = StatsService.Instance.Model;
            view.SetStats(model.Damage, model.Speed);
        }
    }
}
