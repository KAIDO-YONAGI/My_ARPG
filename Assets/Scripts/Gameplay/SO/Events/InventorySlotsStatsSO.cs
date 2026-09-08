using System;
using UnityEngine;

[CreateAssetMenu(fileName = "InventorySlotsStatsSO", menuName = "Events/InventorySlotsStatsSO", order = 0)]
public class InventorySlotsStatsSO : ScriptableObject
{
    //均为双向
    public event Action<ItemSO, int, int> InventoryUpdateRequestEvent;
    /// <summary>
    /// 请求更新物品
    /// </summary>
    /// <param name="item">请求的物品</param>
    /// <param name="price">价格</param>
    /// <param name="amount">数量</param>
    /// 用于购买时请求物品栏变更和出售时发送物品信息并校对
    public void RaiseInventoryUpdateRequest(ItemSO item, int price, int amount)
    {
        InventoryUpdateRequestEvent?.Invoke(item, price, amount);
    }

}
