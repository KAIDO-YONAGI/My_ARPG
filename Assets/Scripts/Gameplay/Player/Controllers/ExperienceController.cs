using Gameplay.Player.Models;
using Gameplay.Player.Services;
using Gameplay.Player.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Player.Controllers
{
    /// <summary>
    /// 经验与等级的控制器。把击杀事件接入 <see cref="StatsService"/>，并把结算结果交给
    /// <see cref="ExperienceView"/> 显示到经验条与等级文本。
    ///
    /// 界面的刷新由 PlayerStatsModel.ExpChanged 事件驱动，加经验与读档都会触发该事件。
    ///
    /// 升级判定与经验曲线位于 PlayerStatsModel.AddExp。技能点由 SkillTreeManager 订阅
    /// PlayerStatsModel.LevelUp 发放。
    /// </summary>
    public class ExperienceController : YSingleton<ExperienceController>
    {
        [SerializeField] private Slider expSlider;
        [SerializeField] private TMP_Text currentLevelText;

        private ExperienceView view;
        private bool listeningToModel;

        private void OnEnable()
        {
            EnemyHealth.OnDefeated += GainExp;
            TrySubscribeModel();
        }

        private void OnDisable()
        {
            EnemyHealth.OnDefeated -= GainExp;
            UnsubscribeModel();
        }

        private void Start()
        {
            // 订阅写成可重试：Start 在所有 Awake 之后执行，此时 StatsService 已经就绪。
            // 初始显示在 Start 中刷新一次。
            TrySubscribeModel();
            UpdateUI();
        }

        private void TrySubscribeModel()
        {
            if (listeningToModel || StatsService.Instance == null) return;

            listeningToModel = true;
            StatsService.Instance.Model.ExpChanged += UpdateUI;
        }

        private void UnsubscribeModel()
        {
            if (!listeningToModel) return;
            listeningToModel = false;

            if (StatsService.Instance == null) return;
            StatsService.Instance.Model.ExpChanged -= UpdateUI;
        }

        public void GainExp(int amount)
        {
            StatsService.Instance.AddExp(amount); // 结算与升级判定在 Model；刷 UI 由 ExpChanged 触发
        }

        public void UpdateUI()
        {
            PlayerStatsModel model = StatsService.Instance.Model;

            view ??= new ExperienceView(expSlider, currentLevelText);
            view.SetExp(model.CurrentExp, model.ExpToUpgrade, model.Level);
        }
    }
}
