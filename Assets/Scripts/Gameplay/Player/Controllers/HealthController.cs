using Gameplay.Player.Models;
using Gameplay.Player.Services;
using Gameplay.Player.Views;

namespace Gameplay.Player.Controllers
{
    /// <summary>
    /// 血量显示的控制器，纯 C# 类，由 HealthView 创建并托管。
    /// 订阅 PlayerStatsModel.HealthChanged，把当前血量推给 HealthView.SetHp。
    /// 数值规则在 PlayerStatsModel，View 只负责显示。
    /// </summary>
    public class HealthController
    {
        private readonly HealthView view;

        public HealthController(HealthView view)
        {
            this.view = view;
            StatsService.Instance.Stats.HealthChanged += Refresh;
        }

        public void Dispose()
        {
            if (StatsService.Instance != null)
                StatsService.Instance.Stats.HealthChanged -= Refresh;
        }

        public void Refresh()
        {
            IPlayerStatsReadOnly stats = StatsService.Instance.Stats;
            view.SetHp(stats.CurrentHealth, stats.MaxHealth);
        }
    }
}
