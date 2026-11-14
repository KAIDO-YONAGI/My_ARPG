using Contracts.Base;
using Contracts.State;
using UnityEngine;

namespace Gameplay.Inventory
{
    /// <summary>
    /// 背包画布的显示层。由 UI 缓存按需创建，槽位全部在 BackpackCanvas 预制体里拖引用，
    /// 打开时读取 <see cref="InventoryManager"/> 的槽位状态并刷新显示。
    /// </summary>
    /// <remarks>
    /// 背包数据由管理器按“快捷栏在前、背包在后”的顺序保存，视图只负责把状态画到槽位上。
    /// 槽位内容在常驻场景加载时由配置建立，打开界面只登记地址并刷新显示。
    /// </remarks>
    public sealed class BackpackCanvasView : CachedCanvasViewBase
    {
        [Header("背包槽位")]
        [Tooltip("背包网格里的槽位，顺序即管理器中的背包槽位顺序。")]
        [SerializeField] private InventorySlot[] slots;

        private VersionReader backpackReader;

        private void Awake()
        {
            Debug.Assert(slots != null, nameof(slots), this);
            for (int i = 0; i < slots.Length; i++)
                Debug.Assert(slots[i] != null, $"{nameof(slots)}[{i}]", this);
        }

        /// <summary>打开时登记槽位地址，再刷新显示。</summary>
        protected override void OnOpen()
        {
            InventoryManager manager = InventoryManager.RequiredInstance;
            for (int i = 0; i < slots.Length; i++)
            {
                // 每个新实例都要登记槽位地址，点击才能定位管理器中的背包数据。
                slots[i].ConfigureSlot(true, i);
            }
            Debug.Assert(manager.CurrentState.BackpackCount == slots.Length,
                "背包控件数量与管理器槽位数不一致。", this);

            backpackReader.Invalidate();
            RefreshIfChanged();
        }

        private void LateUpdate()
        {
            RefreshIfChanged();
        }

        /// <summary>按本视图记录的版本刷新背包；槽位数量不足时多余的槽位清空。</summary>
        private void RefreshIfChanged()
        {
            InventoryManager manager = InventoryManager.Instance;
            if (!IsOpen || manager == null) return;

            InventoryManager.State state = manager.CurrentState;
            ulong version = state.BackpackVersion;
            if (!backpackReader.NeedsRefresh(state, version)) return;

            int slotCount = slots.Length;
            for (int i = 0; i < slotCount; i++)
            {
                if (i < state.BackpackCount)
                {
                    InventorySlotState slot = state.GetBackpackSlot(i);
                    slots[i].RestoreState(slot.item, slot.quantity);
                }
                else
                    slots[i].RestoreState(null, 0);
            }

            backpackReader.Record(state, version);
        }
    }
}
