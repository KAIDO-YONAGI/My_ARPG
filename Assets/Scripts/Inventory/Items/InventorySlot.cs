using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private ItemSO itemSO;
    [SerializeField] private int quantity;
    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text quantityText;

    // 只读访问器：外部可读不可写
    public ItemSO ItemSO => itemSO;
    public int Quantity => quantity;
    public bool IsEmpty => itemSO == null;

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

    /// <summary>
    /// 往槽位添加物品。统一处理 itemSO 赋值、数量累加、UI 刷新。
    /// </summary>
    /// <returns>实际放入的数量（受堆叠上限约束）</returns>
    public int AddItem(ItemSO item, int amount)
    {
        if (item == null || amount <= 0) return 0;

        // 可堆叠则不超过 stackableSize；新物品则按 amount 放入
        int maxForThisSlot = itemSO == item ? item.stackableSize - quantity : item.stackableSize;
        int actual = Mathf.Min(maxForThisSlot, amount);
        if (actual <= 0) return 0;

        itemSO = item;
        quantity += actual;
        UpdateUI();
        return actual;
    }

    /// <summary>
    /// 从槽位移除物品。数量归零时自动清空 itemSO，保持"空槽 itemSO 必为 null"不变量。
    /// </summary>
    /// <returns>实际移除的数量</returns>
    public int RemoveItem(int amount)
    {
        if (itemSO == null || amount <= 0) return 0;

        int actual = Mathf.Min(amount, quantity);
        quantity -= actual;
        if (quantity <= 0)
        {
            quantity = 0;
            itemSO = null;
        }
        UpdateUI();
        return actual;
    }

    /// <summary>
    /// 返回本槽位还能放入多少该物品（同物品受堆叠上限约束；空槽则上限为 stackableSize）。
    /// </summary>
    public int SpaceRemaining(ItemSO item)
    {
        if (itemSO == null) return item != null ? item.stackableSize : 0;
        if (itemSO != item) return 0;
        return item.stackableSize - quantity;
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
