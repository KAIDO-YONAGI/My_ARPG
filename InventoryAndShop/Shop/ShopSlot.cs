using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class ShopSlot : MonoBehaviour,IPointerEnterHandler, IPointerExitHandler,IPointerMoveHandler
{
    public ItemSO item;
    public TMP_Text itemNameText;
    public TMP_Text priceText;
    public Image itemImage;
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private ShopInfo shopInfo;

    public int price;
    public void Initialize(ItemSO item,int price)
    {
        itemNameText.text=item.itemName;
        this.item=item;
        itemImage.sprite = item.icon;
        this.price = price;
        priceText.text=price.ToString();
    }

    public void OnBuyButtonClick()
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
