using Gameplay.Player.Services;
using Gameplay.Player.Views;

namespace Gameplay.Player.Controllers
{
    /// <summary>
    /// 经验与等级的控制器（纯 C#，由 ExperiencePanelView 创建并托管）。
    /// 把击杀事件（EnemyHealth.OnDefeated）接入 Model；界面刷新由 ExpChanged 事件驱动，
    /// 加经验与读档都会触发。升级判定与经验曲线位于 PlayerStatsModel.AddExp；
    /// 技能点由 SkillTreeManager 订阅 PlayerStatsModel.LevelUp 发放。
    /// 订阅在面板失活期间保持——SetExp 只写序列化属性，对失活对象安全。
    /// </summary>
    public class ExperienceController
    {
        private readonly ExperiencePanelView view;

        public ExperienceController(ExperiencePanelView view)
        {
            this.view = view;

            EnemyHealth.OnDefeated += GainExp;
            StatsService.Instance.Model.ExpChanged += Refresh;
        }

        public void Dispose()
        {
            EnemyHealth.OnDefeated -= GainExp;

            if (StatsService.Instance != null)
                StatsService.Instance.Model.ExpChanged -= Refresh;
        }

        public void Refresh()
        {
            var model = StatsService.Instance.Model;
            view.SetExp(model.CurrentExp, model.ExpToUpgrade, model.Level);
        }

        private void GainExp(int amount)
        {
            StatsService.Instance.Model.AddExp(amount); // 结算与升级判定在 Model；刷 UI 由 ExpChanged 触发
        }
    }
}
