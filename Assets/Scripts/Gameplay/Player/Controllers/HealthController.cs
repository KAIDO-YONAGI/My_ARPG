using Gameplay.Player.Models;
using Gameplay.Player.Services;
using Gameplay.Player.Views;

namespace Gameplay.Player.Controllers
{
    /// <summary>
    /// 血量显示的控制器（纯 C#，由 HealthView 创建并托管，不上场景、无单例）。
    /// 订阅 PlayerStatsModel.HealthChanged，把当前血量推给 HealthView.SetHp。
    /// 数值规则在 PlayerStatsModel，View 只负责显示。
    /// </summary>
    public class HealthController
    {
        private readonly HealthView view;
        private bool listening;

        public HealthController(HealthView view)
        {
            this.view = view;
            listening = true;
            StatsService.Instance.Model.HealthChanged += Refresh;
        }

        public void Dispose()
        {
            if (!listening) return;
            listening = false;

            if (StatsService.Instance != null)
                StatsService.Instance.Model.HealthChanged -= Refresh;
        }

        public void Refresh()
        {
            PlayerStatsModel model = StatsService.Instance.Model;
            view.SetHp(model.CurrentHealth, model.MaxHealth);
        }
    }
}
