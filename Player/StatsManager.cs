using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;//单例模式
    public TMP_Text healthText;
    public TMP_Text speedText;

    public StatsUI statsUI;

    [Header("Combat Stats")]
    public int damage;
    public float weaponRange;
    public float knockBackForce;
    public float knockBackTime;
    public float stunTime;
    public float coolDown;

    [Header("Movement Stats")]
    public float speed;

    [Header("Health Stats")]
    public int maxHealth;
    public int currentHealth;


    private void Awake()//每次唤醒就检测单例，如果已有有就删除
    {
        if (Instance == null) Instance = this;
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
        speedText.text = "Speed" + speed.ToString();
    }

}