using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[CreateAssetMenu(fileName = "LoadQuestEventSO", menuName = "Events/LoadQuestEventSO", order = 0)]

public class LoadQuestEventSO : ScriptableObject
{
    public UnityAction<List<QuestSO>> LoadQuestEvent;
    public void OnLoadQuestEventRaised(List<QuestSO> questsOnBoard)
    {
        LoadQuestEvent?.Invoke(questsOnBoard);
    }
}
