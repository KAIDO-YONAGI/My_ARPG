using Gameplay.Player.Services;
using Gameplay.Player.Views;

namespace Gameplay.Player.Controllers
{
    /// <summary>
    /// 经验与等级的控制器（纯 C#，由 ExperiencePanelView 创建并托管，不上场景、无单例）。
    /// 把击杀事件（EnemyHealth.OnDefeated）接入 StatsService；界面刷新由 ExpChanged 事件驱动，
    /// 加经验与读档都会触发。升级判定与经验曲线位于 PlayerStatsModel.AddExp；
    /// 技能点由 SkillTreeManager 订阅 PlayerStatsModel.LevelUp 发放。
    /// </summary>
    public class ExperienceController
    {
        private readonly ExperiencePanelView view;
        private bool listeningToKills;
        private bool listeningToModel;

        public ExperienceController(ExperiencePanelView view)
        {
            this.view = view;

            listeningToKills = true;
            EnemyHealth.OnDefeated += GainExp;

            listeningToModel = true;
            StatsService.Instance.Model.ExpChanged += Refresh;
        }

        /// <summary>面板失活时挂起：退订击杀事件（static 事件生命周期长于面板）。</summary>
        public void Suspend()
        {
            if (!listeningToKills) return;
            listeningToKills = false;
            EnemyHealth.OnDefeated -= GainExp;
        }

        /// <summary>面板重激活时恢复：重新订阅击杀事件并补刷。</summary>
        public void Resume()
        {
            if (listeningToKills) return;
            listeningToKills = true;
            EnemyHealth.OnDefeated += GainExp;

            Refresh();
        }

        public void Dispose()
        {
            Suspend();

            if (!listeningToModel) return;
            listeningToModel = false;

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
