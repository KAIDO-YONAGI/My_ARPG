using Pipeline.SO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Gameplay.Shop
{
    /// <summary>
    /// 商店里的单个商品槽位：显示图标、名称、价格，并把点击转成购买请求。
    /// 鼠标悬停时通知 <see cref="ShopInfoPanel"/> 显示物品详情。
    /// </summary>
    /// <remarks>
    /// 商品数据由 <see cref="ShopManager"/> 在显示时写入，槽位本身不保存商店配置；
    /// 这样槽位被隐藏、复用或界面被缓存销毁都不会改变商品价格。
    /// </remarks>
    public class ShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        // 从管理器写入的商品数据；空槽位时 item 为 null。
        private ItemSO item;
        private int price;

        [Header("引用")]
        [FormerlySerializedAs("shopInfo")]
        [SerializeField] private ShopInfoPanel shopInfoPanel;

        [Header("Link To Child Objections")]
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Image itemImage;

        private void Awake()
        {
            Debug.Assert(shopInfoPanel != null, nameof(shopInfoPanel), this);
            Debug.Assert(itemNameText != null, nameof(itemNameText), this);
            Debug.Assert(priceText != null, nameof(priceText), this);
            Debug.Assert(itemImage != null, nameof(itemImage), this);
        }

        /// <summary>当前槽位的物品；供旧的出售流程查询。</summary>
        public ItemSO GetItemSO()
        {
            return item;
        }

        /// <summary>当前槽位的售价；供旧的出售流程查询。</summary>
        public int GetPrice()
        {
            return price;
        }

        /// <summary>用商品配置刷新槽位显示。</summary>
        public void Initialize(ItemSO item, int price)
        {
            this.item = item;
            this.price = price;
            itemNameText.text = item != null ? item.itemName : string.Empty;
            itemImage.sprite = item != null ? item.icon : null;
            priceText.text = price.ToString();
        }

        /// <summary>购买按钮回调；实际扣款与入包由管理器处理。</summary>
        public void OnBuyButtonClick()
        {
            ShopManager.RequiredInstance.TryBuyItem(item, price);
        }

        /// <summary>鼠标进入：有商品时显示详情。</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (item == null) return;
            shopInfoPanel.ShowItemInfo(item);
        }

        /// <summary>鼠标离开：隐藏详情。</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            shopInfoPanel.HideItemInfo();
        }

        /// <summary>鼠标移动：让详情面板跟随指针。</summary>
        public void OnPointerMove(PointerEventData eventData)
        {
            if (item == null) return;
            shopInfoPanel.FollowMouse();
        }
    }
}
