using Gameplay.Shop;
using Pipeline.SO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.Inventory
{
    /// <summary>
    /// 单个物品槽位的显示与点击入口。快捷栏和背包共用本组件，
    /// 通过 <see cref="ConfigureSlot"/> 区分自己属于哪一组、排第几位。
    /// </summary>
    /// <remarks>
    /// 物品与数量是管理器状态的显示副本，运行期只由管理器通过
    /// <see cref="RestoreState"/> 写入；槽位本身不决定堆叠、使用或出售规则。
    /// 预制体上的初始物品只在第一次初始化管理器状态时读取。
    /// </remarks>
    public class InventorySlot : MonoBehaviour, IPointerClickHandler
    {
        [Header("初始配置")]
        [Tooltip("预制体上的初始物品；只在首次初始化管理器状态时读取。")]
        [SerializeField] private ItemSO itemSO;

        [Tooltip("预制体上的初始数量；只在首次初始化管理器状态时读取。")]
        [SerializeField] private int quantity;

        [Header("控件")]
        [SerializeField] private Image itemImage;
        [SerializeField] private TMP_Text quantityText;

        private int slotIndex = -1;
        private bool isBackpackSlot;
        private ItemSO initialItem;
        private int initialQuantity;

        /// <summary>当前显示的物品；空槽位为 null。</summary>
        public ItemSO ItemSO => itemSO;

        /// <summary>当前显示的数量。</summary>
        public int Quantity => quantity;

        public bool IsEmpty => itemSO == null;

        /// <summary>预制体上配置的初始物品；必须在第一次 <see cref="RestoreState"/> 前读取。</summary>
        public ItemSO InitialItem => initialItem;

        /// <summary>预制体上配置的初始数量。</summary>
        public int InitialQuantity => initialQuantity;

        /// <summary>槽位在所属列表中的下标；<c>-1</c> 表示尚未配置。</summary>
        public int SlotIndex => slotIndex;

        /// <summary>是否属于背包列表；false 表示快捷栏。</summary>
        public bool IsBackpackSlot => isBackpackSlot;

        private void Awake()
        {
            // 先保存预制体上的初始值，后面的 RestoreState 会覆盖 itemSO/quantity。
            initialItem = itemSO;
            initialQuantity = quantity;
        }

        private void OnEnable()
        {
            UpdateUI();
        }

        private void OnValidate()
        {
            UpdateUI();
        }

        /// <summary>
        /// 点击槽位：左键在商店打开时出售，否则使用；右键丢弃。
        /// 只处理有物品的槽位，空槽位不响应。
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (slotIndex < 0 || quantity <= 0) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (ShopManager.Instance != null && ShopManager.Instance.IsShopOpen)
                {
                    // 商店打开时左键表示出售：先记录槽位，商店再按物品报价扣减。
                    InventoryManager.Instance.SetSlotBeenClicked(this);
                    ShopManager.Instance.SellItem(itemSO);
                }
                else
                {
                    InventoryManager.Instance.UseItem(this);
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                InventoryManager.Instance.DropByClick(this);
            }
        }

        /// <summary>配置槽位归属与下标；由快捷栏/背包视图在初始化时调用。</summary>
        public void ConfigureSlot(bool backpack, int index)
        {
            isBackpackSlot = backpack;
            slotIndex = index;
        }

        /// <summary>用管理器状态覆盖显示；数量为 0 时清空槽位。</summary>
        public void RestoreState(ItemSO item, int amount)
        {
            itemSO = amount > 0 ? item : null;
            quantity = itemSO != null ? amount : 0;
            UpdateUI();
        }

        /// <summary>把当前物品与数量画到图标和文本上；数量为 0 时清空物品引用。</summary>
        public void UpdateUI()
        {
            if (quantity <= 0)
            {
                // 把物品引用置空，让槽位重新参与“空槽位”判定。
                itemSO = null;
            }

            if (itemSO != null)
            {
                if (itemImage != null)
                {
                    itemImage.sprite = itemSO.icon;
                    itemImage.gameObject.SetActive(true);
                }
                if (quantityText != null)
                    quantityText.text = quantity.ToString();
            }
            else
            {
                if (itemImage != null)
                    itemImage.gameObject.SetActive(false);
                if (quantityText != null)
                    quantityText.text = "";
            }
        }
    }
}
