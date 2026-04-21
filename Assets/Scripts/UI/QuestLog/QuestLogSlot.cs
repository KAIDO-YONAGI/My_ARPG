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
    public QuestLogUI questLogUI;
    private void OnValidate()
    {
        gameObject.SetActive(false);
        if (currentQuest != null)
            SetQuest(currentQuest);
        
    }

    public void SetQuest(QuestSO quest)
    {
        currentQuest = quest;
        questNameText.text = quest.questName;
        questLevelText.text = "Lv." + quest.lv;
        gameObject.SetActive(true);

    }
    public void OnSlotClicked()
    {
        questLogUI.HandleQuestClicked(currentQuest);
    }

}
