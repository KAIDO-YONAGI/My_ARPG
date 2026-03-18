using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
public class ShopManager : MonoBehaviour
{
    [SerializeField] private List<ShopItems> shopItems;
    [SerializeField] private ShopSlot[] shopSlots;
    [SerializeField] private InventoryManager inventoryManager;

    public static event Action<ShopManager, bool> OnShopStateChanged;
    private bool shopIsOpen = false;


    private void Start()
    {
        // 确保商店初始状态为关闭
        CloseShop();  // 或者 InitializeShop();

        PopulateShopItems();  // 填充商品
    }

    private void Update()
    {
        if (Input.GetButtonDown("Interact") && shopIsOpen)
        {
            ToggleShop();
        }
    }

    private void ToggleShop()
    {
        if (shopIsOpen)
            CloseShop();
        else
            OpenShop();
    }

    public void OpenShop()
    {
        shopIsOpen = true;

        OnShopStateChanged?.Invoke(this, true);
    }

    public void CloseShop()
    {
        shopIsOpen = false;
        OnShopStateChanged?.Invoke(this, false);
    }
    public void PopulateShopItems()
    {
        for (int i = 0; i < shopItems.Count && i < shopSlots.Length; i++)
        {
            ShopItems shopItem = shopItems[i];
            shopSlots[i].Initialize(shopItem.item, shopItem.price);
            shopSlots[i].gameObject.SetActive(true);
        }
        for (int i = shopItems.Count; i < shopSlots.Length; i++)//置空剩余商店槽位
        {
            shopSlots[i].gameObject.SetActive(false);

        }
    }

    public void TryBuyItem(ItemSO item, int price)
    {
        if (item == null || inventoryManager.goldAmount < price) return;
        else
        {
            if (HasSpaceForItem(item))
            {
                inventoryManager.UpdateGold(price);
                inventoryManager.AddItem(item, 1);
            }
        }
    }

    private bool HasSpaceForItem(ItemSO item)
    {
        foreach (var slot in inventoryManager.itemSlots)
        {
            if ((slot.itemSO == item && slot.quantity < item.stackableSize)
                || slot.itemSO == null) return true;
        }
        return false;
    }

    public void SellItem(ItemSO item)
    {
        if (item == null) return;
        foreach (var slot in shopSlots)//找到想卖出的物品
        {
            if (slot.item == item)
            {
                inventoryManager.UpdateGold(-slot.price);//负值，是出售
                return;
            }
        }
    }
}
[System.Serializable]
public class ShopItems
{
    public ItemSO item;
    public int price;
}
