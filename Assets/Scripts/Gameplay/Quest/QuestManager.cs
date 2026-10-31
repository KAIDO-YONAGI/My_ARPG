using System.Collections.Generic;
using Contracts;
using Gameplay.Dialog.HistoryManager;
using Pipeline.SO;
using Pipeline.SO.Events;
using Pipeline.UI.Runtime;
using UnityEngine;

namespace Gameplay.Quest
{
    /// <summary>
    /// 任务系统的持久状态管理器。负责任务板加载、任务状态流转、目标进度计算和奖励发放，
    /// 界面由 <see cref="QuestCanvasView"/> 按脏版本拉取显示。
    /// </summary>
    /// <remarks>
    /// 管理器不持有任何 UI 控件引用：任务板打开/关闭通过 <see cref="UICacheManager"/> 发请求，
    /// 任务进度保存在这里，所以界面被缓存销毁后不会丢失。
    /// </remarks>
    public class QuestManager : YSingleton<QuestManager>
    {
        [Header("Events To Receive")]
        [Tooltip("玩家请求打开任务板。")]
        [SerializeField] private VoidEventSO openQuestEventSO;

        [Tooltip("任务板数据加载完成后刷新任务进度。")]
        [SerializeField] private LoadQuestEventSO loadQuestEventSO;

        [Tooltip("玩家选择接取、放弃或完成任务。")]
        [SerializeField] private QuestOptionsEventSO questOptionsEventSO;

        [Tooltip("场景加载完成后关闭任务板。")]
        [SerializeField] private SceneLoadedEventSO sceneLoadedEvent;

        [Header("Events To Trigger")]
        [Tooltip("完成任务时向背包发放奖励。")]
        [SerializeField] private InventorySlotsStatsSO QuestRewardRequest;

        private QuestState currentQuestState = QuestState.Idle;
        private readonly Dictionary<QuestSO, QuestProgressData> questProgress = new();
        private bool canvasIsActive;
        private List<QuestSO> currentBoardLoadQuests;
        private QuestSO currentQuest;
        private int dirtyVersion;

        public bool CanvasIsActive => canvasIsActive;

        /// <summary>任务板数据已经加载，允许打开界面。</summary>
        public bool CanOpenBoard => currentBoardLoadQuests != null;

        /// <summary>状态版本号，界面每帧对比它决定是否需要重绘。</summary>
        public int DirtyVersion => dirtyVersion;

        public IReadOnlyList<QuestSO> CurrentBoardQuests => currentBoardLoadQuests;

        /// <summary>当前选中的任务；任务详情面板显示它。</summary>
        public QuestSO CurrentQuest => currentQuest;

        public QuestState CurrentQuestState => currentQuestState;

        /// <summary>单个任务的运行期进度：状态 + 每个目标已完成的数量。</summary>
        private sealed class QuestProgressData
        {
            public QuestProgressData(List<QuestObjective> objectives)
            {
                if (objectives == null) return;
                foreach (var objective in objectives)
                {
                    if (objective != null)
                        questObjectives[objective] = 0;
                }
            }

            public QuestState questState = QuestState.Idle;
            public readonly Dictionary<QuestObjective, int> questObjectives = new();
        }

        private void OnEnable()
        {
            if (openQuestEventSO != null) openQuestEventSO.VoidEvent += OnOpenQuestBoard;
            if (loadQuestEventSO != null) loadQuestEventSO.LoadQuestEvent += OnRefreshQuestState;
            if (questOptionsEventSO != null) questOptionsEventSO.questOptionsEvent += OnQuestOptionChosen;
            if (sceneLoadedEvent != null) sceneLoadedEvent.SceneLoadedEvent += OnSceneLoaded;
        }

        private void OnDisable()
        {
            if (openQuestEventSO != null) openQuestEventSO.VoidEvent -= OnOpenQuestBoard;
            if (loadQuestEventSO != null) loadQuestEventSO.LoadQuestEvent -= OnRefreshQuestState;
            if (questOptionsEventSO != null) questOptionsEventSO.questOptionsEvent -= OnQuestOptionChosen;
            if (sceneLoadedEvent != null) sceneLoadedEvent.SceneLoadedEvent -= OnSceneLoaded;
        }

        /// <summary>切换场景时关闭任务板，避免旧场景的任务板跨场景残留。</summary>
        private void OnSceneLoaded(GameSceneSO _)
        {
            CloseQuestBoard();
        }

