using System.Collections.Generic;
using Contracts;
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
    /// 界面由 <see cref="ShopCanvasView"/> 读取 <see cref="State"/> 刷新。
    /// </summary>
    /// <remarks>
    /// 管理器不持有商品槽位或画布引用：买卖只发事件，显隐只请求界面缓存，
    /// 这样商店界面被缓存销毁后重新打开仍能恢复到当前分类。
    /// </remarks>
    public class ShopManager : YSingleton<ShopManager>, IShopInteractable
    {
        /// <summary>商店运行状态：可变化的数值与各自的脏标记放在同一个结构体字段里。</summary>
        public sealed class State
        {
            /// <summary>当前进入范围的店主。</summary>
            public DirtyValue<ShopKeeper> ActiveKeeper;

            /// <summary>商店界面是否打开。</summary>
            public DirtyValue<bool> IsOpen;

            /// <summary>当前分类要显示的商品列表。</summary>
            public DirtyValue<IReadOnlyList<ShopItems>> CurrentItems;

            public State()
            {
                CurrentItems = new DirtyValue<IReadOnlyList<ShopItems>>(
                    System.Array.Empty<ShopItems>(), true);
            }

            /// <summary>任一字段有变化时为 true。</summary>
            public bool AnyDirty =>
                ActiveKeeper.IsDirty || IsOpen.IsDirty || CurrentItems.IsDirty;

            /// <summary>把全部字段标记为待刷新。</summary>
            public void MarkAllDirty()
            {
                ActiveKeeper.MarkDirty();
                IsOpen.MarkDirty();
                CurrentItems.MarkDirty();
            }

            /// <summary>视图读完后清除全部标记。</summary>
            public void ClearDirty()
            {
                ActiveKeeper.ClearMark();
                IsOpen.ClearMark();
                CurrentItems.ClearMark();
            }
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

        /// <summary>店主用于展示立绘的相机目标；没有店主时为空。</summary>
        public Transform CurrentPortraitTarget
        {
            get
            {
                ShopKeeper keeper = state.ActiveKeeper.Value;
                return keeper != null ? keeper.PortraitTarget : null;
            }
        }

        public bool IsShopOpen => state.IsOpen.Value;

        /// <summary>附近有店主时才允许打开商店界面。</summary>
        public bool CanOpenFromView => state.ActiveKeeper.Value != null;

        /// <summary>当前分类的商品列表；没有配置时返回空列表。</summary>
        public IReadOnlyList<ShopItems> CurrentShopItems => state.CurrentItems.Value;

        /// <summary>商店的三个商品分类。</summary>
        private enum ShopCategory
        {
            Items,
            Weapons,
            Armor
        }

        private void OnEnable()
        {
            if (shopKeeperEvent != null)
            {
                shopKeeperEvent.ShopKeeperEntered += OnKeeperEntered;
                shopKeeperEvent.ShopKeeperExited += OnKeeperExited;
            }
            if (sceneLoadedEvent != null)
                sceneLoadedEvent.SceneLoadedEvent += OnSceneLoaded;
        }

        private void OnDisable()
        {
            if (shopKeeperEvent != null)
            {
                shopKeeperEvent.ShopKeeperEntered -= OnKeeperEntered;
                shopKeeperEvent.ShopKeeperExited -= OnKeeperExited;
            }
            if (sceneLoadedEvent != null)
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
            state.ActiveKeeper.Value = keeper;
            if (state.IsOpen.Value)
            {
                LoadKeeperItems();
                state.MarkAllDirty();
            }
        }

        /// <summary>店主离开范围：清空当前店主，并关闭已经打开的商店。</summary>
        private void OnKeeperExited(ShopKeeper keeper)
        {
            if (state.ActiveKeeper.Value != keeper) return;
            state.ActiveKeeper.Value = null;
            if (state.IsOpen.Value)
                CloseShop();
            state.MarkAllDirty();
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
            UICacheManager.Instance?.OpenCanvas(CanvasToToggle.Shop);
        }

        /// <summary>
        /// 关闭商店。已经打开时优先走 UIManager 的关闭流程，保证焦点栈和排序一起复位；
        /// 界面没开或 UIManager 不存在时直接请求缓存关闭。
        /// </summary>
        public void CloseShop()
        {
            if (state.IsOpen.Value)
            {
                if (UIManager.Instance != null)
                    UIManager.Instance.RequestCanvasClose(CanvasToToggle.Shop);
                else
                    UICacheManager.Instance?.CloseCanvas(CanvasToToggle.Shop);
            }
            else
                UICacheManager.Instance?.CloseCanvas(CanvasToToggle.Shop);
        }

        /// <summary>缓存界面打开后回调：重新读取店主商品并回到物品分类。</summary>
        public void OnCachedViewOpened()
        {
            LoadKeeperItems();
            currentCategory = ShopCategory.Items;
            PublishCurrentItems();
            state.IsOpen.Value = true;
        }

        /// <summary>缓存界面关闭后回调：清除打开标记。</summary>
        public void OnCachedViewClosed()
        {
            state.IsOpen.Value = false;
        }

        /// <summary>从当前店主复制三个分类的商品列表；没有店主时保持原数据。</summary>
        private void LoadKeeperItems()
        {
            ShopKeeper keeper = state.ActiveKeeper.Value;
            if (keeper == null) return;
            shopItems = keeper.ShopItems;
            shopWeapon = keeper.ShopWeapon;
            shopArmor = keeper.ShopArmor;
            PublishCurrentItems();
        }

        /// <summary>把当前分类的商品列表写进状态，并标记视图刷新。</summary>
        private void PublishCurrentItems()
        {
            state.CurrentItems.Value = GetItems(currentCategory);
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
        public void MarkDirty()
        {
            state.MarkAllDirty();
        }

        /// <summary>购买一个商品：向背包系统发正数量请求，金币检查在背包侧完成。</summary>
        public void TryBuyItem(ItemSO item, int price)
        {
            if (item == null || InventoryUpdateRequest == null) return;
            InventoryUpdateRequest.RaiseInventoryUpdateRequest(item, price, 1);
        }

        /// <summary>出售一个商品：在当前分类里按物品匹配报价，再发负数量请求。</summary>
        public void SellItem(ItemSO item)
        {
            if (item == null || InventoryUpdateRequest == null) return;
            foreach (var slot in CurrentShopItems)
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
    }

    /// <summary>商店里的一条商品配置：物品与售价。</summary>
    [System.Serializable]
    public class ShopItems
    {
        public ItemSO item;
        public int price;
    }
}
