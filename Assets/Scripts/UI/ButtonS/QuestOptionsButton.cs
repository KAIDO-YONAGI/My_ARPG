using UnityEngine;
using UnityEngine.Events;

public class QuestOptionsButton : MonoBehaviour
{
    [SerializeField]private MyEnums.QuestState questStateToShift;
    public QuestOptionsEventSO questOptionsEventSO;

    public void OnOptionButtonClicked()//unity中绑定
    {
        questOptionsEventSO.OnQuestOptionsEventRaised(questStateToShift);
    }
}
