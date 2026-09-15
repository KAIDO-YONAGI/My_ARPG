using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gameplay.Player.Services;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int currentHealth;
    [SerializeField] private int maxHealth;
    [SerializeField] private int expReward = 2;
    [SerializeField] private EnemyDefeatedEventSO defeatedEvent;

    private EnemyKnockBack knockBack;

    private void Awake()
    {
        knockBack = GetComponent<EnemyKnockBack>();
    }
    private void Start()
    {
        currentHealth = maxHealth;
    }
    public void ChangeHealth(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        else if (currentHealth <= 0)
        {
            defeatedEvent.OnEnemyDefeated(expReward, transform);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// IDamageable：扣血 + 击退一次完成。玩家攻击命中只需调这个方法。
    /// </summary>
    public void TakeDamage(int damage, Transform attacker)
    {
        ChangeHealth(-damage);
        if (knockBack != null)
        {
            knockBack.Knockback(
                attacker,
                StatsService.Instance.Stats.KnockBackForce,
                StatsService.Instance.Stats.StunTime,
                StatsService.Instance.Stats.KnockBackTime);
        }
    }
}
