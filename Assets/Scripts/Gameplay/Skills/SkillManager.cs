using Gameplay.Player;
using Gameplay.Player.Services;
using UnityEngine;

namespace Gameplay.Skills
{
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
                    StatsService.Instance.UpdateMaxHealth(1);
                    StatsService.Instance.UpdateHealth(1);
                    break;
                case "SwordSlash":
                    combat.SetActive(true);
                    break;
    
            }
        }
    }
}
