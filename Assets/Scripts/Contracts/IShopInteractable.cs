using UnityEngine;

/// <summary>
/// 商店面板对外交互接口。
/// 面板内部控件（ShopSlot/ShopToggles 等）经 GetComponentInParent 获取，不再依赖 ShopManager 单例；
/// 面板外部系统（ShopKeeper/相机）经 ShopKeeperEventSO 事件交互。
/// 例外：InventorySlot 暂留单例访问。
/// </summary>
public interface IShopInteractable
{
    bool IsShopOpen { get; }
    Transform CurrentPortraitTarget { get; }

    void TryBuyItem(ItemSO item, int price);
    void SellItem(ItemSO item);

    void OpenItemShop();
    void OpenWeaponShop();
    void OpenArmorShop();
}
