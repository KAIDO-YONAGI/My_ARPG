using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[CreateAssetMenu(fileName = "ShopLoadEventSO", menuName = "Events/ShopLoadEventSO", order = 0)]

public class ShopLoadEventSO : ScriptableObject
{
    public UnityAction<List<ShopItems>, List<ShopItems>, List<ShopItems>> ShopLoadEvent;
    public void RaiseShopLoadRequest(List<ShopItems> shopItems, List<ShopItems> shopWeapon, List<ShopItems> shopArmour)
    {
        ShopLoadEvent?.Invoke(shopItems, shopWeapon, shopArmour);
    }

}
