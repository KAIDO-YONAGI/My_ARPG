using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public PlayerCombat combat;
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
                StatsManager.instance.UpdateHealth(1);
                break;
            case "SwordSlash":
                combat.enabled = true;
                break;

            default:
                Debug.Log($"The skill {skillName} has lost");
                break;
    
        }
    }
}
