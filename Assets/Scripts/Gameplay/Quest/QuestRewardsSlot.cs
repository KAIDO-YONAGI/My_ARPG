using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Quest
{
    public class QuestRewardsSlot : MonoBehaviour
    {
        [SerializeField] private Image rewardImage;
        [SerializeField] private TMP_Text rewardQuantity;

        public void DisplayReward(Sprite sprite,int quantity)
        {
            rewardImage.sprite=sprite;
            rewardQuantity.text=quantity.ToString();
        }
    }
}
