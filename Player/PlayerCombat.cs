using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public Transform attackPoint;
    public LayerMask enemyMask;//需要在unity中创建并且标记
    public StatsUI statsUI;
    public PlayerMovement playerMovement;
    private float Timer = 0;
    private void Update()
    {
        if (Timer > 0)
            Timer -= Time.deltaTime;
    }
    public void Attack()
    {
        if (Timer > 0) return;
        playerMovement.ChangeState(PlayerMovement.PlayerState.Attacking);
        Timer = StatsManager.Instance.coolDown;

    }
    public void FinshCombat()
    {
        playerMovement.ChangeState(PlayerMovement.PlayerState.Idle);
    }

    public void DealDamage()
    {

        StrengthBUff();

        Collider2D[] enemis = Physics2D.OverlapCircleAll(attackPoint.position, StatsManager.Instance.weaponRange, enemyMask);

        if (enemis.Length > 0)
        {
            enemis[0].GetComponent<EnemyHealth>().ChangeHealth(-(StatsManager.Instance.damage));
            enemis[0].GetComponent<EnemyKnockBack>().Knockback(transform, StatsManager.Instance.knockBackForce, StatsManager.Instance.stunTime, StatsManager.Instance.knockBackTime);
        }
    }
    private void StrengthBUff()
    {
        StatsManager.Instance.damage += 1;
        statsUI.UpdateDamage();
    }

    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawWireSphere(attackPoint.position, StatsManager.Instance.weaponRange);
    //}
}
