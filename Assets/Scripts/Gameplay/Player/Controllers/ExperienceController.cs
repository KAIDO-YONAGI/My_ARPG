using UnityEngine;
using Gameplay.Player.Services;
using Gameplay.Player.Views;

namespace Gameplay.Player.Controllers
{
    /// <summary>
    /// 经验与等级的控制器，纯 C# 类，由 ExperiencePanelView 创建并托管。
    /// 击杀输入来自构造注入的 EnemyDefeatedEventSO 通道；界面刷新由 ExpChanged 事件驱动，
    /// 加经验与读档都会触发。升级判定与经验曲线位于 PlayerStatsModel.AddExp；
    /// 技能点由 SkillTreeManager 订阅 PlayerStatsModel.LevelUp 发放。
    /// 订阅在面板失活期间保持，SetExp 只写序列化属性，对失活对象安全。
    /// </summary>
    public class ExperienceController
    {
        private readonly ExperiencePanelView view;
        private readonly EnemyDefeatedEventSO defeatedEvent;

        public ExperienceController(ExperiencePanelView view, EnemyDefeatedEventSO defeatedEvent)
        {
            this.view = view;
            this.defeatedEvent = defeatedEvent;

            defeatedEvent.EnemyDefeated += GainExp;
            StatsService.Instance.Model.ExpChanged += Refresh;
        }

        public void Dispose()
        {
            defeatedEvent.EnemyDefeated -= GainExp;

            if (StatsService.Instance != null)
                StatsService.Instance.Model.ExpChanged -= Refresh;
        }

        public void Refresh()
        {
            var model = StatsService.Instance.Model;
            view.SetExp(model.CurrentExp, model.ExpToUpgrade, model.Level);
        }

        private void GainExp(int exp, Transform defeatedEnemy)
        {
            StatsService.Instance.Model.AddExp(exp); // 结算与升级判定在 Model；刷 UI 由 ExpChanged 触发
        }
    }
}
