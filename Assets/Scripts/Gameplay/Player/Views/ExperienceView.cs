using Gameplay.Player.Controllers;
using TMPro;
using UnityEngine.UI;

namespace Gameplay.Player.Views
{
    /// <summary>
    /// 经验条与等级文本的显示层：把传进来的数值写进 Slider 与 TMP_Text。
    /// 由 <see cref="ExperienceController"/> 构造并传入数值，规则位于 PlayerStatsModel。
    /// </summary>
    public class ExperienceView
    {
        private readonly Slider expSlider;
        private readonly TMP_Text currentLevelText;

        public ExperienceView(Slider expSlider, TMP_Text currentLevelText)
        {
            this.expSlider = expSlider;
            this.currentLevelText = currentLevelText;
        }

        /// <summary>把一次结算后的经验状态画到界面：进度条上限 = 当前升级阈值，值 = 当前经验。</summary>
        public void SetExp(int currentExp, int expToUpgrade, int level)
        {
            expSlider.maxValue = expToUpgrade;
            expSlider.value = currentExp;
            currentLevelText.text = "Level:" + level;
        }
    }
}
