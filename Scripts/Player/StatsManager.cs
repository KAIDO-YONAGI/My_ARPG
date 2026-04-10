using UnityEngine;
using TMPro;
public class StatsManager : MonoBehaviour
{
    public static StatsManager instance;//单例模式
    public TMP_Text healthText;
    public StatsUI statsUI;


    [SerializeField]private int damage;
    [SerializeField] private float weaponRange;
    [SerializeField] private float knockBackForce;
    [SerializeField] private float knockBackTime;
    [SerializeField] private float stunTime;
    [SerializeField] private float coolDown;
    [SerializeField]private float speed;
    [SerializeField]private int maxHealth;
    [SerializeField]private int currentHealth;
    public int GetDamage() { return damage; }
    public void SetDamage(int value) { damage = value; }

    public float GetWeaponRange() { return weaponRange; }
    public void SetWeaponRange(float value) { weaponRange = value; }

    public float GetKnockBackForce() { return knockBackForce; }
    public void SetKnockBackForce(float value) { knockBackForce = value; }

    public float GetKnockBackTime() { return knockBackTime; }
    public void SetKnockBackTime(float value) { knockBackTime = value; }

    public float GetStunTime() { return stunTime; }
    public void SetStunTime(float value) { stunTime = value; }

    public float GetCoolDown() { return coolDown; }
    public void SetCoolDown(float value) { coolDown = value; }

    public float GetSpeed() { return speed; }
    public void SetSpeed(float value) { speed = value; }


    public int GetMaxHealth() { return maxHealth; }
    public int GetCurrentHealth() { return currentHealth; }
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