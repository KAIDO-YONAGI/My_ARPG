using TMPro;
using UnityEngine;
using Gameplay.Player.Controllers;

namespace Gameplay.Player.Views
{
    /// <summary>
    /// 血量文本的显示层：面板唯一的场景组件，持有控件引用并托管纯 C# 的 HealthController
    /// （Start 创建并首刷，OnDestroy 销毁）。
    /// 订阅模型事件与取数都在 Controller，View 不接触 PlayerStatsModel。
    /// </summary>
    public class HealthView : MonoBehaviour
    {
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Animator healthTextAnimator;

        private HealthController controller;

        private void Start()
        {
            // Start 在所有 Awake 之后：此时 StatsService 必然就绪，Controller 可以立即订阅
            controller = new HealthController(this);
            controller.Refresh();
        }

        private void OnDestroy()
        {
            controller?.Dispose();
            controller = null;
        }

        public void SetHp(int current, int max)
        {
            if (healthTextAnimator != null)
            {
                healthTextAnimator.Play("TextUpdate");
            }
            healthText.text = "HP:" + current + "/" + max;
        }
    }
}
