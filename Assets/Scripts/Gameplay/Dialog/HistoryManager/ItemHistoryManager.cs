using System;
using System.Collections.Generic;
using Contracts.Base;
using Pipeline.SO;

namespace Gameplay.Dialog.HistoryManager
{
    public class ItemHistoryManager : YSingleton<ItemHistoryManager>
    {
        /// <summary>
        /// 物品累计数量变化时触发，携带发生变化的物品。
        /// 记录口径是累计数量，消耗与丢弃以负数累加，监听方按当前累计值重算。
        /// </summary>
        public event Action<ItemSO> ItemQuantityChanged;

        private Dictionary<ItemSO, int> itemHasPicked = new();

        public void RecordItem(ItemSO item, int quantity)
        {
            if (itemHasPicked.ContainsKey(item))
            {
                itemHasPicked[item] += quantity;
            }
            else itemHasPicked.Add(item, quantity);

            ItemQuantityChanged?.Invoke(item);
        }

        public bool HasPickedOverAmount(ItemSO item, int amount)//实际上会记录是否捡过和数量，当前数量归零的并不会删除
        {
            if (itemHasPicked.ContainsKey(item) && itemHasPicked[item] >= amount)
                return true;

            return false;
        }
        public int GetItemQuantity(ItemSO item)
        {
            if (itemHasPicked.TryGetValue(item, out int amount))
                return amount;
            return 0;
        }
    }
}
