using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "LootEventSO", menuName = "Events/LootEventSO", order = 0)]
public class LootEventSO : ScriptableObject
{
    public UnityAction<ItemSO, int, Loot> LootEvent;

    public void OnEventRaised(ItemSO item, int quantity, Loot loot)
    {
        LootEvent?.Invoke(item, quantity, loot);
    }
}
