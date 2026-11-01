using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Quest
{
    /// <summary>
    /// 任务奖励槽位：显示奖励物品图标与数量。
    /// 属于 QuestLogPanel 的子控件，数据由面板传入。
    /// </summary>
    public class QuestRewardsSlot : MonoBehaviour
    {
        [SerializeField] private Image rewardImage;
        [SerializeField] private TMP_Text rewardQuantity;

        private void Awake()
        {
            Debug.Assert(rewardImage != null, nameof(rewardImage), this);
            Debug.Assert(rewardQuantity != null, nameof(rewardQuantity), this);
        }

        /// <summary>刷新奖励图标与数量。</summary>
        public void DisplayReward(Sprite sprite, int quantity)
        {
            rewardImage.sprite = sprite;
            rewardQuantity.text = quantity.ToString();
        }
    }
}
