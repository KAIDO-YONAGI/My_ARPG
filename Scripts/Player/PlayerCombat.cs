using UnityEngine;
using MyEnums;
public class PlayerCombat : MonoBehaviour
{
    public Transform attackPoint;
    public LayerMask enemyMask;//需要在unity中创建并且标记
    public StatsUI statsUI;
    public PlayerMovement playerMovement;



    public void DealDamage()
    {

        StrengthBUff();

        Collider2D[] enemis = Physics2D.OverlapCircleAll(attackPoint.position, StatsManager.instance.GetWeaponRange(), enemyMask);

        if (enemis.Length > 0)
        {
            enemis[0].GetComponent<EnemyHealth>().ChangeHealth(-(StatsManager.instance.GetDamage()));
            enemis[0].GetComponent<EnemyKnockBack>().Knockback(transform, StatsManager.instance.GetKnockBackForce(), StatsManager.instance.GetStunTime(), StatsManager.instance.GetKnockBackTime());
        }
    }
    private void StrengthBUff()
    {
        StatsManager.instance.SetDamage(StatsManager.instance.GetDamage() + 1);
        statsUI.UpdateDamage();
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
