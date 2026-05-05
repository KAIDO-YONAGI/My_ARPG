using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    public ItemSO itemSO;
    public int quantity;
    public Image itemImage;
    public TMP_Text quantityText;
    private void OnEnable() {
        UpdateUI();
    }
    private void OnValidate() {
        UpdateUI();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (quantity > 0)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (ShopManager.instance.IsShopOpen)//如果检测到商店活跃，那就出售物品
                {
                    InventoryManager.instance.SetSlotBeenClicked(this);
                    ShopManager.instance.SellItem(itemSO);
                }
                else
                {
                    InventoryManager.instance.UseItem(this);
                }

            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                InventoryManager.instance.DropByClick(this);
            }
        }
    }

    public void UpdateUI()
    {
        if (quantity <= 0)//把脚本化对象置空，让槽位被清空的逻辑正常运行
        {
            itemSO = null;
        }

        if (itemSO != null)
        {
            itemImage.sprite = itemSO.icon;
            itemImage.gameObject.SetActive(true);
            quantityText.text = quantity.ToString();
        }
        else
        {
            itemImage.gameObject.SetActive(false);
            quantityText.text = "";

        }
    }
}
