using Contracts.Base;
using Pipeline.SO;
using UnityEngine;

namespace Gameplay.Shop
{
    /// <summary>
    /// 商店画布的显示层。由 UI 缓存按需创建，商品槽位全部在 ShopCanvas 预制体里拖引用，
    /// 打开时读取 <see cref="ShopManager"/> 当前分类的商品列表并刷新。
    /// </summary>
    /// <remarks>
    /// 商品价格和物品数据由管理器持有，视图只在显示时写入槽位；
    /// 槽位被隐藏或界面被缓存销毁都不会影响商店数据。
    /// </remarks>
    public sealed class ShopCanvasView : CachedCanvasViewBase
    {
        [Header("商品槽位")]
        [Tooltip("商店里的商品槽位，顺序与当前分类的商品列表一致。")]
        [SerializeField] private ShopSlot[] slots;

        private void Awake()
        {
            Debug.Assert(slots != null, nameof(slots), this);
            for (int i = 0; i < slots.Length; i++)
                Debug.Assert(slots[i] != null, $"{nameof(slots)}[{i}]", this);
        }

        /// <summary>附近没有店主时不允许打开商店。</summary>
        protected override bool CanOpen()
        {
            return ShopManager.Instance != null && ShopManager.Instance.CanOpenFromView;
        }

        /// <summary>打开后通知管理器进入商店状态，并立即刷新商品。</summary>
        protected override void OnOpen()
        {
            ShopManager manager = ShopManager.RequiredInstance;

            manager.OnCachedViewOpened();
            RefreshIfDirty();
        }

        private void Update()
        {
            RefreshIfDirty();
        }

        /// <summary>
        /// 按脏标记刷新商品槽位。没有商品的槽位直接隐藏，
        /// 避免切换分类后残留上一页的商品和点击目标。
        /// </summary>
        private void RefreshIfDirty()
        {
            ShopManager manager = ShopManager.Instance;
            if (!IsOpen || manager == null) return;

            ShopManager.State state = manager.CurrentState;
            if (!state.IsItemsDirty) return;

            int slotCount = slots.Length;
            for (int i = 0; i < slotCount; i++)
            {
                ItemSO item = i < state.CurrentItemCount ? state.GetItem(i) : null;
                bool hasItem = item != null;
                slots[i].gameObject.SetActive(hasItem);
                if (hasItem)
                    slots[i].Initialize(item, state.GetPrice(i));
            }

            state.ClearItemsDirty();
        }

        /// <summary>关闭时通知管理器退出商店状态。</summary>
        protected override void OnClose()
        {
            ShopManager manager = ShopManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("ShopManager 不可用，商店视图无法上报关闭。");
                return;
            }

            manager.OnCachedViewClosed();
        }
    }
}
