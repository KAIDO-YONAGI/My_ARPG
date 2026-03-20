using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopToggles : MonoBehaviour
{
public void OpenItemShop()//打开商店界面，调用商店管理器的PopulateShopItems方法来填充商店物品
    {
        if (ShopKeeper.currentShopKeeper != null)
        {
            ShopKeeper.currentShopKeeper.OpenItemShop();
        }
    }public void OpenWeaponShop()//打开商店界面，调用商店管理器的PopulateShopItems方法来填充商店物品
    {
        if (ShopKeeper.currentShopKeeper != null)
        {
            ShopKeeper.currentShopKeeper.OpenWeaponShop();
        }
    }public void OpenArmourShop()//打开商店界面，调用商店管理器的PopulateShopItems方法来填充商店物品
    {
        if (ShopKeeper.currentShopKeeper != null)
        {
            ShopKeeper.currentShopKeeper.OpenArmourShop();
        }
    }
}
