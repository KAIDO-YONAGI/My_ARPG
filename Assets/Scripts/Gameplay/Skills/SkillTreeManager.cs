using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Gameplay.Player.Services;
public class SkillTreeManager : MonoBehaviour
{
    [SerializeField] private SkillSlot[] skillSlots;
    [SerializeField] private TMP_Text pointsText;

    private bool listeningToLevelUp;

    private void OnEnable()
    {
        SkillSlot.OnAbilityPointSpent += HandleAbilityPointSpent;
        SkillSlot.OnMaxSkillLevel += HandleSkillMaxed;
        TrySubscribeLevelUp();
    }


    private void OnDisable()
    {
        SkillSlot.OnAbilityPointSpent -= HandleAbilityPointSpent;
        SkillSlot.OnMaxSkillLevel -= HandleSkillMaxed;
        UnsubscribeLevelUp();

    }

    // OnEnable 可能早于 StatsService 的 Awake，所以订阅写成可重试的；Start 一定在所有 Awake 之后。
    private void TrySubscribeLevelUp()
    {
        if (listeningToLevelUp || StatsService.Instance == null) return;

        listeningToLevelUp = true;
        StatsService.Instance.Model.LevelUp += UpdateAbilityPoints; // 升级事件由模型发出
    }

    private void UnsubscribeLevelUp()
    {
        if (!listeningToLevelUp) return;
        listeningToLevelUp = false;

        if (StatsService.Instance == null) return;
        StatsService.Instance.Model.LevelUp -= UpdateAbilityPoints;
    }

    private void HandleAbilityPointSpent(SkillSlot skillSlot)
    {
        if (StatsService.Instance.Model.SkillPoints > 0)
        {
            UpdateAbilityPoints(-1);
        }
    }

    private void HandleSkillMaxed(SkillSlot skillSlot)//传入slot以便获知哪个技能槽满了
    {
        foreach (SkillSlot slot in skillSlots)
        {
            if (slot.isUnlocked == false && slot.CanUnlockSkill())
                slot.Unlock();
        }
    }
    private void Start()
    {
        TrySubscribeLevelUp();

        foreach (SkillSlot slot in skillSlots)
        {
            slot.skillButton.onClick.AddListener(()=>{
                if (StatsService.Instance.Model.SkillPoints > 0)
                    slot.TryUpgradeSkill();
            });//注册事件处理器，但是由unity刷新时响应每次的事件
        }
        UpdateAbilityPoints(0);
    }
    public void UpdateAbilityPoints(int amount)
    {
        StatsService.Instance.Model.UpdateSkillPoints(amount);
        pointsText.text = "Skill Points: " + StatsService.Instance.Model.SkillPoints.ToString();
    }
}
