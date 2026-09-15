using UnityEngine;
using Gameplay.Player.Services;

namespace Gameplay.Player.Controllers
{
    /// <summary>
    /// 血量域的输入侧控制器。订阅 PlayerDamagedEventSO 广播，统一处理扣血、击退
    /// 与死亡编排：请求 GameOver 画布、隐藏玩家根节点。
    /// 显示侧的控制器是 HealthController，负责血量文本；两者分工，这里管输入与后果。
    /// </summary>
    public class PlayerDamageController : MonoBehaviour
    {
        [SerializeField] private GameObject playerRoot;
        [SerializeField] private PlayerDamagedEventSO playerDamagedEvent;
        [SerializeField] private PlayerMovement movement;

        void Start()
        {
            StatsService.Instance.Respawn();
        }

        private void OnEnable()
        {
            playerDamagedEvent.PlayerDamaged += OnDamaged;
        }

        private void OnDisable()
        {
            playerDamagedEvent.PlayerDamaged -= OnDamaged;
        }

        private void OnDamaged(int damage, Transform attacker, float knockBackForce, float stunTime)
        {
            StatsService.Instance.UpdateHealth(-damage);

            if (movement != null)
                movement.KnockBack(attacker, knockBackForce, stunTime);

            if (StatsService.Instance.Stats.CurrentHealth <= 0)
            {
                // GameOver 不配置按键，通过统一 RequestCanvasToggle 请求分支进入焦点栈与阻塞体系。
                UIManager.Instance.RequestCanvasToggle(MyEnums.CanvasToToggle.GameOver);

                if (playerRoot != null)
                    playerRoot.SetActive(false);
            }
        }
    }
}