        /// <summary>
        /// 玩家选择任务操作。完成操作必须先满足所有目标，否则只记录日志不改变状态。
        /// </summary>
        private void OnQuestOptionChosen(QuestState nextState)
        {
            if (currentQuest == null) return;
            if (nextState == QuestState.Completed && !IsQuestObjDone(currentQuest))
            {
                Debug.Log("Quest Not Done");
                return;
            }

            QuestStateChanged(currentQuest, nextState);
        }

        /// <summary>收到打开任务板请求：数据已加载且界面未打开时才请求显示。</summary>
        private void OnOpenQuestBoard()
        {
            if (currentBoardLoadQuests != null && !canvasIsActive)
                UICacheManager.Instance?.OpenCanvas(CanvasToToggle.Quest);
        }

        /// <summary>关闭任务板并清空当前选择；下次打开需要重新加载任务板数据。</summary>
        public void CloseQuestBoard()
        {
            UICacheManager.Instance?.CloseCanvas(CanvasToToggle.Quest);
            canvasIsActive = false;
            currentBoardLoadQuests = null;
            currentQuest = null;
            MarkDirty();
        }

        /// <summary>判断当前任务板是否就是传入的这批任务，用于避免重复加载。</summary>
        public bool IsDisplayingQuestBoard(List<QuestSO> quests)
        {
            return currentBoardLoadQuests == quests;
        }

        /// <summary>选中某个任务并把它的当前状态同步到界面。</summary>
        public void OpenQuest(QuestSO quest)
        {
            if (quest == null) return;
            currentQuest = quest;
            QuestStateChanged(quest, GetQuestStateFromProgress(quest));
        }

        /// <summary>缓存界面打开后回调：登记激活状态、补全进度并选中第一个未完成任务。</summary>
        public void OnCachedViewOpened()
        {
            if (currentBoardLoadQuests == null) return;

            canvasIsActive = true;
            InitializeQuestProgress(currentBoardLoadQuests);
            if (currentQuest == null)
                currentQuest = GetFirstIncompletedQuest();

            if (currentQuest != null)
            {
                currentQuestState = GetQuestStateFromProgress(currentQuest);
                RefreshObjectiveProgress(currentQuest);
            }
            MarkDirty();
        }

        /// <summary>缓存界面关闭后回调：清空任务板引用，避免继续操作已经关闭的界面。</summary>
        public void OnCachedViewClosed()
        {
            canvasIsActive = false;
            currentBoardLoadQuests = null;
            currentQuest = null;
            MarkDirty();
        }

        /// <summary>
        /// 加载任务板数据：登记进度、把已满足目标的任务推进到“待提交”，
        /// 并刷新所有依赖任务状态的界面。
        /// </summary>
        private void OnRefreshQuestState(List<QuestSO> quests)
        {
            currentBoardLoadQuests = quests;
            InitializeQuestProgress(quests);
            if (quests != null)
            {
                foreach (var quest in quests)
                {
                    if (quest != null && IsQuestObjDone(quest) &&
                        GetQuestStateFromProgress(quest) == QuestState.Accepted)
                        QuestStateChanged(quest, QuestState.IsToComplete);
                }
            }
            MarkDirty();
        }

        /// <summary>向背包系统发放任务奖励；奖励内容由任务配置决定。</summary>
        private void RaiseRewardEvent(QuestSO quest)
        {
            if (quest == null || quest.rewards == null || QuestRewardRequest == null) return;
            foreach (var reward in quest.rewards)
            {
                if (reward?.rewardItem != null)
                    QuestRewardRequest.RaiseInventoryUpdateRequest(reward.rewardItem, 0, reward.quantity);
            }
        }

        /// <summary>为任务板里的每个任务建立进度记录；已经存在的记录保持不变。</summary>
        private void InitializeQuestProgress(List<QuestSO> quests)
        {
            if (quests == null) return;
            foreach (var quest in quests)
            {
                if (quest != null && !questProgress.ContainsKey(quest))
                    questProgress.Add(quest, new QuestProgressData(quest.questObjectives));
            }
        }

        /// <summary>返回任务板中第一个未完成的任务；全部完成时返回 null。</summary>
        public QuestSO GetFirstIncompletedQuest()
        {
            if (currentBoardLoadQuests == null) return null;
            foreach (var quest in currentBoardLoadQuests)
            {
                if (quest != null && questProgress.TryGetValue(quest, out var progress) &&
                    progress.questState != QuestState.Completed)
                    return quest;
            }
            return null;
        }

