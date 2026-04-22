using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;
    public CanvasGroup questCanvaGroup;
    public VoidEventSO openQuestEvent;

    [Header("Options")]
    public CanvasGroup acceptCanvaGroup;
    public CanvasGroup declineCanvaGroup;
    public CanvasGroup completeCanvaGroup;

    private MyEnums.QuestState currentuestState = MyEnums.QuestState.Idle;

    private bool canvasIsActive;
    private Dictionary<QuestSO, Dictionary<QuestObjective, int>> questProgress = new();


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }


    }

    private void OnEnable()
    {
        openQuestEvent.VoidEvent += OnOpenQuestBoard;

        SetCanvaState(acceptCanvaGroup, false);
        SetCanvaState(declineCanvaGroup, false);
        SetCanvaState(completeCanvaGroup, false);
    }

    private void OnDisable()
    {
        openQuestEvent.VoidEvent -= OnOpenQuestBoard;

    }
    private void OnOpenQuestBoard()
    {
        if (!canvasIsActive)
        {
            SetCanvaState(questCanvaGroup, true);

            canvasIsActive = true;
        }
        else
        {
            SetCanvaState(questCanvaGroup, false);

            canvasIsActive = false;
        }
    }
    private void SetCanvaState(CanvasGroup canva, bool state)
    {
        canva.alpha = state ? 1 : 0;
        canva.blocksRaycasts = state;
        canva.interactable = state;
    }
    public void OnCurrentQuestStateChanged(MyEnums.QuestState state)
    {
        currentuestState = state;

        if (currentuestState == MyEnums.QuestState.Idle)
        {
            SetCanvaState(acceptCanvaGroup, true);
            SetCanvaState(declineCanvaGroup, true);

        }
        else if (currentuestState == MyEnums.QuestState.Accepted)
        {
            SetCanvaState(acceptCanvaGroup, false);
            SetCanvaState(completeCanvaGroup, false);
        }
        else if (currentuestState == MyEnums.QuestState.Decline)
        {
            SetCanvaState(acceptCanvaGroup, true);
            SetCanvaState(declineCanvaGroup, true);
            //TODO 完成任务逻辑
        }
        else if (currentuestState == MyEnums.QuestState.Complete)
        {
            SetCanvaState(completeCanvaGroup,true);
            //TODO 完成任务逻辑
        }
    }
    public void UpdateObjectiveProgress(QuestSO quest, QuestObjective obj)
    {
        if (!questProgress.ContainsKey(quest))
        {
            questProgress.Add(quest, new Dictionary<QuestObjective, int>());
        }
        var progressDictionary = questProgress[quest];
        int newAmount = 0;

        if (obj.targetItem != null)
        {
            newAmount = ItemHistoryManager.instance.GetItemQuantity(obj.targetItem);
        }
        else if (obj.targetCharacter != null && ConversationHistoryManager.instance.HasChatedWith(obj.targetCharacter))
        {
            newAmount = obj.requiredAmount;
        }

        progressDictionary[obj] = newAmount;
    }
    public string GetProgressText(QuestSO quest, QuestObjective obj)
    {
        int currentAmount = GetCurrentAmount(quest, obj);

        if (currentAmount >= obj.requiredAmount)
            return "√";
        else if (obj != null)
            return $"{currentAmount}/{obj.requiredAmount}";
        else
            return "In Progress";
    }
    public int GetCurrentAmount(QuestSO quest, QuestObjective obj)
    {
        if (questProgress.TryGetValue(quest, out var objectiveDictionary))
            if (objectiveDictionary.TryGetValue(obj, out int amount))
                return amount;
        return 0;
    }
}
