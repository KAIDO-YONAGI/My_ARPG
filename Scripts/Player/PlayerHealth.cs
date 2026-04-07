using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using TMPro;
public class PlayerHealth : MonoBehaviour
{

    public TMP_Text healthText;
    public Animator animator;
    void Start()
    {
        healthText.text = "HP:" + StatsManager.instance.currentHealth + "/" + StatsManager.instance.maxHealth;
    }
    public void changeHealth(int amount)
    {
        StatsManager.instance.currentHealth += amount;
        healthText.text = "HP:" + StatsManager.instance.currentHealth + "/" + StatsManager.instance.maxHealth;
        animator.Play("TextUpdate");

        if (StatsManager.instance.currentHealth <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}
