using TMPro;
using UnityEngine;

namespace Gameplay.Inventory
{
    /// <summary>
    /// 常驻快捷栏的显示层。快捷栏不接入界面缓存，始终跟随常驻场景存在；
    /// 槽位和金币文本由场景直接拖引用，运行时不按名字遍历子物体。
    /// </summary>
    /// <remarks>
    /// 快捷栏只显示管理器中的前若干个槽位（管理器把快捷栏排在背包之前）。
    /// 首次 <c>Start</c> 把场景里配置的初始物品交给管理器，之后每帧按脏标记刷新。
    /// </remarks>
    public sealed class HotbarCanvasView : MonoBehaviour
    {
        [Header("快捷栏控件")]
        [Tooltip("快捷栏槽位，顺序即管理器中的快捷栏槽位顺序。")]
        [SerializeField] private InventorySlot[] hotbarSlots;

        [Tooltip("金币数量文本；没有金币显示需求时可以留空。")]
        [SerializeField] private TMP_Text goldAmountText;

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
            manager.InitializeHotbarState(hotbarSlots);

            manager.CurrentState.MarkHotbarDirty();
            manager.CurrentState.MarkGoldDirty();
            RefreshIfDirty();
        }

        private void Update()
        {
            RefreshIfDirty();
        }

        /// <summary>
        /// 分别按脏标记刷新快捷栏和金币，避免金币变化时遍历槽位。
        /// </summary>
        private void RefreshIfDirty()
        {
            InventoryManager manager = InventoryManager.Instance;
            if (manager == null) return;

            InventoryManager.State state = manager.CurrentState;
            if (state.IsHotbarDirty)
            {
                int slotCount = hotbarSlots.Length;
                for (int i = 0; i < slotCount; i++)
                {
                    if (i < state.HotbarCount)
                    {
                        InventoryManager.InventorySlotState slot = state.GetHotbarSlot(i);
                        hotbarSlots[i].RestoreState(slot.item, slot.quantity);
                    }
                    else
                        hotbarSlots[i].RestoreState(null, 0);
                }
                state.ClearHotbarDirty();
            }

            if (state.IsGoldDirty)
            {
                if (goldAmountText != null)
                    goldAmountText.text = state.GoldAmount.ToString();
                state.ClearGoldDirty();
            }
        }
    }
}
