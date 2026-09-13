using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopKeeperEventSO", menuName = "Events/ShopKeeperEventSO", order = 0)]
public class ShopKeeperEventSO : ScriptableObject
{
    /// <summary>玩家进入商店 keeper 的触发范围。</summary>
    public event Action<ShopKeeper> ShopKeeperEntered;
    /// <summary>玩家离开范围，或 keeper 失活/销毁。</summary>
    public event Action<ShopKeeper> ShopKeeperExited;

    public void RaiseShopKeeperEntered(ShopKeeper keeper)
    {
        ShopKeeperEntered?.Invoke(keeper);
    }

    public void RaiseShopKeeperExited(ShopKeeper keeper)
    {
        ShopKeeperExited?.Invoke(keeper);
    }
}
