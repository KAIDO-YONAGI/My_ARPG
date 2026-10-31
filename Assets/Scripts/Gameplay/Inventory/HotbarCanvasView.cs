using Pipeline.SO;
using TMPro;
using UnityEngine;

namespace Gameplay.Inventory
{
    /// <summary>
    /// 常驻快捷栏的显示层。快捷栏不接入界面缓存，始终跟随常驻场景存在；
    /// 槽位和金币文本由场景直接拖引用，运行时不再按名字遍历子物体。
    /// </summary>
    /// <remarks>
    /// 快捷栏只显示管理器中的前若干个槽位（管理器把快捷栏排在背包之前）。
    /// 首次 <c>Start</c> 把场景里配置的初始物品交给管理器，之后每帧按脏版本刷新。
    /// </remarks>
    public sealed class HotbarCanvasView : MonoBehaviour
    {
        [Header("快捷栏控件")]
        [Tooltip("快捷栏槽位，顺序即管理器中的快捷栏槽位顺序。")]
        [SerializeField] private InventorySlot[] hotbarSlots;

        [Tooltip("金币数量文本；没有金币显示需求时可以留空。")]
        [SerializeField] private TMP_Text goldAmountText;

        /// <summary>上次刷新时的管理器脏版本。</summary>
        private int observedDirtyVersion = -1;

        private void Start()
        {
            var manager = InventoryManager.Instance;
            if (manager == null) return;

            if (hotbarSlots != null)
            {
                var items = new ItemSO[hotbarSlots.Length];
                var quantities = new int[hotbarSlots.Length];
                for (int i = 0; i < hotbarSlots.Length; i++)
                {
                    items[i] = hotbarSlots[i].InitialItem;
                    quantities[i] = hotbarSlots[i].InitialQuantity;
                    // 快捷栏槽位在管理器里固定排在背包之前，先标记自己的地址。
                    hotbarSlots[i].ConfigureSlot(false, i);
                }
                manager.InitializeHotbarState(items, quantities);
            }

            RefreshIfDirty(true);
        }

        private void Update()
        {
            RefreshIfDirty(false);
        }

        /// <summary>
        /// 按管理器脏版本刷新快捷栏和金币。
        /// <paramref name="force"/> 用于首次进入时强制刷新一次，避免初始版本恰好是 -1。
        /// </summary>
        private void RefreshIfDirty(bool force)
        {
            var manager = InventoryManager.Instance;
            if (manager == null || (!force && observedDirtyVersion == manager.DirtyVersion)) return;

            int slotCount = hotbarSlots != null ? hotbarSlots.Length : 0;
            for (int i = 0; i < slotCount; i++)
            {
                if (manager.TryGetSlotState(false, i, out var item, out int quantity))
                    hotbarSlots[i].RestoreState(item, quantity);
                else
                    hotbarSlots[i].RestoreState(null, 0);
            }

            if (goldAmountText != null)
                goldAmountText.text = manager.GoldAmount.ToString();
            observedDirtyVersion = manager.DirtyVersion;
        }
    }
}
