using TMPro;
using UnityEngine;

public class HealthCanvasManager : MonoBehaviour
{
    private static HealthCanvasManager _instance;
    public static HealthCanvasManager Instance => _instance;
    [SerializeField] private TMP_Text healthText;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

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
