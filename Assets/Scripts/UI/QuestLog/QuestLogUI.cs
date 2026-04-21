using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestLogUI : MonoBehaviour
{
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questDescriptionText;
    
    public void HandleQuestClicked(QuestSO quest)
    {
        questNameText.text=quest.questName;
        questDescriptionText.text=quest.questDescription;
        foreach (var obj in quest.questObjectives)
        {
            QuestManager.instance.UpdateObjectiveProgress(quest,obj);
            Debug.Log("obj des:"+obj.description+"=>"+QuestManager.instance.GetProgressText(quest,obj));
        }
    }
}
