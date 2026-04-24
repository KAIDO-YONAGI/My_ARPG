using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[CreateAssetMenu(fileName = "QuestOptionsEventSO", menuName = "Events/QuestOptionsEventSO", order = 0)]

public class QuestOptionsEventSO : ScriptableObject
{
    public UnityAction<MyEnums.QuestState> questOptionsEvent;
    
    public void OnQuestOptionsEventRaised(MyEnums.QuestState questState)
    {
        questOptionsEvent?.Invoke(questState);        
    }
    
}

