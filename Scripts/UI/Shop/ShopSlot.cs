using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class ShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    //ui部分在unity中链接到队对应子对象


    private ItemSO item;
    private int price;

    [SerializeField] private ShopManager shopManager;
    [SerializeField] private ShopInfo shopInfo;

    [Header("Link To Child Objections")]
    public TMP_Text itemNameText;
    public TMP_Text priceText;
    public Image itemImage;
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
        shopManager.TryBuyItem(item, price);
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
