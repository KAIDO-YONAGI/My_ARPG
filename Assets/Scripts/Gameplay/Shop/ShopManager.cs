using System.Collections.Generic;
using Contracts.Base;
using Contracts.Enums;
using Contracts.Interfaces;
using Contracts.State;
using Gameplay.Units.ShopKeeper;
using Pipeline.SO;
using Pipeline.SO.Events;
using Pipeline.UI.Runtime;
using Pipeline.UI.SystemCanvasManagers;
using UnityEngine;

namespace Gameplay.Shop
{
    /// <summary>
    /// 商店的持久状态管理器。保存当前店主、三个分类的商品列表和当前分类，
    /// 界面由 <see cref="ShopCanvasView"/> 读取商品状态刷新。
    /// </summary>
    /// <remarks>
    /// 管理器不持有商品槽位或画布引用：买卖只发事件，显隐只请求界面缓存，
    /// 这样商店界面被缓存销毁后重新打开仍能恢复到当前分类。
    /// </remarks>
    public class ShopManager : YSingleton<ShopManager>
    {
        public sealed class State
        {
            /// <summary>当前进入范围的店主。</summary>
            private ShopKeeper activeKeeper;

            /// <summary>商店界面是否打开。</summary>
            private bool isOpen;

            /// <summary>当前分类的商品列表及其脏标记。</summary>
            private DirtyValue<IReadOnlyList<ShopItems>> currentItems;

            /// <summary>当前进入范围的店主。</summary>
            public ShopKeeper ActiveKeeper => activeKeeper;

            /// <summary>商店界面是否打开。</summary>
            public bool IsOpen => isOpen;

            /// <summary>商品列表是否需要刷新。</summary>
            public bool IsItemsDirty => currentItems.IsDirty;

            /// <summary>当前分类的商品数量。</summary>
            public int CurrentItemCount => currentItems.Value.Count;

            /// <summary>按下标读取商品物品；越界返回 null。</summary>
            public ItemSO GetItem(int index) =>
                index >= 0 && index < CurrentItemCount ? currentItems.Value[index]?.item : null;

            /// <summary>按下标读取商品价格；越界返回 0。</summary>
            public int GetPrice(int index) =>
                index >= 0 && index < CurrentItemCount ? currentItems.Value[index]?.price ?? 0 : 0;

            public State()
            {
                currentItems = new DirtyValue<IReadOnlyList<ShopItems>>(
                    System.Array.Empty<ShopItems>(), true);
            }

            /// <summary>值未变但需要重画商品时单独置脏。</summary>
            public void MarkItemsDirty() => currentItems.MarkDirty();

            /// <summary>视图刷新完成后清除商品脏标记。</summary>
            public void ClearItemsDirty() => currentItems.ClearMark();

            /// <summary>写入当前店主。</summary>
            internal void SetActiveKeeper(ShopKeeper keeper) => activeKeeper = keeper;

            /// <summary>写入商店界面开关标记。</summary>
            internal void SetIsOpen(bool value) => isOpen = value;

            /// <summary>写入当前分类的商品列表并置脏。</summary>
            internal void SetCurrentItems(IReadOnlyList<ShopItems> items) => currentItems.Set(items);

            /// <summary>当前分类的商品列表。</summary>
            internal IReadOnlyList<ShopItems> Items => currentItems.Value;
        }

        [Header("触发事件")]
        [Tooltip("买卖请求：正数量购买，负数量出售。")]
        [SerializeField] private InventorySlotsStatsSO InventoryUpdateRequest;

        [Header("接收事件")]
        [Tooltip("店主进入/离开交互范围。")]
        [SerializeField] private ShopKeeperEventSO shopKeeperEvent;

        [Tooltip("场景加载完成后关闭商店。")]
        [SerializeField] private SceneLoadedEventSO sceneLoadedEvent;

        private List<ShopItems> shopItems;
        private List<ShopItems> shopWeapon;
        private List<ShopItems> shopArmor;
        private ShopCategory currentCategory = ShopCategory.Items;
        private readonly State state = new();

        /// <summary>商店运行状态，供视图读取。</summary>
        public State CurrentState => state;

        /// <summary>商店界面是否打开。</summary>
        public bool IsShopOpen => state.IsOpen;

        /// <summary>附近有店主时才允许打开商店界面。</summary>
        public bool CanOpenFromView => state.ActiveKeeper != null;

        /// <summary>商店的三个商品分类。</summary>
        private enum ShopCategory
        {
            Items,
            Weapons,
            Armor
        }

        protected override void OnSingletonInitialized()
        {
            Debug.Assert(shopKeeperEvent != null, nameof(shopKeeperEvent), this);
            Debug.Assert(sceneLoadedEvent != null, nameof(sceneLoadedEvent), this);
            Debug.Assert(InventoryUpdateRequest != null, nameof(InventoryUpdateRequest), this);
        }

        private void OnEnable()
        {
            shopKeeperEvent.ShopKeeperEntered += OnKeeperEntered;
            shopKeeperEvent.ShopKeeperExited += OnKeeperExited;
            sceneLoadedEvent.SceneLoadedEvent += OnSceneLoaded;
        }

        private void OnDisable()
        {
            shopKeeperEvent.ShopKeeperEntered -= OnKeeperEntered;
            shopKeeperEvent.ShopKeeperExited -= OnKeeperExited;
            sceneLoadedEvent.SceneLoadedEvent -= OnSceneLoaded;
        }

