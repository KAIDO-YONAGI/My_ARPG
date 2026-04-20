using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestLogSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questLevelText;
    public QuestSO currentQuest;

    private void OnValidate()
    {
        if (currentQuest != null)
            SetQuest(currentQuest);
    }

    public void SetQuest(QuestSO quest)
    {
        currentQuest = quest;
        questNameText.text = quest.questName;
        questLevelText.text = "Lv." + quest.lv;
    }


}
