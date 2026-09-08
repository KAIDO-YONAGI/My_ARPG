using TMPro;
using UnityEngine;

public class HealthCanvasManager : YSingleton<HealthCanvasManager>
{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Animator healthTextAnimator;

    // 运行时绑定 StatsManager 的数据副本（事件在副本身上）；初始绑定放在 Start：
    // 面板可能随 GamePlay 根节点延迟激活，那时 StatsManager 必然已就绪。
    private PlayerStatsSO stats;

    private void Start()
    {
        stats = StatsManager.Instance.RuntimeStats;
        stats.HealthChanged += UpdateHealthText;
        UpdateHealthText();
    }

    private void OnEnable()
    {
        // 重激活时补一次刷新：失活期间错过的事件没有累积通知
        if (stats != null)
            UpdateHealthText();
    }

    private void OnDestroy()
    {
        if (stats != null)
            stats.HealthChanged -= UpdateHealthText;
    }

    public void UpdateHealthText()
    {
        if (stats == null || healthText == null) return;

        var data = stats.Data;
        if (healthTextAnimator != null)
        {
            healthTextAnimator.Play("TextUpdate");
        }
        healthText.text = "HP:" + data.currentHealth + "/" + data.maxHealth;
    }
}
