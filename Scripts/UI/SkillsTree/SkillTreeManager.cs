using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class SkillTreeManager : MonoBehaviour
{
    public SkillSlot[] skillSlots;
    public TMP_Text pointsText;
    public int availablePoints;


    private void OnEnable()
    {
        SkillSlot.OnAbilityPointSpent += HandleAbilityPointSpent;
        SkillSlot.OnMaxSkillLevel += HandleSkillMaxed;
        ExpManager.OnLevelUp += UpdateAbilityPoints;
    }


    private void OnDisable()
    {
        SkillSlot.OnAbilityPointSpent -= HandleAbilityPointSpent;
        SkillSlot.OnMaxSkillLevel -= HandleSkillMaxed;
        ExpManager.OnLevelUp -= UpdateAbilityPoints;

    }
    private void HandleAbilityPointSpent(SkillSlot skillSlot)
    {
        if (availablePoints > 0)
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
        foreach (SkillSlot slot in skillSlots)
        {
            slot.skillButton.onClick.AddListener(()=>{
                if (availablePoints > 0)
                    slot.TryUpgradeSkill();
            });//注册事件处理器，但是由unity刷新时响应每次的事件
        }
        UpdateAbilityPoints(0);
    }
    public void UpdateAbilityPoints(int amount)
    {
        availablePoints += amount;
        pointsText.text = "Skill POints: " + availablePoints.ToString();
    }
}
