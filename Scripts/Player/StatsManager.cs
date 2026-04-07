using UnityEngine;
using TMPro;
public class StatsManager : MonoBehaviour
{
    public static StatsManager instance;//单例模式
    public TMP_Text healthText;
    public StatsUI statsUI;

    [SerializeField, Header("Combat Stats")]
    private int damage;
    public int GetDamage() { return damage; }
    public void SetDamage(int value) { damage = value; }

    private float weaponRange;
    public float GetWeaponRange() { return weaponRange; }
    public void SetWeaponRange(float value) { weaponRange = value; }

    private float knockBackForce;
    public float GetKnockBackForce() { return knockBackForce; }
    public void SetKnockBackForce(float value) { knockBackForce = value; }

    private float knockBackTime;
    public float GetKnockBackTime() { return knockBackTime; }
    public void SetKnockBackTime(float value) { knockBackTime = value; }

    private float stunTime;
    public float GetStunTime() { return stunTime; }
    public void SetStunTime(float value) { stunTime = value; }

    private float coolDown;
    public float GetCoolDown() { return coolDown; }
    public void SetCoolDown(float value) { coolDown = value; }

    [SerializeField, Header("Movement Stats")]
    private float speed;
    public float GetSpeed() { return speed; }
    public void SetSpeed(float value) { speed = value; }

    [SerializeField, Header("Health Stats")]
    private int maxHealth;
    public int GetMaxHealth() { return maxHealth; }
    public void SetMaxHealth(int value) { maxHealth = value; }
    private int currentHealth;

    public int GetCurrentHealth() { return currentHealth; }
    public void SetCurrentHealth(int value) { currentHealth = value; }

    private void Awake()//每次唤醒就检测单例，如果已有有就删除
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    private void UpdateHealthText()
    {
        Animator animator = healthText.GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play("TextUpdate");
        }
        healthText.text = "HP:" + currentHealth + "/" + maxHealth;
    }
    public void UpdateMaxHealth(int amount)
    {
        maxHealth += amount;
        UpdateHealthText();
    }
    public void UpdateHealth(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
        UpdateHealthText();
    }
    public void UpdateSpeed(float amount)
    {
        speed += amount;
    }

    public void UpdateDamage(int amount)
    {
        damage += amount;
    }
}