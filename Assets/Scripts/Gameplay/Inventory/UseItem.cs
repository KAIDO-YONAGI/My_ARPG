using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gameplay.Player.Services;

public class UseItem : MonoBehaviour
{
    public void ApplyItemEffects(ItemSO item)
    {

        if (item.maxHealth > 0)
            StatsService.Instance.UpdateMaxHealth(item.maxHealth);
        if (item.currentHealth > 0)
            StatsService.Instance.UpdateHealth(item.currentHealth);
        if (item.speed > 0)
            StatsService.Instance.UpdateSpeed(item.speed);
        if (item.damage > 0)
            StatsService.Instance.UpdateDamage(item.damage);
        if (item.duration > 0)
            StartCoroutine(EffectTimer(item, item.duration));

    }

    private IEnumerator EffectTimer(ItemSO item, float duration)//用以计时，一段时间之后还原
    {
        yield return new WaitForSeconds(duration);

        if (item.maxHealth > 0)
            StatsService.Instance.UpdateMaxHealth(-item.maxHealth);

        int healthDiff = StatsService.Instance.Stats.CurrentHealth - StatsService.Instance.Stats.MaxHealth;
        if (healthDiff > 0)//如果更新前当前生命大于更新后（已经减小回去）最大生命，那就会减去healthDiff
            StatsService.Instance.UpdateHealth(healthDiff);

        if (item.speed > 0)
            StatsService.Instance.UpdateSpeed(-item.speed);

        if (item.damage > 0)
            StatsService.Instance.UpdateDamage(-item.damage);

    }
}
