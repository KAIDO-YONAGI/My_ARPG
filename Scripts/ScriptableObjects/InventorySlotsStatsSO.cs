using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "InventorySlotsStatsSO", menuName = "InventorySlotsStatsSO", order = 0)]
public class InventorySlotsStatsSO : ScriptableObject
{
    //均为双向
    public UnityAction<ItemSO, int, int> InventoryUpdateRequestEvent;
    public UnityAction<bool> InventoryRespondEvent;
    /// <summary>
    /// 购买/出售的物品，价格，数量
    /// </summary>
    public void RaiseInventoryUpdateRequest(ItemSO item, int price, int amount)//用于购买时请求物品栏变更和出售时发送物品信息并校对
    {
        InventoryUpdateRequestEvent?.Invoke(item, price, amount);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="haveItem">有无物品</param>
    public void RaiseInventoryRespondEvent(bool haveItem)//物品栏请求出售后商店查询并且返回是否有该物品，bool值会决定物品栏管理器的行为，也可用于物品栏是否已经更新
    {
        InventoryRespondEvent?.Invoke(haveItem);
    }

}
