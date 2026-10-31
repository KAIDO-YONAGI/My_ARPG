using System;
using Contracts;
using UnityEngine;

namespace Pipeline.SO.Events
{
    [CreateAssetMenu(fileName = "QuestOptionsEventSO", menuName = "Events/QuestOptionsEventSO", order = 0)]

    public class QuestOptionsEventSO : ScriptableObject
    {
        public event Action<QuestState> questOptionsEvent;

        public void OnQuestOptionsEventRaised(QuestState questState)
        {
            questOptionsEvent?.Invoke(questState);
        }

    }
}

