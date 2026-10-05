using UnityEngine;
using Gameplay.Player.Services;

namespace Gameplay.Player.Controllers
{
    /// <summary>
    /// 血量域的输入侧控制器。订阅 PlayerDamagedEventSO 广播，统一处理扣血、击退
    /// 与死亡编排：请求 GameOver 画布、隐藏玩家根节点；订阅 RetryRequestEvent，
    /// 在重试请求时先复活回血，再委托 SceneChanger 重载当前场景组。
    /// 显示侧的控制器是 HealthController，负责血量文本；两者分工，这里管输入与后果。
    /// </summary>
    public class PlayerDamageController : MonoBehaviour
    {
        [SerializeField] private GameObject playerRoot;
        [SerializeField] private PlayerDamagedEventSO playerDamagedEvent;
        [SerializeField] private VoidEventSO retryEventSO;
        [SerializeField] private PlayerMovement movement;

        private void OnEnable()
        {
            playerDamagedEvent.PlayerDamaged += OnDamaged;
            retryEventSO.VoidEvent += OnRetryRequest;
        }

        private void OnDisable()
        {
            playerDamagedEvent.PlayerDamaged -= OnDamaged;
            retryEventSO.VoidEvent -= OnRetryRequest;
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

        /// <summary>
        /// 重试请求：先复活回血，再请求重载当前所在场景组。
        /// 回血必须早于 RequestSceneLoad 的广播段——SaveDataManager 在广播段同步抓存档快照，
        /// 又在场景加载完成后回灌该快照；回血晚于快照抓取，会把死亡态血量写进存档并覆盖回运行时。
        /// 场景组取 SceneChanger 的当前组副本，位置传零向量以复用场景组预设出生点。
        /// </summary>
        private void OnRetryRequest()
        {
            StatsService.Instance.Respawn();
            SceneChanger.Instance.RequestSceneLoad(SceneChanger.Instance.GetCurrentScenes(), Vector3.zero, true);
        }
    }
}
