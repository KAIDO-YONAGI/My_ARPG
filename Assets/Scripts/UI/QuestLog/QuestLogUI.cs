using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestLogUI : MonoBehaviour
{
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questDescriptionText;
    [SerializeField] private QuestObjectiveSlot[] objectiveSlots;//任务条目槽位

    private QuestSO currnetQuestSO;

    public void HandleQuestClicked(QuestSO quest)
    {
        currnetQuestSO = quest;
        questNameText.text = quest.questName;
        questDescriptionText.text = quest.questDescription;
        DisPlayObjectives();
    }


    private void DisPlayObjectives()
    {
        for (int i = 0; i < objectiveSlots.Length; i++)
        {
            if (i < currnetQuestSO.questObjectives.Count)
            {
                var obj = currnetQuestSO.questObjectives[i];
                QuestManager.instance.UpdateObjectiveProgress(currnetQuestSO, obj);
                int currentAmount=QuestManager.instance.GetCurrentAmount(currnetQuestSO,obj);
                string progress=QuestManager.instance.GetProgressText(currnetQuestSO,obj);
                bool isCompleted=currentAmount>=obj.requiredAmount;

                objectiveSlots[i].gameObject.SetActive(true);
                objectiveSlots[i].RefreshObjectives(obj.description,progress,isCompleted);

            }
            else
            {
                objectiveSlots[i].gameObject.SetActive(false);
            }
        }
    }
}