        /// <summary>读取任务进度状态；没有记录时按未接取处理。</summary>
        public QuestState GetQuestStateFromProgress(QuestSO quest)
        {
            if (quest == null) return QuestState.Idle;
            return questProgress.TryGetValue(quest, out var progress)
                ? progress.questState
                : QuestState.Idle;
        }

        /// <summary>
        /// 改变任务状态。只有从“未完成”首次进入“已完成”时才发奖，
        /// 防止重复提交或界面刷新导致奖励重复发放。
        /// </summary>
        public void QuestStateChanged(QuestSO quest, QuestState state)
        {
            if (quest == null || !questProgress.TryGetValue(quest, out var progress)) return;

            bool isCurrentQuest = currentQuest == quest;
            bool isCompletingNow = state == QuestState.Completed && progress.questState != QuestState.Completed;
            progress.questState = state;
            if (isCurrentQuest)
                currentQuestState = state;

            if (isCompletingNow)
                RaiseRewardEvent(quest);

            RefreshObjectiveProgress(quest);
            MarkDirty();
        }

        /// <summary>
        /// 重算单个目标的进度：物品目标按历史获得数量，角色目标按是否已经对话。
        /// 其他类型的目标保持原值。
        /// </summary>
        public void UpdateObjectiveProgress(QuestSO quest, QuestObjective objective)
        {
            if (objective == null || !questProgress.TryGetValue(quest, out var progress)) return;

            int newAmount = 0;
            if (objective.targetItem != null && ItemHistoryManager.Instance != null)
                newAmount = ItemHistoryManager.Instance.GetItemQuantity(objective.targetItem);
            else if (objective.targetCharacter != null && ConversationHistoryManager.Instance != null &&
                     ConversationHistoryManager.Instance.HasChatedWith(objective.targetCharacter))
                newAmount = objective.requiredAmount;

            progress.questObjectives[objective] = newAmount;
        }

        /// <summary>重算任务的全部目标；已完成的任务保持冻结，避免完成后进度回退。</summary>
        public void RefreshObjectiveProgress(QuestSO quest)
        {
            if (quest == null || !questProgress.ContainsKey(quest) ||
                GetQuestStateFromProgress(quest) == QuestState.Completed)
                return;

            if (quest.questObjectives == null) return;
            foreach (var objective in quest.questObjectives)
                UpdateObjectiveProgress(quest, objective);
        }

        /// <summary>把进度格式化为“当前/需求”文本；已完成时显示对勾。</summary>
        public string GetProgressText(QuestSO quest, QuestObjective objective)
        {
            if (objective == null) return string.Empty;
            int currentAmount = GetCurrentObjAmount(quest, objective);
            if (currentAmount >= objective.requiredAmount) return "\u221A";
            return $"{currentAmount}/{objective.requiredAmount}";
        }

        /// <summary>读取单个目标的当前数量；没有记录时返回 0。</summary>
        public int GetCurrentObjAmount(QuestSO quest, QuestObjective objective)
        {
            if (quest == null || objective == null) return 0;
            return questProgress.TryGetValue(quest, out var progress) &&
                   progress.questObjectives.TryGetValue(objective, out int amount)
                ? amount
                : 0;
        }

        /// <summary>判断一批任务是否全部完成；空列表不算全部完成。</summary>
        public bool IsAllQuestsCompleted(IReadOnlyList<QuestSO> quests)
        {
            if (quests == null || quests.Count == 0) return false;
            foreach (var quest in quests)
            {
                if (GetQuestStateFromProgress(quest) != QuestState.Completed)
                    return false;
            }
            return true;
        }

        /// <summary>递增状态版本，通知视图下一帧重绘。</summary>
        public void MarkDirty()
        {
            dirtyVersion++;
        }

        /// <summary>判断任务的所有目标是否都已完成；已完成任务直接返回 true。</summary>
        private bool IsQuestObjDone(QuestSO quest)
        {
            if (quest == null) return false;
            if (GetQuestStateFromProgress(quest) == QuestState.Completed) return true;
            if (quest.questObjectives == null) return false;

            foreach (var objective in quest.questObjectives)
            {
                if (objective == null) continue;
                if (GetCurrentObjAmount(quest, objective) < objective.requiredAmount)
                    return false;
            }
            return true;
        }
    }
}
