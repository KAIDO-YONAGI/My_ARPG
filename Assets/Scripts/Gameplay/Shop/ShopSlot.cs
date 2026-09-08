using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class ShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    //ui部分在unity中链接到队对应子对象

    //从SO中初始化
    private ItemSO item;
    private int price;

    [SerializeField] private ShopInfo shopInfo;
    [Header("Link To Parent Shop")]
    [Tooltip("拖入父级商店面板上的 ShopManager 组件（接口无法序列化，用 Component 承载）")]
    [SerializeField] private Component shopRef;

    private IShopInteractable shop;

    [Header("Link To Child Objections")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image itemImage;

    private void Awake()
    {
        shop = shopRef as IShopInteractable;
        if (shop == null)
            Debug.LogError("ShopSlot: shopRef 未接线或未实现 IShopInteractable。", this);
    }
    public ItemSO GetItemSO()
    {
        return item;
    }
    public int GetPrice()
    {
        return price;
    }
    public void Initialize(ItemSO item, int price)
    {
        itemNameText.text = item.itemName;
        this.item = item;
        itemImage.sprite = item.icon;
        this.price = price;
        priceText.text = price.ToString();
    }

    public void OnBuyButtonClick()//unity按钮组件事件
    {
        if (shop != null)
            shop.TryBuyItem(item, price);
        else
            Debug.LogWarning("ShopSlot: 商店引用缺失，无法购买。", this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item == null) return;
        shopInfo.ShowItemInfo(item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        shopInfo.HideItemInfo();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (item == null) return;
        shopInfo.FollowMouse();
    }
}
