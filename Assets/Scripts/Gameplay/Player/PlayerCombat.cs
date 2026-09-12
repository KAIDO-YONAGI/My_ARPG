using UnityEngine;
using MyEnums;
using Gameplay.Player.Services;
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyMask;//需要在unity中创建并且标记
    [SerializeField] private VoidEventSO slashActionFinishedEvent;

    /// <summary>
    /// 当前是否为激活的武器模式。
    /// Player 根节点上的 PlayerAnimationEventRelay 会按此状态转发动画事件。
    /// </summary>
    public bool IsActive { get; private set; } = true;

    public void SetActive(bool active) => IsActive = active;



    public void DealDamage()
    {
        //TODO:可以引入空间优化算法
        Collider2D[] enemis = Physics2D.OverlapCircleAll(
            attackPoint.position,
            StatsService.Instance.Model.WeaponRange,
            enemyMask);

        foreach (Collider2D enemy in enemis)
        {
            // 例外保留：碰撞/命中对象运行时才知道是谁，无法预引用（见重构清单 GetComponent 治理一节）
            if (!enemy.TryGetComponent<IDamageable>(out var damageable)) continue;
            damageable.TakeDamage(StatsService.Instance.Model.Damage, transform);
        }

    }
    public void FinishCombat()
    {
        // 动画事件触发：通知 PlayerMovement（及其它订阅者）近战动作结束，由其统一重置状态
        slashActionFinishedEvent.OnEventRaised();
    }

    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawWireSphere(attackPoint.position, StatsService.Instance.Model.WeaponRange);
    //}
}
