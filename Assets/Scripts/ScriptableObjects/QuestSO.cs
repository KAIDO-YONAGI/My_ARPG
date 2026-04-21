using System;
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
    public List<Reward> rewards;
}

[Serializable]
public class Reward
{
    public ItemSO rewardItem;
    public int quantity;
}
[Serializable]
public class QuestObjective
{
    public string description;
    public ItemSO targetItem;
    public CharacterSO targetCharacter;
    public LocationSO targetLocation;


    public int requiredAmount;
    public int currentAmount;
}