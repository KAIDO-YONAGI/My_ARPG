using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;
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
    private Dictionary<QuestSO, Dictionary<QuestObjective, int>> questProgress = new();
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
        int currentAmount = GetCurrentAmount(quest,obj);

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
