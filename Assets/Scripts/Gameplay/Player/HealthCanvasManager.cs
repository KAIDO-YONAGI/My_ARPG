using TMPro;
using UnityEngine;

public class HealthCanvasManager : YSingleton<HealthCanvasManager>
{
    [SerializeField] private TMP_Text healthText;


    public void UpdateHealthText()
    {
        var stats = StatsManager.Instance.GetStats();
        Animator animator = healthText.GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play("TextUpdate");
        }
        healthText.text = "HP:" + stats.currentHealth + "/" + stats.maxHealth;
    }
}
