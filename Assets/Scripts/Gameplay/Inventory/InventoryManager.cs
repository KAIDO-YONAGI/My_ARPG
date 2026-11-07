using System.Collections;
using System.Collections.Generic;
using Contracts.Base;
using Contracts.State;
using Gameplay.Dialog.HistoryManager;
using Gameplay.Player.Services;
using Pipeline;
using Pipeline.Scene;
using Pipeline.SO;
using Pipeline.SO.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay.Inventory
{
    /// <summary>
    /// 背包与快捷栏的持久状态管理器。保存槽位内容、金币和选中槽位，
    /// 快捷栏与背包界面读取只读状态刷新。
    /// </summary>
    /// <remarks>
    /// 管理器不持有 UI 控件引用，也不在运行时遍历预制体。
    /// 快捷栏和背包的初始物品由各自视图在首次打开时通过 <c>Initialize*State</c> 传入，
    /// 之后所有增删改都只改这里的状态列表。
    /// </remarks>
    public class InventoryManager : YSingleton<InventoryManager>
    {
        private static readonly int IsPicked = Animator.StringToHash("isPicked");

        /// <summary>一个槽位的运行期状态：物品与数量。</summary>
        [System.Serializable]
        public struct InventorySlotState
        {
            public ItemSO item;
            public int quantity;

            public InventorySlotState(ItemSO item, int quantity)
            {
                this.item = item;
                this.quantity = quantity;
            }
        }

        /// <summary>槽位地址：区分快捷栏/背包，避免两个列表的下标混用。</summary>
        public readonly struct SlotAddress
        {
            public readonly bool isBackpack;
            public readonly int index;

            public SlotAddress(bool isBackpack, int index)
            {
                this.isBackpack = isBackpack;
                this.index = index;
            }
        }

        /// <summary>背包运行状态：金币与两侧槽位列表各自保存版本。</summary>
        public sealed class State
        {
            /// <summary>金币数量及其版本。</summary>
            internal VersionedValue<int> Gold;

            /// <summary>快捷栏槽位及其版本。</summary>
            internal VersionedValue<List<InventorySlotState>> Hotbar;

            /// <summary>背包槽位及其版本。</summary>
            internal VersionedValue<List<InventorySlotState>> Backpack;

            /// <summary>金币数量。</summary>
            public int GoldAmount => Gold.Value;

            /// <summary>金币数据版本。</summary>
            public ulong GoldVersion => Gold.Version;

            /// <summary>快捷栏槽位数量。</summary>
            public int HotbarCount => Hotbar.Value.Count;

            /// <summary>背包槽位数量。</summary>
            public int BackpackCount => Backpack.Value.Count;

            /// <summary>快捷栏数据版本。</summary>
            public ulong HotbarVersion => Hotbar.Version;

            /// <summary>背包数据版本。</summary>
            public ulong BackpackVersion => Backpack.Version;

            /// <summary>读取快捷栏槽位；下标越界时返回空槽。</summary>
            public InventorySlotState GetHotbarSlot(int index) =>
                index >= 0 && index < HotbarCount ? Hotbar.Value[index] : default;

            /// <summary>读取背包槽位；下标越界时返回空槽。</summary>
            public InventorySlotState GetBackpackSlot(int index) =>
                index >= 0 && index < BackpackCount ? Backpack.Value[index] : default;

            /// <summary>快捷栏集合内容修改后推进版本。</summary>
            internal void TouchHotbar() => Hotbar.Touch();

            /// <summary>背包集合内容修改后推进版本。</summary>
            internal void TouchBackpack() => Backpack.Touch();

            public State()
            {
                Gold = new VersionedValue<int>(0);
                Hotbar = new VersionedValue<List<InventorySlotState>>(
                    new List<InventorySlotState>());
                Backpack = new VersionedValue<List<InventorySlotState>>(
                    new List<InventorySlotState>());
            }
        }

        private bool hasHotbarState;
        private bool hasBackpackState;
        private SlotAddress? selectedSlot;
        private bool hasSelectedSlot;
        private readonly State state = new();

        [Header("场景引用")]
        [Tooltip("使用物品时执行效果的组件。")]
        [SerializeField] private UseItem useItem;

        [Tooltip("掉落物预制体；没有现成掉落物时用它实例化。")]
        [SerializeField] private Loot lootPrefab;

        [Tooltip("掉落物生成位置，通常是玩家。")]
        [SerializeField] private Transform player;

        [Header("掉落物对象池")]
        [Tooltip("掉落物对象池预热数量。")]
        [SerializeField] private int lootPrewarm = 4;

        [Tooltip("掉落物对象池上限。")]
        [SerializeField] private int lootPoolMaxSize = 20;
        private ObjectPool<Loot> lootPool;

        [Header("事件")]
        [Tooltip("商店买卖请求。")]
        [SerializeField] private InventorySlotsStatsSO ShoppingRequest;

        [Tooltip("任务奖励请求。")]
        [SerializeField] private InventorySlotsStatsSO QuestRewardRequest;

        [Tooltip("场景中掉落物被拾取时触发。")]
        [SerializeField] private LootEventSO lootEvent;

        /// <summary>背包运行状态，供视图读取。</summary>
        public State CurrentState => state;

        public bool IsHotbarInitialized => hasHotbarState;
        public bool IsBackpackInitialized => hasBackpackState;

        /// <summary>创建掉落物对象池。</summary>
        private void Start()
        {
            lootPool = new ObjectPool<Loot>(lootPrefab, lootPrewarm, transform, lootPoolMaxSize);
        }

        protected override void OnSingletonInitialized()
        {
            Debug.Assert(lootEvent != null, nameof(lootEvent), this);
            Debug.Assert(ShoppingRequest != null, nameof(ShoppingRequest), this);
            Debug.Assert(QuestRewardRequest != null, nameof(QuestRewardRequest), this);
        }

        private void OnEnable()
        {
            lootEvent.LootEvent += OnItemLootedHandler;
            ShoppingRequest.InventoryUpdateRequestEvent += HandleShopping;
            QuestRewardRequest.InventoryUpdateRequestEvent += HandleQuestReward;
        }

        private void OnDisable()
        {
            lootEvent.LootEvent -= OnItemLootedHandler;
            ShoppingRequest.InventoryUpdateRequestEvent -= HandleShopping;
            QuestRewardRequest.InventoryUpdateRequestEvent -= HandleQuestReward;
        }

        /// <summary>
        /// 用快捷栏视图的初始配置建立槽位状态。只在第一次调用时生效，
        /// 之后重复打开界面不会把玩家已经获得的物品重置回初始值。
        /// </summary>
        public void InitializeHotbarState(IReadOnlyList<InventorySlotState> initialSlots)
        {
            if (hasHotbarState) return;

            List<InventorySlotState> slots = state.Hotbar.Value;
            slots.Clear();
            if (initialSlots != null)
                for (int i = 0; i < initialSlots.Count; i++)
                    slots.Add(initialSlots[i]);

            hasHotbarState = true;
            state.Hotbar.Set(slots);
        }

        /// <summary>
        /// 用背包视图的初始配置建立槽位状态。只在第一次调用时生效，
        /// 后续背包界面被缓存销毁再重建也不会覆盖运行期数据。
        /// </summary>
        public void InitializeBackpackState(IReadOnlyList<InventorySlotState> initialSlots)
        {
            if (hasBackpackState) return;

            List<InventorySlotState> slots = state.Backpack.Value;
            slots.Clear();
            if (initialSlots != null)
                for (int i = 0; i < initialSlots.Count; i++)
                    slots.Add(initialSlots[i]);

            hasBackpackState = true;
            state.Backpack.Set(slots);
        }

        /// <summary>读取指定槽位的物品与数量；下标越界时返回 false。</summary>
        public bool TryGetSlotState(bool isBackpack, int index, out ItemSO item, out int quantity)
        {
            List<InventorySlotState> slots = isBackpack ? state.Backpack.Value : state.Hotbar.Value;
            if (index >= 0 && index < slots.Count)
            {
                item = slots[index].item;
                quantity = slots[index].quantity;
                return true;
            }

            item = null;
            quantity = 0;
            return false;
        }

        /// <summary>任务奖励：按奖励数量走普通物品入包流程，不扣金币。</summary>
        private void HandleQuestReward(ItemSO item, int price, int amount)
        {
            UpdateInventorySlots(item, amount);
        }

        /// <summary>场景掉落物被拾取时进入背包；放不下则把剩余数量留在地上。</summary>
        private void OnItemLootedHandler(ItemSO item, int quantity, Loot lootObj)
        {
            UpdateInventorySlots(item, quantity, lootObj);
        }

        /// <summary>
        /// 商店买卖：正数数量表示购买（先检查金币和空位），负数表示出售。
        /// 价格使用正负号约定买方/卖方，具体扣加金币统一走 <see cref="UpdateGold"/>。
        /// </summary>
        private void HandleShopping(ItemSO item, int price, int amount)
        {
            if (item == null || state.Gold.Value < price) return;
            if (amount > 0)
            {
                if (!HasSpaceForItem(item)) return;
                UpdateGold(price);
                UpdateInventorySlots(item, amount);
            }
            else if (amount < 0)
            {
                UpdateGold(price);
                UpdateInventorySlots(item, amount);
            }
        }

        /// <summary>
        /// 背包变更的统一入口：金币和经验直接结算，普通物品按正负数量进入或移出槽位，
        /// 放不下的部分掉到地上。
        /// </summary>
        private void UpdateInventorySlots(ItemSO item, int quantity, Loot lootObj = null)
        {
            if (item == null) return;

            if (item.isGold)
            {
                state.Gold.Value += quantity;
                ItemHistoryManager.RequiredInstance.RecordItem(item, quantity);
                lootObj?.MarkAsDisable();
                return;
            }

            if (item.isEXP)
            {
                StatsService.RequiredInstance.AddExperience(quantity);
                return;
            }

            if (quantity < 0)
            {
                if (!hasSelectedSlot)
                {
#if UNITY_EDITOR
                    Debug.Log("未标记任何槽位。");
#endif
                    return;
                }

                int removed = RemoveFromSlot(selectedSlot.Value, -quantity);
                hasSelectedSlot = false;
                if (removed > 0)
                    ItemHistoryManager.RequiredInstance.RecordItem(item, -removed);
                return;
            }

            if (quantity <= 0) return;

            // 先填快捷栏，再填背包；这是项目既定的优先级。
            int remaining = AddToSlots(state.Hotbar.Value, item, quantity);
            remaining = AddToSlots(state.Backpack.Value, item, remaining);
            if (remaining != quantity)
            {
                state.TouchHotbar();
                state.TouchBackpack();
            }

            if (remaining > 0)
                DropLoot(item, remaining, lootObj);
            else
                lootObj?.MarkAsDisable();
        }

        /// <summary>
        /// 把物品放进一组槽位，优先补同类堆叠，再占用空格。
        /// 返回仍未放下的数量，调用方据此决定是否掉落。
        /// </summary>
        private int AddToSlots(List<InventorySlotState> slots, ItemSO item, int quantity)
        {
            for (int i = 0; i < slots.Count && quantity > 0; i++)
            {
                InventorySlotState slot = slots[i];
                if (slot.item != null && slot.item != item) continue;

                int capacity = item.stackableSize - (slot.item == item ? slot.quantity : 0);
                int placed = Mathf.Min(capacity, quantity);
                if (placed <= 0) continue;

                slot.item = item;
                slot.quantity += placed;
                slots[i] = slot;
                ItemHistoryManager.RequiredInstance.RecordItem(item, placed);
                quantity -= placed;
            }
            return quantity;
        }

        /// <summary>从指定槽位移除数量；返回实际移除的数量。</summary>
        private int RemoveFromSlot(SlotAddress address, int quantity)
        {
            List<InventorySlotState> slots = address.isBackpack
                ? state.Backpack.Value
                : state.Hotbar.Value;
            if (address.index < 0 || address.index >= slots.Count) return 0;

            InventorySlotState slot = slots[address.index];
            if (slot.item == null || quantity <= 0) return 0;

            int removed = Mathf.Min(slot.quantity, quantity);
            slot.quantity -= removed;
            if (slot.quantity <= 0)
                slot = new InventorySlotState(null, 0);
            slots[address.index] = slot;

            if (address.isBackpack)
                state.TouchBackpack();
            else
                state.TouchHotbar();
            return removed;
        }

        /// <summary>检查快捷栏或背包是否有能放下该物品的槽位。</summary>
        private bool HasSpaceForItem(ItemSO item)
        {
            return HasSpaceInSlots(state.Hotbar.Value, item) ||
                   HasSpaceInSlots(state.Backpack.Value, item);
        }

        /// <summary>单组槽位的空位检查：空格可放，同类物品看剩余堆叠上限。</summary>
        private static bool HasSpaceInSlots(List<InventorySlotState> slots, ItemSO item)
        {
            if (item == null) return false;
            foreach (InventorySlotState slot in slots)
            {
                if (slot.item == null || slot.item == item)
                {
                    int capacity = item.stackableSize - (slot.item == item ? slot.quantity : 0);
                    if (capacity > 0) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 在玩家脚下生成掉落物。已有掉落物时直接复用它，
        /// 否则从对象池取一个并移动到当前场景。
        /// </summary>
        private void DropLoot(ItemSO item, int quantity, Loot existingLoot = null)
        {
            if (item == null || quantity <= 0) return;
            Vector3 dropPosition = player != null ? player.position : transform.position;

            if (existingLoot != null)
            {
                existingLoot.transform.position = dropPosition;
                existingLoot.Initialize(item, quantity);
                existingLoot.sr.enabled = true;
                existingLoot.gameObject.SetActive(true);
                StartCoroutine(ResetLootState(existingLoot));
                return;
            }

            if (lootPool == null || lootPrefab == null) return;

            Scene currentScene = SceneChanger.Instance != null
                ? SceneChanger.Instance.GetCurrentScene()
                : SceneManager.GetActiveScene();
            Loot loot = lootPool.Get();
            if (loot == null)
            {
                loot = Instantiate(lootPrefab, dropPosition, Quaternion.identity);
                loot.OnPoolGet();
            }
            else
            {
                loot.transform.SetPositionAndRotation(dropPosition, Quaternion.identity);
            }
            loot.SetSourcePool(lootPool);
            SceneManager.MoveGameObjectToScene(loot.gameObject, currentScene);
            loot.Initialize(item, quantity);
        }

        /// <summary>掉落动画播放到三成时允许重新拾取，避免刚丢下就被立刻捡回。</summary>
        private IEnumerator ResetLootState(Loot loot)
        {
            yield return new WaitForFixedUpdate();
            if (loot.animator == null) yield break;
            AnimatorStateInfo stateInfo = loot.animator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(stateInfo.length * 0.3f);
            loot.canBePick = true;
            loot.hasBeenPicked = false;
            loot.animator.SetBool(IsPicked, false);
        }

        /// <summary>记录玩家点击的槽位；出售物品时从这里扣减。</summary>
        public void SetSlotBeenClicked(SlotAddress address)
        {
            if (address.index < 0)
            {
                hasSelectedSlot = false;
                return;
            }

            selectedSlot = address;
            hasSelectedSlot = true;
        }

        /// <summary>右键丢弃一个物品到玩家脚下。</summary>
        public void DropByClick(SlotAddress address)
        {
            if (address.index < 0) return;
            if (!TryGetSlotState(address.isBackpack, address.index, out ItemSO item, out int quantity) ||
                item == null || quantity <= 0)
                return;

            DropLoot(item, 1);
            RemoveFromSlot(address, 1);
            ItemHistoryManager.Instance.RecordItem(item, -1);
        }

        /// <summary>使用一个物品：先执行效果，再从槽位移除。</summary>
        public void UseItem(SlotAddress address)
        {
            if (address.index < 0 || useItem == null) return;
            if (!TryGetSlotState(address.isBackpack, address.index, out ItemSO item, out int quantity) ||
                item == null || quantity <= 0)
                return;

            useItem.ApplyItemEffects(item);
            RemoveFromSlot(address, 1);
            ItemHistoryManager.Instance.RecordItem(item, -1);
        }

        /// <summary>结算金币：购买时传入正价格扣钱，出售时传入负价格加钱。</summary>
        public void UpdateGold(int price)
        {
            state.Gold.Value -= price;
        }

    }
}
