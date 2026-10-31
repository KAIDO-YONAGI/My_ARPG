using Pipeline.SO;
using Pipeline.UI.Runtime;
using UnityEngine;

namespace Gameplay.Inventory
{
    /// <summary>
    /// 背包画布的显示层。由 UI 缓存按需创建，槽位全部在 BackpackCanvas 预制体里拖引用，
    /// 打开时读取 <see cref="InventoryManager"/> 的槽位状态并刷新显示。
    /// </summary>
    /// <remarks>
    /// 背包数据由管理器按“快捷栏在前、背包在后”的顺序保存，视图只负责把状态画到槽位上。
    /// 首次打开时把预制体里配置的初始物品交给管理器，之后管理器不再回头读预制体。
    /// </remarks>
    public sealed class BackpackCanvasView : CachedCanvasView
    {
        [Header("背包槽位")]
        [Tooltip("背包网格里的槽位，顺序即管理器中的背包槽位顺序。")]
        [SerializeField] private InventorySlot[] slots;

        /// <summary>上次刷新时的管理器脏版本。</summary>
        private int observedDirtyVersion = -1;

        /// <summary>
        /// 打开时先登记预制体上的初始物品，再刷新显示。
        /// 管理器只在第一次调用时接收快照，后续打开不会重置玩家已经获得的物品。
        /// </summary>
        protected override void OnOpen()
        {
            var manager = InventoryManager.Instance;
            if (manager != null && slots != null)
            {
                var items = new ItemSO[slots.Length];
                var quantities = new int[slots.Length];
                for (int i = 0; i < slots.Length; i++)
                {
                    items[i] = slots[i].InitialItem;
                    quantities[i] = slots[i].InitialQuantity;
                }
                manager.InitializeBackpackState(items, quantities);
            }

            observedDirtyVersion = -1;
            RefreshIfDirty();
        }

        private void Update()
        {
            RefreshIfDirty();
        }

        /// <summary>按管理器脏版本刷新背包槽位；槽位数量不足时多余的槽位清空。</summary>
        private void RefreshIfDirty()
        {
            var manager = InventoryManager.Instance;
            if (!IsOpen || manager == null || observedDirtyVersion == manager.DirtyVersion) return;

            int slotCount = slots != null ? slots.Length : 0;
            for (int i = 0; i < slotCount; i++)
            {
                if (manager.TryGetSlotState(true, i, out var item, out int quantity))
                    slots[i].RestoreState(item, quantity);
                else
                    slots[i].RestoreState(null, 0);
            }

            observedDirtyVersion = manager.DirtyVersion;
        }
    }
}
