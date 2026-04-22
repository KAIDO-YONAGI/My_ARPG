using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;
    public CanvasGroup questCanvaGroup;
    public VoidEventSO openQuestEvent;

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
    }

    private void OnDisable()
    {
        openQuestEvent.VoidEvent -= OnOpenQuestBoard;

    }
    private void OnOpenQuestBoard()
    {
        if (!canvasIsActive)
        {
            questCanvaGroup.alpha = 1;
            questCanvaGroup.blocksRaycasts = true;
            questCanvaGroup.interactable = true;
            canvasIsActive = true;
        }
        else
        {
            questCanvaGroup.alpha = 0;
            questCanvaGroup.blocksRaycasts = false;
            questCanvaGroup.interactable = false;
            canvasIsActive = false;
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
