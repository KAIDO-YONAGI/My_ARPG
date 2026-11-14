using TMPro;
using Contracts.State;
using UnityEngine;

namespace Gameplay.Inventory
{
    /// <summary>
    /// 常驻快捷栏的显示层。快捷栏不接入界面缓存，始终跟随常驻场景存在；
    /// 槽位和金币文本由场景直接拖引用，运行时不按名字遍历子物体。
    /// </summary>
    /// <remarks>
    /// 快捷栏只显示管理器中的前若干个槽位（管理器把快捷栏排在背包之前）。
    /// 启动时登记槽位地址，之后在每帧 <c>LateUpdate</c> 中比较版本并按需刷新。
    /// </remarks>
    public sealed class HotbarCanvasView : MonoBehaviour
    {
        [Header("快捷栏控件")]
        [Tooltip("快捷栏槽位，顺序即管理器中的快捷栏槽位顺序。")]
        [SerializeField] private InventorySlot[] hotbarSlots;

        [Tooltip("金币数量文本；没有金币显示需求时可以留空。")]
        [SerializeField] private TMP_Text goldAmountText;

        private VersionReader hotbarReader;
        private VersionReader goldReader;

        private void Awake()
        {
            Debug.Assert(hotbarSlots != null, nameof(hotbarSlots), this);
            for (int i = 0; i < hotbarSlots.Length; i++)
                Debug.Assert(hotbarSlots[i] != null, $"{nameof(hotbarSlots)}[{i}]", this);
        }

        private void Start()
        {
            InventoryManager manager = InventoryManager.RequiredInstance;

            for (int i = 0; i < hotbarSlots.Length; i++)
            {
                // 快捷栏槽位在管理器里固定排在背包之前，先标记自己的地址。
                hotbarSlots[i].ConfigureSlot(false, i);
            }
            Debug.Assert(manager.CurrentState.HotbarCount == hotbarSlots.Length,
                "快捷栏控件数量与管理器槽位数不一致。", this);

            hotbarReader.Invalidate();
            goldReader.Invalidate();
            RefreshIfChanged();
        }

        private void LateUpdate()
        {
            RefreshIfChanged();
        }

        /// <summary>
        /// 分别比较快捷栏和金币版本，避免金币变化时遍历槽位。
        /// </summary>
        private void RefreshIfChanged()
        {
            InventoryManager manager = InventoryManager.Instance;
            if (manager == null) return;

            InventoryManager.State state = manager.CurrentState;
            ulong hotbarVersion = state.HotbarVersion;
            ulong goldVersion = state.GoldVersion;
            if (hotbarReader.NeedsRefresh(state, hotbarVersion))
            {
                int slotCount = hotbarSlots.Length;
                for (int i = 0; i < slotCount; i++)
                {
                    if (i < state.HotbarCount)
                    {
                        InventorySlotState slot = state.GetHotbarSlot(i);
                        hotbarSlots[i].RestoreState(slot.item, slot.quantity);
                    }
                    else
                        hotbarSlots[i].RestoreState(null, 0);
                }
                hotbarReader.Record(state, hotbarVersion);
            }

            if (goldReader.NeedsRefresh(state, goldVersion))
            {
                if (goldAmountText != null)
                    goldAmountText.text = state.GoldAmount.ToString();
                goldReader.Record(state, goldVersion);
            }
        }
    }
}
