using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ExpManager : MonoBehaviour
{
    [SerializeField] private Slider expSlider;
    [SerializeField] private TMP_Text currentLevelText;
    public static event Action<int> OnLevelUp;
    private static ExpManager _instance;
    public static ExpManager Instance => _instance;

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
        var stats = StatsManager.Instance.GetStats();
        stats.currentExp += amount;
        if (stats.currentExp >= stats.expToUpgrade)
        {
            LevelUp();
        }
        UpdateUI();
    }
    public void UpdateUI()
    {
        var stats = StatsManager.Instance.GetStats();
        expSlider.maxValue = stats.expToUpgrade;
        expSlider.value = stats.currentExp;
        currentLevelText.text = "Level:" + stats.level;
    }
    private void LevelUp()
    {
        var stats = StatsManager.Instance.GetStats();
        stats.level++;
        stats.currentExp -= stats.expToUpgrade;
        stats.expToUpgrade = Mathf.RoundToInt(stats.expToUpgrade * stats.expMultiplier);
        OnLevelUp?.Invoke(1);
    }
}
