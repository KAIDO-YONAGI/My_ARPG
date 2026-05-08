using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ExpManager : MonoBehaviour
{
    public Slider expSlider;
    public TMP_Text currentLevelText;
    public static event Action<int> OnLevelUp;
    static public ExpManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    private void Start()
    {
        UpdateUI();
    }

    private void OnEnable()
    {
        EnemyHealth.OnDefeated += GainExp;
    }

    private void OnDisable()
    {
        EnemyHealth.OnDefeated -= GainExp;
    }
    public void GainExp(int amount)
    {
        var stats = StatsManager.instance.GetStats();
        stats.currentExp += amount;
        if (stats.currentExp >= stats.expToUpgrade)
        {
            LevelUp();
        }
        UpdateUI();
    }
    public void UpdateUI()
    {
        var stats = StatsManager.instance.GetStats();
        expSlider.maxValue = stats.expToUpgrade;
        expSlider.value = stats.currentExp;
        currentLevelText.text = "Level:" + stats.level;
    }
    private void LevelUp()
    {
        var stats = StatsManager.instance.GetStats();
        stats.level++;
        stats.currentExp -= stats.expToUpgrade;
        stats.expToUpgrade = Mathf.RoundToInt(stats.expToUpgrade * stats.expMutiplier);
        OnLevelUp?.Invoke(1);
    }
}
