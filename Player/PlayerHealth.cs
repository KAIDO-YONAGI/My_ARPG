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
        healthText.text = "HP:" + StatsManager.Instance.currentHealth + "/" + StatsManager.Instance.maxHealth;
    }
    public void changeHealth(int amount)
    {
        StatsManager.Instance.currentHealth += amount;
        healthText.text = "HP:" + StatsManager.Instance.currentHealth + "/" + StatsManager.Instance.maxHealth;
        animator.Play("TextUpdate");

        if (StatsManager.Instance.currentHealth <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}
