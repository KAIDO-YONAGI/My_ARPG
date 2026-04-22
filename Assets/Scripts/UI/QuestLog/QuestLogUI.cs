using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class QuestLogUI : MonoBehaviour//UI更新有关逻辑
{
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questDescriptionText;
    [SerializeField] private QuestObjectiveSlot[] objectiveSlots;//任务条目槽位
    [SerializeField] private QuestRewardsSlot[] questRewardsSlot;//任务奖励槽位
    private QuestSO CurrentQuestSOInstance => QuestManager.instance.GetCurrentQuestSO();
    public void ShowQuestOffer(QuestSO incomingQuestSO)
    {
        HandleQuestClicked(incomingQuestSO);
    }

    public void HandleQuestClicked(QuestSO quest)//绑定了按钮事件
    {
        QuestManager.instance.SetCurrentQuest(quest);
        questNameText.text = CurrentQuestSOInstance.questName;
        questDescriptionText.text = CurrentQuestSOInstance.questDescription;

        QuestManager.instance.OnQuestStateChanged(CurrentQuestSOInstance, MyEnums.QuestState.Idle);
        DisPlayObjectives();
        DisplayRewards();
    }

    private void DisPlayObjectives()
    {
        for (int i = 0; i < objectiveSlots.Length; i++)
        {
            if (i < CurrentQuestSOInstance.questObjectives.Count)
            {
                var obj = CurrentQuestSOInstance.questObjectives[i];

                if (QuestManager.instance.GetQuestStateFromProgress(CurrentQuestSOInstance)
                        == MyEnums.QuestState.Complete)//完成状态就不更新了
                    continue;
                QuestManager.instance.UpdateObjectiveProgress(obj);

                int currentAmount =
                    QuestManager.instance.GetCurrentObjAmount(CurrentQuestSOInstance, obj);

                string progress =
                    QuestManager.instance.GetProgressText(CurrentQuestSOInstance, obj);

                bool isCompleted = currentAmount >= obj.requiredAmount;

                objectiveSlots[i].gameObject.SetActive(true);

                objectiveSlots[i].RefreshObjectives(obj.description, progress, isCompleted);

            }
            else
            {
                objectiveSlots[i].gameObject.SetActive(false);
            }
        }
    }
    private void DisplayRewards()
    {
        for (int i = 0; i < questRewardsSlot.Length; i++)
        {
            if (i < CurrentQuestSOInstance.rewards.Count)
            {
                var reward = CurrentQuestSOInstance.rewards[i];
                questRewardsSlot[i].DisplayReward(reward.rewardItem.icon, reward.quantity);

                questRewardsSlot[i].gameObject.SetActive(true);

            }
            else
            {
                questRewardsSlot[i].gameObject.SetActive(false);
            }
        }
    }
}
