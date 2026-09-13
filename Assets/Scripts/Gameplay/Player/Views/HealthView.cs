using TMPro;
using UnityEngine;
using Gameplay.Player.Controllers;

namespace Gameplay.Player.Views
{
    /// <summary>
    /// 血量文本的显示层：唯一的场景组件，持有控件引用并托管 <see cref="HealthController"/>。
    /// 订阅模型事件与取数都在 Controller（纯 C#，由本类创建/销毁），View 不接触 PlayerStatsModel。
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

        private void OnEnable()
        {
            // 重激活时补一次刷新：失活期间错过的事件没有累积通知
            controller?.Refresh();
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
