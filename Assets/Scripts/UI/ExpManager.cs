using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
public class ExpManager : MonoBehaviour
{
    public int level = 0;
    public int currentExp;
    public int expToUpgrade = 10;
    public float mutiplier = 1.2f;
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

    private void OnEnable()//用于观察者模式中订阅、监听事件
    {
        EnemyHealth.OnDefeated += GainExp;
    }

    private void OnDisable()//取消监听
    {
        EnemyHealth.OnDefeated -= GainExp;

    }
    public void GainExp(int amount)
    {
        currentExp += amount;
        if (currentExp >= expToUpgrade)
        {
            LevelUp();
        }
        UpdateUI();//注意升级之后要改
    }
    public void UpdateUI()
    {
        expSlider.maxValue = expToUpgrade;
        expSlider.value = currentExp;
        currentLevelText.text = "Level:" + level;
    }
    private void LevelUp()
    {
        level++;
        currentExp -= expToUpgrade;
        expToUpgrade = Mathf.RoundToInt(expToUpgrade * mutiplier);
        OnLevelUp?.Invoke(1);//事件被触发
    }
}
