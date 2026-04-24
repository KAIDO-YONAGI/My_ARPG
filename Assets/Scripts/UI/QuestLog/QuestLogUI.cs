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

    public VoidEventSO openQuestEvent;

    private QuestSO currentQuest;

    private void OnEnable()
    {
        openQuestEvent.VoidEvent += ShowQuestOffer;
    }
    private void OnDisable()
    {
        openQuestEvent.VoidEvent -= ShowQuestOffer;

    }
    public void ShowQuestOffer()
    {
        QuestSO incomingQuestSO = QuestManager.instance.GetFirstIncompletedQuest();

        if (incomingQuestSO != null)
            HandleQuestClicked(incomingQuestSO);
    }

    public void HandleQuestClicked(QuestSO quest)//绑定了按钮事件
    {
        SetCurrentQuest(quest);
        questNameText.text = currentQuest.questName;
        questDescriptionText.text = currentQuest.questDescription;

        QuestManager.instance.OnQuestStateChanged(currentQuest, QuestManager.instance.GetQuestStateFromProgress(currentQuest));
        DisPlayObjectives();
        DisplayRewards();
    }
    private void SetCurrentQuest(QuestSO quest)
    {
        currentQuest = quest;
        QuestManager.instance.SetCurrentQuest(quest);
    }
    public void DisPlayObjectives()
    {
        for (int i = 0; i < objectiveSlots.Length; i++)
        {
            if (i < currentQuest.questObjectives.Count)
            {
                var obj = currentQuest.questObjectives[i];
                int currentAmount =
                    QuestManager.instance.GetCurrentObjAmount(currentQuest, obj);
                string progress =
                    QuestManager.instance.GetProgressText(currentQuest, obj);
                bool isCompleted = currentAmount >= obj.requiredAmount;

                objectiveSlots[i].gameObject.SetActive(true);

                objectiveSlots[i].RefreshObjectives(obj.description, progress, isCompleted);

                if (QuestManager.instance.GetQuestStateFromProgress(currentQuest)
                        == MyEnums.QuestState.Completed)//完成状态就不更新状态了
                    continue;
                else QuestManager.instance.UpdateObjectiveProgress(currentQuest, obj);
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
            if (i < currentQuest.rewards.Count)
            {
                var reward = currentQuest.rewards[i];
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
