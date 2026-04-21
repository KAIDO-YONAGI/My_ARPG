using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class QuestRewardsSlot : MonoBehaviour
{
    public Image rewardImage;
    public TMP_Text rewardQuantity;

    public void DisplayReward(Sprite sprite,int quantity)
    {
        rewardImage.sprite=sprite;
        rewardQuantity.text=quantity.ToString();
    }
}
