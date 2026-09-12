using TMPro;
using UnityEngine;

public class HealthCanvasManager : YSingleton<HealthCanvasManager>
{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Animator healthTextAnimator;

    // 运行时绑定 StatsManager 的数值模型（事件在模型上）；初始绑定放在 Start：
    // 面板可能随 GamePlay 根节点延迟激活，那时 StatsManager 必然已就绪。
    private PlayerStatsModel model;

    private void Start()
    {
        model = StatsManager.Instance.Model;
        model.HealthChanged += UpdateHealthText;
        UpdateHealthText();
    }

    private void OnEnable()
    {
        // 重激活时补一次刷新：失活期间错过的事件没有累积通知
        if (model != null)
            UpdateHealthText();
    }

    private void OnDestroy()
    {
        if (model != null)
            model.HealthChanged -= UpdateHealthText;
    }

    public void UpdateHealthText()
    {
        if (model == null || healthText == null) return;

        if (healthTextAnimator != null)
        {
            healthTextAnimator.Play("TextUpdate");
        }
        healthText.text = "HP:" + model.CurrentHealth + "/" + model.MaxHealth;
    }
}
