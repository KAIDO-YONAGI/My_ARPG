using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;
    public CanvasGroup questCanvaGroup;

    [Header("Events")]

    public VoidEventSO openQuestEventSO;
    public LoadQuestEventSO loadQuestEventSO;
    public QuestOptionsEventSO questOptionsEventSO;
    public InventorySlotsStatsSO QuestRewardRequest;



    [Header("Options")]
    public CanvasGroup acceptCanvaGroup;
    public CanvasGroup declineCanvaGroup;
    public CanvasGroup completeCanvaGroup;


    [Header("QuestLogSlots")]
    public QuestLogSlot[] questLogSlots;

    [Header("Canvas To Operate While No Quests")]
    public CanvasGroup detailsCanvaGroup;
    public CanvasGroup promptCanvaGroup;

    [Header("QuestLogUI")]
    public QuestLogUI questLogUI;

    private MyEnums.QuestState currentQuestState = MyEnums.QuestState.Idle;

    //（任务（任务状态，任务要求（要求条目）））
    private Dictionary<QuestSO, QuestProgressData> questProgress = new();
    private bool canvasIsActive;
    private List<QuestSO> currentBoardLoadQuests;
    private QuestSO currentQuest;

    public void SetCurrentQuest(QuestSO quest)
    {
        currentQuest = quest;
    }
    public bool CanvasIsActive => canvasIsActive;
    class QuestProgressData
    {
        public QuestProgressData(List<QuestObjective> objectives)
        {
            foreach (var obj in objectives)
            {
                questObjectives[obj] = 0;
            }
        }
        public MyEnums.QuestState questState = MyEnums.QuestState.Idle;
        public Dictionary<QuestObjective, int> questObjectives = new();
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
        openQuestEventSO.VoidEvent += OnOpenQuestBoard;
        loadQuestEventSO.LoadQuestEvent += OnReFreshQuestState;
        questOptionsEventSO.questOptionsEvent += OnQuestOptionChose;

    }

    private void OnDisable()
    {
        openQuestEventSO.VoidEvent -= OnOpenQuestBoard;
        loadQuestEventSO.LoadQuestEvent -= OnReFreshQuestState;
        questOptionsEventSO.questOptionsEvent -= OnQuestOptionChose;

    }

    private void OnQuestOptionChose(MyEnums.QuestState questStateToShift)
    {

        if (questStateToShift == MyEnums.QuestState.Completed)
        {
            if (IsQuestObjDone(currentQuest))
            {
                OnQuestStateChanged(currentQuest, questStateToShift);
            }
            else Debug.Log("Quest Not Done");
        }
        else OnQuestStateChanged(currentQuest, questStateToShift);

    }
    private void OnOpenQuestBoard()
    {
        if (!canvasIsActive)
        {
            SetCanvaState(questCanvaGroup, true);

            canvasIsActive = true;
            questLogUI.ShowQuestOffer();
        }
        else
        {
            SetCanvaState(questCanvaGroup, false);

            canvasIsActive = false;
        }
    }
    private void OnReFreshQuestState(List<QuestSO> quests)
    {
        currentBoardLoadQuests = quests;
        InitializeQuestProgress(quests);
        InitiateQuestSlots(quests);

        if (quests.Count == 0
         || IsAllQuestsCompleted(quests)
        )
        {
            SetNoQuestsState(true);
            return;
        }
        else
            SetNoQuestsState(false);

        foreach (var quest in quests)
        {
            if (IsQuestObjDone(quest) && GetQuestStateFromProgress(quest) == MyEnums.QuestState.Accepted)
            {
                OnQuestStateChanged(quest, MyEnums.QuestState.IsToComplete);
                Debug.Log("IsToComplete");

            }
            else Debug.Log("not done");
        }
    }
    private void RaiseRewardEvent(QuestSO quest)
    {
        foreach (var reward in quest.rewards)
        {
            ItemSO item = reward.rewardItem;
            int quantity = reward.quantity;
            QuestRewardRequest.RaiseInventoryUpdateRequest(item, 0, quantity);
        }
    }
    private void SetNoQuestsState(bool isOpenWhiteUI)
    {
        SetCanvaState(detailsCanvaGroup, !isOpenWhiteUI);
        SetCanvaState(promptCanvaGroup, isOpenWhiteUI);
    }
    private void InitializeQuestProgress(List<QuestSO> quests)
    {
        foreach (var quest in quests)
        {
            if (!questProgress.ContainsKey(quest))
            {
                questProgress.Add(quest, new QuestProgressData(quest.questObjectives));
            }
        }
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
            QuestSO quest = quests[i];
            questLogSlots[i].SetQuest(quest);

            if (GetQuestStateFromProgress(quest) == MyEnums.QuestState.Completed)
            {
                SetQuestSlotToDoneState(quest);
            }
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

        questProgress[quest].questState = currentQuestState;


        if (currentQuestState == MyEnums.QuestState.Idle)
        {
            SetCanvaState(acceptCanvaGroup, true);

        }
        else if (currentQuestState == MyEnums.QuestState.Accepted)
        {
            SetCanvaState(declineCanvaGroup, true);
            SetCanvaState(completeCanvaGroup, true);

        }
        else if (currentQuestState == MyEnums.QuestState.Decline)
        {
            SetCanvaState(acceptCanvaGroup, true);
            SetCanvaState(declineCanvaGroup, true);
            //TODO 取消任务逻辑
        }
        else if (currentQuestState == MyEnums.QuestState.IsToComplete)
        {
            SetCanvaState(declineCanvaGroup, true);
            SetCanvaState(completeCanvaGroup, true);

            //TODO 完成任务逻辑 以后点按钮后进入Completed,将任务固定
        }
        else if (currentQuestState == MyEnums.QuestState.Completed)
        {
            SetQuestSlotToDoneState(quest);
            RaiseRewardEvent(quest);
            //TODO 完成任务逻辑 以后点按钮后进入Completed,将任务固定
        }

        questLogUI.DisPlayObjectives();

    }
    public void UpdateObjectiveProgress(QuestSO quest, QuestObjective obj)//更新完成条件
    {
        var progressDictionary = questProgress[quest].questObjectives;
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
            if (questProgressData.questObjectives.TryGetValue(obj, out int amount))
                return amount;
        return 0;
    }
    private bool IsAllQuestsCompleted(List<QuestSO> quests)
    {
        foreach (var quest in quests)
        {
            if (!questProgress.ContainsKey(quest)) return false;
            MyEnums.QuestState questState = questProgress[quest].questState;

            if (questState != MyEnums.QuestState.Completed)
                return false;
        }

        return true;
    }
    public void SetQuestSlotToDoneState(QuestSO quest)
    {
        foreach (var questSlot in questLogSlots)
        {
            if (questSlot.currentQuest == quest)
            {
                CanvasGroup questSlotCanvas = questSlot.GetComponent<CanvasGroup>();
                questSlotCanvas.alpha = .2f;
                questSlotCanvas.interactable = false;
                questSlotCanvas.blocksRaycasts = false;

            }
        }
    }
    private bool IsQuestObjDone(QuestSO quest)
    {
        if (!questProgress.ContainsKey(quest)) return false;

        MyEnums.QuestState questState = questProgress[quest].questState;
        if (questState == MyEnums.QuestState.Completed)
            return true;

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
