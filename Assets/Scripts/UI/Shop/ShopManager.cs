using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class ShopManager : MonoBehaviour
{
    [SerializeField] private ShopSlot[] shopSlots;
    [SerializeField] private CanvasGroup shopCanvasGroup;


    [Header("Events To Trigger")]
    public InventorySlotsStatsSO InventoryUpdateRequest;
    [Header("Events To Receive")]

    public ShopLoadEventSO shopLoadEvent;
    public ToggleCanvasEventSO toggleShopCanvasEvent;


    private List<ShopItems> shopItems;
    private List<ShopItems> shopWeapon;
    private List<ShopItems> shopArmor;

    public static ShopManager instance;


    private bool isShopOpen = false;
    public bool IsShopOpen => isShopOpen;//物品栏会根据这个bool变量选择是出售还是使用物品
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else Destroy(gameObject);
    }
    private void OnEnable()
    {
        shopLoadEvent.ShopLoadEvent += OnShopLoad;
        toggleShopCanvasEvent.toggleCanvasEvent += OnShopToggle;

    }
    private void OnDisable()
    {
        shopLoadEvent.ShopLoadEvent -= OnShopLoad;
        toggleShopCanvasEvent.toggleCanvasEvent -= OnShopToggle;

    }
    private void OnShopToggle(bool state)
    {
        if (!state)
        {
            TimeManager.instance.ResumeGame();
            shopCanvasGroup.alpha = 0;
            shopCanvasGroup.interactable = false;
            shopCanvasGroup.blocksRaycasts = false;
            isShopOpen=false;
        }
    }
    private void OnShopLoad(List<ShopItems> shopItems, List<ShopItems> shopWeapon, List<ShopItems> shopArmor)
    {
        this.shopItems = shopItems;
        this.shopWeapon = shopWeapon;
        this.shopArmor = shopArmor;
        OpenItemShop();
        TimeManager.instance.PauseGame();
        shopCanvasGroup.alpha = 1;
        shopCanvasGroup.interactable = true;
        shopCanvasGroup.blocksRaycasts = true;
        isShopOpen=true;
    }

    private void PopulateShopItems(List<ShopItems> shopItems)
    {
        if (shopItems == null) return;
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
        InventoryUpdateRequest.RaiseInventoryUpdateRequest(item, price, 1);
    }
    public void SellItem(ItemSO item)
    {
        if (item == null) return;
        foreach (var slot in shopSlots)//找到想卖出的物品
        {
            if (slot.GetItemSO() == item)
            {
                //价格和数目均设置为负，出售
                InventoryUpdateRequest.RaiseInventoryUpdateRequest(item, -slot.GetPrice(), -1);
                return;
            }
        }
    }

    public void OpenItemShop()
    {
        PopulateShopItems(shopItems);
    }

    public void OpenWeaponShop()
    {
        PopulateShopItems(shopWeapon);
    }

    public void OpenArmorShop()
    {
        PopulateShopItems(shopArmor);
    }
}
[System.Serializable]
public class ShopItems
{
    public ItemSO item;
    public int price;
}
