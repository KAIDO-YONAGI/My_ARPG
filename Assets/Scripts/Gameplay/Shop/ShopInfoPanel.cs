using System.Collections.Generic;
using Pipeline.SO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Shop
{
    /// <summary>
    /// 商店物品详情浮窗：显示名称、描述和属性，并跟随鼠标移动。
    /// 由 <see cref="ShopSlot"/> 在鼠标悬停时驱动。
    /// </summary>
    /// <remarks>
    /// 浮窗的 RectTransform 和 CanvasGroup 都在 ShopCanvas 预制体上拖引用，
    /// 运行时不再用 <c>GetComponent</c> 获取自身组件。
    /// </remarks>
    public class ShopInfoPanel : MonoBehaviour
    {
        [Header("浮窗控件")]
        [Tooltip("浮窗根节点，用于整体显隐。")]
        [SerializeField] private CanvasGroup infoPanel;

        [Tooltip("浮窗的 RectTransform，用于跟随鼠标位置。")]
        [SerializeField] private RectTransform infoPanelRect;

        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemDescriptionText;

        [Header("Stats")]
        [SerializeField] private TMP_Text[] itemStatsText;

        /// <summary>显示物品详情，并按有效属性动态填充属性行。</summary>
        public void ShowItemInfo(ItemSO item)
        {
            if (item == null) return;
            if (infoPanel != null) infoPanel.alpha = 1;
            if (itemNameText != null) itemNameText.text = item.itemName;
            if (itemDescriptionText != null) itemDescriptionText.text = item.itemDescription;

            List<string> stats = new List<string>();
            if (item.currentHealth > 0) stats.Add("Health:" + item.currentHealth);
            if (item.maxHealth > 0) stats.Add("MaxHealth:" + item.maxHealth);
            if (item.damage > 0) stats.Add("Damage:" + item.damage);
            if (item.speed > 0) stats.Add("Speed:" + item.speed);
            if (item.duration > 0) stats.Add("Duration:" + item.duration);

            if (itemStatsText == null) return;
            for (int i = 0; i < itemStatsText.Length; i++)
            {
                if (itemStatsText[i] == null) continue;
                if (i < stats.Count)
                {
                    itemStatsText[i].text = stats[i];
                    itemStatsText[i].gameObject.SetActive(true);
                }
                else
                {
                    // 不需要显示的位置直接禁用，避免 GridGroup 重排后留下空行。
                    itemStatsText[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>隐藏浮窗并清空文本。</summary>
        public void HideItemInfo()
        {
            if (infoPanel != null) infoPanel.alpha = 0;
            if (itemNameText != null) itemNameText.text = "";
            if (itemDescriptionText != null) itemDescriptionText.text = "";
        }

        /// <summary>让浮窗跟随鼠标，并向右下偏移一点避免遮住指针。</summary>
        public void FollowMouse()
        {
            if (Mouse.current == null || infoPanelRect == null) return;
            Vector3 mousePosition = Mouse.current.position.ReadValue();
            mousePosition += new Vector3(10, -10, 0);
            infoPanelRect.position = mousePosition;
        }
    }
}
