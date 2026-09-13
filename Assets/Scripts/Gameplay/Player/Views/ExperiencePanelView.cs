using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Gameplay.Player.Controllers;

namespace Gameplay.Player.Views
{
    /// <summary>
    /// 经验条与等级文本的显示层，面板唯一的场景组件，持有控件引用并托管纯 C# 的
    /// ExperienceController：Start 时创建并首刷，OnDestroy 时销毁。
    /// 击杀事件的订阅与经验数据的读取都在 Controller。
    /// </summary>
    public class ExperiencePanelView : MonoBehaviour
    {
        [SerializeField] private Slider expSlider;
        [SerializeField] private TMP_Text currentLevelText;

        private ExperienceController controller;

        private void Start()
        {
            // Start 在所有 Awake 之后：此时 StatsService 必然就绪，Controller 可以立即订阅
            controller = new ExperienceController(this);
            controller.Refresh();
        }

        private void OnDestroy()
        {
            controller?.Dispose();
            controller = null;
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
