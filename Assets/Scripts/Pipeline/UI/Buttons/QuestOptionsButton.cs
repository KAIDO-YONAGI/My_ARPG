using Contracts.Enums;
using Pipeline.SO.Events;
using UnityEngine;

namespace Pipeline.UI.Buttons
{
    public class QuestOptionsButton : MonoBehaviour
    {
        [SerializeField]private QuestState questStateToShift;
        [SerializeField] private QuestOptionsEventSO questOptionsEventSO;

        public void OnOptionButtonClicked()//unity中绑定
        {
            questOptionsEventSO.OnQuestOptionsEventRaised(questStateToShift);
        }
    }
}
