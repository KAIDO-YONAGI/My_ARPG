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

        // 物品累计数量记录，按物品配置索引。
        private Dictionary<ItemSO, int> itemHasPicked = new();

        /// <summary>记录一次物品增量并通知监听方。</summary>
        /// <param name="item">物品配置。</param>
        /// <param name="quantity">本次增量，消耗与丢弃传负数。</param>
        public void RecordItem(ItemSO item, int quantity)
        {
            if (itemHasPicked.ContainsKey(item))
            {
                itemHasPicked[item] += quantity;
            }
            else itemHasPicked.Add(item, quantity);

            ItemQuantityChanged?.Invoke(item);
        }

        /// <summary>判断物品累计数量是否达到指定阈值。</summary>
        /// <param name="item">物品配置。</param>
        /// <param name="amount">判定阈值。</param>
        public bool HasPickedOverAmount(ItemSO item, int amount)//实际上会记录是否捡过和数量，当前数量归零的并不会删除
        {
            if (itemHasPicked.ContainsKey(item) && itemHasPicked[item] >= amount)
                return true;

            return false;
        }
        /// <summary>读取物品累计数量；没有记录时返回零。</summary>
        /// <param name="item">物品配置。</param>
        public int GetItemQuantity(ItemSO item)
        {
            if (itemHasPicked.TryGetValue(item, out int amount))
                return amount;
            return 0;
        }
    }
}
