using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestLogUI : MonoBehaviour
{
    public void HandleQuestClicked(QuestSO quest)
    {
        foreach (var obj in quest.questObjectives)
        {
            QuestManager.instance.UpdateObjectiveProgress(quest,obj);
            Debug.Log("obj des:"+obj.description+"=>"+QuestManager.instance.GetProgressText(quest,obj));
        }
    }
}
