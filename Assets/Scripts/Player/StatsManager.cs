using UnityEngine;
using System;
[Serializable]
public class PlayerStatsData
{
    public int damage;
    public float weaponRange;
    public float knockBackForce;
    public float knockBackTime;
    public float stunTime;
    public float coolDown;
    public float speed;
    public int maxHealth;
    public int currentHealth;
    public int skillPoints;
    public int level;
    public int currentExp;
    public int expToUpgrade;
    public float expMutiplier;
}

public class StatsManager : MonoBehaviour
{
    public static StatsManager instance;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    [SerializeField] private PlayerStatsData stats = new();

    public PlayerStatsData GetStats() => stats;
    public void LoadStats(PlayerStatsData data)
    {
        stats = data;
    }
    public int GetDamage() => stats.damage;
    public float GetWeaponRange() => stats.weaponRange;
    public float GetKnockBackForce() => stats.knockBackForce;
    public float GetKnockBackTime() => stats.knockBackTime;
    public float GetStunTime() => stats.stunTime;
    public float GetCoolDown() => stats.coolDown;
    public float GetSpeed() => stats.speed;
    public int GetMaxHealth() => stats.maxHealth;
    public int GetCurrentHealth() => stats.currentHealth;
    public int GetSkillPoints() => stats.skillPoints;
    public int GetLevel() => stats.level;
    public int GetCurrentExp() => stats.currentExp;
    public int GetExpToUpgrade() => stats.expToUpgrade;
    public float GetExpMutiplier() => stats.expMutiplier;

    public void SetWeaponRange(float value) => stats.weaponRange = value;
    public void SetKnockBackForce(float value) => stats.knockBackForce = value;
    public void SetKnockBackTime(float value) => stats.knockBackTime = value;
    public void SetStunTime(float value) => stats.stunTime = value;
    public void SetCoolDown(float value) => stats.coolDown = value;
    public void SetHealth(int health)
    {
        stats.currentHealth = Math.Min(health, stats.maxHealth);
        HealthCanvasManager.instance.UpdateHealthText();
    }
    public void Respwan()
    {
        if (stats.currentHealth <= 0)
            stats.currentHealth = stats.maxHealth;
        HealthCanvasManager.instance.UpdateHealthText();
    }

    public void UpdateMaxHealth(int amount)
    {
        stats.maxHealth += amount;
        HealthCanvasManager.instance.UpdateHealthText();
    }
    public void UpdateHealth(int amount)
    {
        stats.currentHealth += amount;
        if (stats.currentHealth > stats.maxHealth)
            stats.currentHealth = stats.maxHealth;
        HealthCanvasManager.instance.UpdateHealthText();
    }
    public void UpdateSpeed(float amount)
    {
        stats.speed += amount;
        StatsCanvasManager.instance.UpdateSpeed();
    }
    public void UpdateDamage(int amount)
    {
        stats.damage += amount;
        StatsCanvasManager.instance.UpdateDamage();
    }
    public void UpdateSkillPoints(int amount) => stats.skillPoints += amount;

}