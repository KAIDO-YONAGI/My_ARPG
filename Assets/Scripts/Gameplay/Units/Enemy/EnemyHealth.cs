using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int currentHealth;
    [SerializeField] private int maxHealth;
    [SerializeField] private int expReward = 2;

    public delegate void MonsterDefeated(int exp);//观察者模式
    public static event MonsterDefeated OnDefeated;

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
            OnDefeated(expReward);//事件被触发
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
                StatsManager.Instance.GetKnockBackForce(),
                StatsManager.Instance.GetStunTime(),
                StatsManager.Instance.GetKnockBackTime());
        }
    }
}