        /// <summary>切换场景时关闭商店，避免旧场景的店主数据残留。</summary>
        private void OnSceneLoaded(GameSceneSO _)
        {
            CloseShop();
        }

        /// <summary>店主进入范围：记录当前店主；商店若已打开则换用新店主的商品。</summary>
        private void OnKeeperEntered(ShopKeeper keeper)
        {
            state.SetActiveKeeper(keeper);
            if (state.IsOpen)
                LoadKeeperItems();
        }

        /// <summary>店主离开范围：清空当前店主，并关闭已经打开的商店。</summary>
        private void OnKeeperExited(ShopKeeper keeper)
        {
            if (state.ActiveKeeper != keeper) return;
            state.SetActiveKeeper(null);
            if (state.IsOpen)
                CloseShop();
        }

        /// <summary>按店主配置打开商店；界面缓存负责真正的实例创建和显示。</summary>
        public void OpenShop(
            List<ShopItems> items,
            List<ShopItems> weapon,
            List<ShopItems> armor)
        {
            shopItems = items;
            shopWeapon = weapon;
            shopArmor = armor;
            currentCategory = ShopCategory.Items;
            PublishCurrentItems();
            UICacheManager cache = UICacheManager.Instance;
            if (cache == null)
            {
                Debug.LogWarning("商店画布：UI 缓存不可用，无法打开。");
                return;
            }

            cache.OpenCanvas(CanvasToToggle.Shop);
        }

        /// <summary>
        /// 关闭商店。已经打开时优先走 UIManager 的关闭流程，保证焦点栈和排序一起复位；
        /// 界面没开或 UIManager 不存在时直接请求缓存关闭。
        /// </summary>
        public void CloseShop()
        {
            if (state.IsOpen)
            {
                if (UIManager.Instance != null)
                    UIManager.Instance.RequestCanvasClose(CanvasToToggle.Shop);
                else
                    CloseShopCanvas();
            }
            else
                CloseShopCanvas();
        }

        /// <summary>缓存界面打开后回调：重新读取店主商品并回到物品分类。</summary>
        public void OnCachedViewOpened()
        {
            LoadKeeperItems();
            currentCategory = ShopCategory.Items;
            PublishCurrentItems();
            state.SetIsOpen(true);
        }

        /// <summary>缓存界面关闭后回调：清除打开标记。</summary>
        public void OnCachedViewClosed()
        {
            state.SetIsOpen(false);
        }

        /// <summary>从当前店主复制三个分类的商品列表；没有店主时保持原数据。</summary>
        private void LoadKeeperItems()
        {
            ShopKeeper keeper = state.ActiveKeeper;
            if (keeper == null) return;
            shopItems = keeper.ShopItems;
            shopWeapon = keeper.ShopWeapon;
            shopArmor = keeper.ShopArmor;
            PublishCurrentItems();
        }

        /// <summary>把当前分类的商品列表写进状态，并标记视图刷新。</summary>
        private void PublishCurrentItems()
        {
            state.SetCurrentItems(GetItems(currentCategory));
        }

        /// <summary>取指定分类的商品列表；列表未配置时返回空列表。</summary>
        private IReadOnlyList<ShopItems> GetItems(ShopCategory category)
        {
            List<ShopItems> items = category switch
            {
                ShopCategory.Weapons => shopWeapon,
                ShopCategory.Armor => shopArmor,
                _ => shopItems
            };
            return items ?? (IReadOnlyList<ShopItems>)System.Array.Empty<ShopItems>();
        }

        /// <summary>把全部状态标记为待刷新。</summary>
        /// <summary>购买一个商品：向背包系统发正数量请求，金币检查在背包侧完成。</summary>
        public void TryBuyItem(ItemSO item, int price)
        {
            if (item == null) return;
            InventoryUpdateRequest.RaiseInventoryUpdateRequest(item, price, 1);
        }

        /// <summary>出售一个商品：在当前分类里按物品匹配报价，再发负数量请求。</summary>
        public void SellItem(ItemSO item)
        {
            if (item == null) return;
            foreach (ShopItems slot in state.Items)
            {
                if (slot != null && slot.item == item)
                {
                    // 使用负数价格和数量表示出售：背包侧会加金币并移除物品。
                    InventoryUpdateRequest.RaiseInventoryUpdateRequest(item, -slot.price, -1);
                    return;
                }
            }
        }

        /// <summary>切换到物品分类并刷新界面。</summary>
        public void OpenItemShop()
        {
            currentCategory = ShopCategory.Items;
            PublishCurrentItems();
        }

        /// <summary>切换到武器分类并刷新界面。</summary>
        public void OpenWeaponShop()
        {
            currentCategory = ShopCategory.Weapons;
            PublishCurrentItems();
        }

        /// <summary>切换到护甲分类并刷新界面。</summary>
        public void OpenArmorShop()
        {
            currentCategory = ShopCategory.Armor;
            PublishCurrentItems();
        }

        /// <summary>请求界面缓存关闭商店画布；缓存缺失时只记录警告。</summary>
        private static void CloseShopCanvas()
        {
            UICacheManager cache = UICacheManager.Instance;
            if (cache == null)
            {
                Debug.LogWarning("商店画布：UI 缓存不可用，无法通过界面关闭。");
                return;
            }

            cache.CloseCanvas(CanvasToToggle.Shop);
        }
    }

    /// <summary>商店里的一条商品配置：物品与售价。</summary>
    [System.Serializable]
    public class ShopItems
    {
        public ItemSO item;
        public int price;
    }
}
