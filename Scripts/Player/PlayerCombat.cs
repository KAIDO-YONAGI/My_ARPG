using UnityEngine;
using MyEnums;
public class PlayerCombat : MonoBehaviour
{
    public Transform attackPoint;
    public LayerMask enemyMask;//需要在unity中创建并且标记
    public PlayerMovement playerMovement;



    public void DealDamage()
    {
        //TODO:可以引入空间优化算法
        StrengthBUff();

        Collider2D[] enemis = Physics2D.OverlapCircleAll(
            attackPoint.position,
            StatsManager.instance.GetWeaponRange(),
            enemyMask);

        foreach (Collider2D enemy in enemis)
        {
            enemy.GetComponent<EnemyHealth>().
            ChangeHealth(-StatsManager.instance.GetDamage());
            enemy.GetComponent<EnemyKnockBack>().Knockback(
            transform,
            StatsManager.instance.GetKnockBackForce(),
            StatsManager.instance.GetStunTime(),
            StatsManager.instance.GetKnockBackTime());
        }

    }
    private void StrengthBUff()
    {
        StatsManager.instance.UpdateDamage(1);//伤害+1
    }
    public void FinshCombat()
    {
        playerMovement.AnimatorSM(PlayerState.Idle);
        playerMovement.animator.SetBool("isAttacking", false);
        playerMovement.SetCanBeInterrupted(true);
        playerMovement.ResetTimer();
    }
    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawWireSphere(attackPoint.position, StatsManager.instance.weaponRange);
    //}
}
