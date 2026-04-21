using System.Collections;
using System.Collections.Generic;
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
    public void UpdateObjectiveProgress(QuestSO quest,QuestObjective obj)
    {
        
    }
    public string GetProgressText(QuestSO quest, QuestObjective obj)
    {
        int currentAmount = 0;

        if (currentAmount >= obj.requiredAmount)
            return "Completed";
        else if (obj != null)
            return $"{currentAmount}/{obj.requiredAmount}";
        else
            return "In Progress";
    }
}
