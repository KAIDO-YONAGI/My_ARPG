using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;
    public CanvasGroup questCanvaGroup;

    [Header("Events")]

    public VoidEventSO openQuestEvent;
    public LoadQuestEventSO loadQuestEventSO;


    [Header("Options")]
    public CanvasGroup acceptCanvaGroup;
    public CanvasGroup declineCanvaGroup;
    public CanvasGroup completeCanvaGroup;


    [Header("QuestLogSlots")]

    public QuestLogSlot[] questLogSlots;
    [Header("Canvas To Operate While No Quests")]
    public CanvasGroup detailsCanvaGroup;
    public CanvasGroup promptCanvaGroup;

    private MyEnums.QuestState currentQuestState = MyEnums.QuestState.Idle;

    //（任务（任务状态，任务要求（要求条目）））
    private Dictionary<QuestSO, QuestProgressData> questProgress = new();
    private bool canvasIsActive;
    private List<QuestSO> currentBoardLoadQuests;
    public bool CanvasIsActive => canvasIsActive;
    class QuestProgressData
    {
        public QuestProgressData(List<QuestObjective> objectives)
        {
            foreach (var obj in objectives)
            {
                this.objectives[obj] = 0;
            }
        }
        public MyEnums.QuestState questState = MyEnums.QuestState.Idle;
        public Dictionary<QuestObjective, int> objectives = new();
    }
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
        loadQuestEventSO.LoadQuestEvent += ReFreshQuestStates;

    }

    private void OnDisable()
    {
        openQuestEvent.VoidEvent -= OnOpenQuestBoard;
        loadQuestEventSO.LoadQuestEvent -= ReFreshQuestStates;

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
    private void ReFreshQuestStates(List<QuestSO> quests)
    {
        InitiateQuestSlots(quests);
        currentBoardLoadQuests = quests;

        if (quests.Count == 0)
        {
            DealNoQuests(true);
            return;
        }
        else
            DealNoQuests(false);

        foreach (var quest in quests)
        {
            if (!questProgress.ContainsKey(quest))
            {
                questProgress.Add(quest, new QuestProgressData(quest.questObjectives));
            }
            if (IsQuestDone(quest))
            {
                OnQuestStateChanged(quest, MyEnums.QuestState.IsToComplete);
                Debug.Log("IsToComplete");

            }
            else Debug.Log("not done");
        }
    }
    private void DealNoQuests(bool isOpenWhiteUI)
    {
        SetCanvaState(detailsCanvaGroup, !isOpenWhiteUI);
        SetCanvaState(promptCanvaGroup, isOpenWhiteUI);
    }
    private void InitiateQuestSlots(List<QuestSO> quests)
    {
        foreach (var questSlot in questLogSlots)
        {
            questSlot.SetQuestActive(false);
        }

        int length = Math.Min(questLogSlots.Length, quests.Count);
        for (int i = 0; i < length; i++)
        {
            questLogSlots[i].SetQuest(quests[i]);
        }
    }
    public QuestSO GetFirstIncompletedQuest()
    {
        foreach (var quest in currentBoardLoadQuests)
        {
            if (questProgress[quest].questState != MyEnums.QuestState.Completed)
                return quest;
        }
        return null;
    }
    public MyEnums.QuestState GetQuestStateFromProgress(QuestSO quest)
    {
        return questProgress[quest].questState;
    }
    public void OnQuestStateChanged(QuestSO quest, MyEnums.QuestState state)
    {
        if (!questProgress.ContainsKey(quest)) return;

        SetCanvaState(acceptCanvaGroup, false);
        SetCanvaState(declineCanvaGroup, false);
        SetCanvaState(completeCanvaGroup, false);

        currentQuestState = state;

        if (questProgress[quest].questState == MyEnums.QuestState.Completed)
            return;
        else
            questProgress[quest].questState = currentQuestState;


        if (currentQuestState == MyEnums.QuestState.Idle)
        {
            SetCanvaState(acceptCanvaGroup, true);
            SetCanvaState(declineCanvaGroup, true);

        }
        else if (currentQuestState == MyEnums.QuestState.Accepted)
        {
            SetCanvaState(declineCanvaGroup, true);

        }
        else if (currentQuestState == MyEnums.QuestState.Decline)
        {
            SetCanvaState(acceptCanvaGroup, true);
            SetCanvaState(declineCanvaGroup, true);

            //TODO 取消任务逻辑
        }
        else if (currentQuestState == MyEnums.QuestState.IsToComplete)
        {
            //TODO 完成任务逻辑 以后点按钮后进入Completed,将任务固定
        }

    }
    public void UpdateObjectiveProgress(QuestSO quest, QuestObjective obj)
    {
        var progressDictionary = questProgress[quest].objectives;
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
        int currentObjAmount = GetCurrentObjAmount(quest, obj);

        if (IsObjDone(quest, obj))
        {
            return "√";
        }

        else if (obj != null)
            return $"{currentObjAmount}/{obj.requiredAmount}";
        else
            return "In Progress";
    }
    public int GetCurrentObjAmount(QuestSO quest, QuestObjective obj)
    {
        if (questProgress.TryGetValue(quest, out var questProgressData))
            if (questProgressData.objectives.TryGetValue(obj, out int amount))
                return amount;
        return 0;
    }
    private bool IsQuestDone(QuestSO quest)
    {
        foreach (var obj in quest.questObjectives)
        {
            if (!IsObjDone(quest, obj))
                return false;
        }
        return true;
    }


    private bool IsObjDone(QuestSO quest, QuestObjective obj)
    {
        if (GetCurrentObjAmount(quest, obj) < obj.requiredAmount)
            return false;
        else
            return true;
    }
    private void SetCanvaState(CanvasGroup canva, bool state)
    {
        canva.alpha = state ? 1 : 0;
        canva.blocksRaycasts = state;
        canva.interactable = state;
    }

}
