using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gameplay.Player.Services;

public class SkillManager : MonoBehaviour
{
    [SerializeField] private PlayerCombat combat;
    private void OnEnable()
    {
        SkillSlot.OnAbilityPointSpent += HandleAbilityPointSpent;
    }
    private void OnDisable()
    {
        SkillSlot.OnAbilityPointSpent -= HandleAbilityPointSpent;
    }
    private void HandleAbilityPointSpent(SkillSlot skillSlot)
    {
        string skillName = skillSlot.skillSO.skillName;

        switch (skillName)
        {
            case "MaxHealthBoost":
                StatsService.Instance.Model.UpdateMaxHealth(1);
                StatsService.Instance.Model.UpdateHealth(1);
                break;
            case "SwordSlash":
                combat.SetActive(true);
                break;
    
        }
    }
}
