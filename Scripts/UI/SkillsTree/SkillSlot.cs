using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
public class SkillSlot : MonoBehaviour
{
    public SkillSO skillSO;
    public Image skillIcon;
    public TMP_Text skillLevelText;
    public Button skillButton;
    public List<SkillSlot> preRiquriedForSkillUnlock_List;

    public int currentLevel;
    public bool isUnlocked = false;

    public static event Action<SkillSlot> OnAbilityPointSpent;//事件广播
    public static event Action<SkillSlot> OnMaxSkillLevel;//事件广播
    private void OnValidate()
    {
        if (skillSO != null && skillLevelText != null)
        {
            UpdateUI();
        }
    }
    private void UpdateUI()
    {
        skillIcon.sprite = skillSO.skillIcon;
        if (isUnlocked)
        {

            skillButton.interactable = true;
            skillLevelText.text = currentLevel.ToString() + "/" + skillSO.maxLevel.ToString();
            skillIcon.color = Color.white;
        }
        else
        {
            skillButton.interactable = false;
            skillLevelText.text = "Locked";
            skillIcon.color = Color.grey;

        }
    }
    public void TryUpgradeSkill()
    {
        if (isUnlocked && currentLevel < skillSO.maxLevel)
        {
            currentLevel++;

            OnAbilityPointSpent?.Invoke(this);//如果事件非空： ?. 发送事件给订阅者
            if (currentLevel >= skillSO.maxLevel)
            {
                OnMaxSkillLevel?.Invoke(this);
            }
            UpdateUI();
        }
    }
    public void Unlock()
    {
        isUnlocked = true;
        UpdateUI();
    }

    public bool CanUnlockSkill()
    {
        foreach (SkillSlot slot in preRiquriedForSkillUnlock_List)
        {
            if (!slot.isUnlocked || slot.currentLevel < slot.skillSO.maxLevel)
            {
                return false;
            }
        }
        return true;
    }
    //void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.yellow;

    //    foreach (var next in preRiquriedForSkillUnlock_List)
    //    {
    //        if (next != null)
    //        {
    //            Gizmos.DrawLine(
    //                transform.position,
    //                next.transform.position
    //            );
    //        }
    //    }
    //}
}
