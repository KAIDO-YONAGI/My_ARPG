using System.Collections.Generic;
using Gameplay.Inventory;
using UnityEngine;

namespace Pipeline.SO
{
    /// <summary>
    /// 背包与快捷栏的初始槽位配置。管理器在常驻场景加载时读取一次并复制成运行态，
    /// 资产内容在运行期保持不变。
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryConfigSO", menuName = "Data/InventoryConfigSO", order = 0)]
    public class InventoryConfigSO : ScriptableObject
    {
        [Tooltip("快捷栏每个槽位的初始物品与数量，长度即快捷栏槽位数。")]
        [SerializeField] private InventorySlotState[] hotbarSlots;

        [Tooltip("背包每个槽位的初始物品与数量，长度即背包槽位数。")]
        [SerializeField] private InventorySlotState[] backpackSlots;

        /// <summary>快捷栏初始槽位。</summary>
        public IReadOnlyList<InventorySlotState> HotbarSlots => hotbarSlots;

        /// <summary>背包初始槽位。</summary>
        public IReadOnlyList<InventorySlotState> BackpackSlots => backpackSlots;
    }
}
