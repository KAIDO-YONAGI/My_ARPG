using Contracts.Base;
using Pipeline.SO;
using UnityEngine;

namespace Gameplay.Inventory
{
    /// <summary>
    /// 背包画布的显示层。由 UI 缓存按需创建，槽位全部在 BackpackCanvas 预制体里拖引用，
    /// 打开时读取 <see cref="InventoryManager"/> 的槽位状态并刷新显示。
    /// </summary>
    /// <remarks>
    /// 背包数据由管理器按“快捷栏在前、背包在后”的顺序保存，视图只负责把状态画到槽位上。
    /// 首次打开时把预制体里配置的初始物品交给管理器，此后管理器以内存中的槽位状态为准，预制体只提供初始值。
    /// </remarks>
    public sealed class BackpackCanvasView : CachedCanvasViewBase
    {
        [Header("背包槽位")]
        [Tooltip("背包网格里的槽位，顺序即管理器中的背包槽位顺序。")]
        [SerializeField] private InventorySlot[] slots;

        private void Awake()
        {
            Debug.Assert(slots != null, nameof(slots), this);
            for (int i = 0; i < slots.Length; i++)
                Debug.Assert(slots[i] != null, $"{nameof(slots)}[{i}]", this);
        }

        /// <summary>
        /// 打开时先登记预制体上的初始物品，再刷新显示。
        /// 管理器只在第一次调用时接收初始槽位，后续打开不会重置玩家已经获得的物品。
        /// </summary>
        protected override void OnOpen()
        {
            InventoryManager manager = InventoryManager.RequiredInstance;
            for (int i = 0; i < slots.Length; i++)
            {
                // 每个新实例都要登记槽位地址，点击才能定位管理器中的背包数据。
                slots[i].ConfigureSlot(true, i);
            }
            manager.InitializeBackpackState(slots);

            manager.CurrentState.MarkBackpackDirty();
            RefreshIfDirty();
        }

        private void Update()
        {
            RefreshIfDirty();
        }

        /// <summary>按管理器脏标记刷新背包槽位；槽位数量不足时多余的槽位清空。</summary>
        private void RefreshIfDirty()
        {
            InventoryManager manager = InventoryManager.Instance;
            if (!IsOpen || manager == null) return;

            InventoryManager.State state = manager.CurrentState;
            if (!state.IsBackpackDirty) return;

            int slotCount = slots.Length;
            for (int i = 0; i < slotCount; i++)
            {
                if (i < state.BackpackCount)
                {
                    InventoryManager.InventorySlotState slot = state.GetBackpackSlot(i);
                    slots[i].RestoreState(slot.item, slot.quantity);
                }
                else
                    slots[i].RestoreState(null, 0);
            }

            state.ClearBackpackDirty();
        }
    }
}
