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
        StatsManager.instance.UpdateHealth(StatsManager.instance.GetMaxHealth());
    }
    public void ChangeHealth(int amount)
    {
        StatsManager.instance.UpdateHealth(amount);        
        animator.Play("TextUpdate");

        if (StatsManager.instance.GetCurrentHealth() <= 0)
        {
            gameObject.SetActive(false);
            
        }
    }
}
