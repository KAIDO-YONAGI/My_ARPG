using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New Quest", menuName = "QuestSO")]
public class QuestSO : ScriptableObject
{
    public string questName;
    public int lv;
    [TextArea] public string questDescription;

    public List<QuestObjective> questObjectives;
}



[System.Serializable]
public class QuestObjective
{
    public string description;

    [SerializeField] private Object target;
    public ItemSO TargetItem=>target as ItemSO;
    public CharacterSO TargetCharactertarget=>target as CharacterSO;
    public LocationSO TargetLocationtarget=>target as LocationSO;


    public int requiredAmount;
    public int currentAmount;
}