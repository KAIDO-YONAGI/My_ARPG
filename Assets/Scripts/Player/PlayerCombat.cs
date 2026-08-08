using UnityEngine;
using MyEnums;
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyMask;//需要在unity中创建并且标记
    [SerializeField] private VoidEventSO slashActionFinishedEvent;

    /// <summary>
    /// 当前是否为激活的武器模式。替代组件 enabled：
    /// 组件常驻 enabled 才能正常接收 Animation Event（DealDamage/FinishCombat）。
    /// </summary>
    public bool IsActive { get; private set; } = true;

    public void SetActive(bool active) => IsActive = active;



    public void DealDamage()
    {
        //TODO:可以引入空间优化算法
        Collider2D[] enemis = Physics2D.OverlapCircleAll(
            attackPoint.position,
            StatsManager.instance.GetWeaponRange(),
            enemyMask);

        foreach (Collider2D enemy in enemis)
        {
            var damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(StatsManager.instance.GetDamage(), transform);
            }
        }

    }
    public void FinishCombat()
    {
        // 动画事件触发：通知 PlayerMovement（及其它订阅者）近战动作结束，由其统一重置状态
        if (slashActionFinishedEvent != null) slashActionFinishedEvent.OnEventRaised();
    }

    // 动画事件里拼写为 "FinshCombat"（少个 i），此处作为别名接收，避免 no receiver 警告
    public void FinshCombat()
    {
        FinishCombat();
    }
    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawWireSphere(attackPoint.position, StatsManager.instance.weaponRange);
    //}
}
