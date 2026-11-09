using System.Collections.Generic;
using Contracts.Base;
using Contracts.Enums;
using Contracts.State;
using Gameplay.Dialog.HistoryManager;
using Pipeline.SO;
using Pipeline.SO.Events;
using Pipeline.UI.Runtime;
using UnityEngine;

namespace Gameplay.Quest
{
    /// <summary>
    /// 任务系统的持久状态管理器。负责任务板加载、任务状态流转、目标进度计算和奖励发放，
    /// 界面由 <see cref="QuestCanvasView"/> 读取只读状态刷新。
    /// </summary>
    /// <remarks>
    /// 管理器不持有任何 UI 控件引用：任务板打开/关闭通过 <see cref="UICacheManager"/> 发请求，
    /// 任务进度保存在这里，所以界面被缓存销毁后不会丢失。
    /// </remarks>
    public class QuestManager : YSingleton<QuestManager>
    {
        public sealed class State
        {
            /// <summary>任务板任务列表及其版本。</summary>
            internal VersionedValue<List<QuestSO>> boardQuests;

            /// <summary>当前选中的任务及其版本。</summary>
            private VersionedValue<QuestSO> currentQuest;

            /// <summary>当前选中任务的状态及其版本。</summary>
            private VersionedValue<QuestState> currentQuestState;

            /// <summary>任务板数据是否已经加载。</summary>
            public bool HasBoardData => boardQuests.Value != null;

            /// <summary>任务板中的任务数量。</summary>
            public int BoardQuestCount => boardQuests.Value?.Count ?? 0;

            /// <summary>按下标读取任务板任务；越界返回 null。</summary>
            public QuestSO GetBoardQuest(int index) =>
                index >= 0 && index < BoardQuestCount ? boardQuests.Value[index] : null;

            /// <summary>当前选中的任务。</summary>
            public QuestSO CurrentQuest => currentQuest.Value;

            /// <summary>当前选中任务的状态。</summary>
            public QuestState CurrentQuestState => currentQuestState.Value;

            /// <summary>任务板列表及其条目状态版本。</summary>
            public ulong BoardQuestsVersion => boardQuests.Version;

            /// <summary>当前选中任务版本。</summary>
            public ulong CurrentQuestVersion => currentQuest.Version;

            /// <summary>当前任务状态版本。</summary>
            public ulong CurrentQuestStateVersion => currentQuestState.Version;

            public State() => boardQuests = new VersionedValue<List<QuestSO>>(null);

            /// <summary>写入任务板列表并推进版本。</summary>
            internal void SetBoardQuests(List<QuestSO> value) => boardQuests.Set(value);

            /// <summary>写入当前任务并推进版本。</summary>
            internal void SetCurrentQuest(QuestSO value) => currentQuest.Set(value);

            /// <summary>写入当前任务状态并推进版本。</summary>
            internal void SetCurrentQuestState(QuestState value) => currentQuestState.Set(value);

            /// <summary>任务条目内容修改后推进列表版本。</summary>
            internal void TouchBoardQuests() => boardQuests.Touch();
        }

        [Header("接收事件")]
        [Tooltip("玩家请求打开任务板。")]
        [SerializeField] private VoidEventSO openQuestEventSO;

        [Tooltip("任务板数据加载完成后刷新任务进度。")]
        [SerializeField] private LoadQuestEventSO loadQuestEventSO;

        [Tooltip("玩家选择接取、放弃或完成任务。")]
        [SerializeField] private QuestOptionsEventSO questOptionsEventSO;

        [Tooltip("场景加载完成后关闭任务板。")]
        [SerializeField] private SceneLoadedEventSO sceneLoadedEvent;

        [Header("触发事件")]
        [Tooltip("完成任务时向背包发放奖励。")]
        [SerializeField] private InventorySlotsStatsSO QuestRewardRequest;

        private readonly Dictionary<QuestSO, QuestProgressData> questProgress = new();
        private readonly State state = new();
        private bool canvasIsActive;

        /// <summary>任务运行状态，供视图读取。</summary>
        public State CurrentState => state;

        /// <summary>任务板数据已经加载，允许打开界面。</summary>
        public bool CanOpenBoard => state.HasBoardData;

        /// <summary>单个任务的运行期进度：状态 + 每个目标已完成的数量。</summary>
        private sealed class QuestProgressData
        {
            public QuestProgressData(List<QuestObjective> objectives)
            {
                if (objectives == null) return;
                foreach (QuestObjective objective in objectives)
                {
                    if (objective != null)
                        questObjectives[objective] = 0;
                }
            }

            public QuestState questState = QuestState.Idle;
            public readonly Dictionary<QuestObjective, int> questObjectives = new();
        }

        protected override void OnSingletonInitialized()
        {
            Debug.Assert(openQuestEventSO != null, nameof(openQuestEventSO), this);
            Debug.Assert(loadQuestEventSO != null, nameof(loadQuestEventSO), this);
            Debug.Assert(questOptionsEventSO != null, nameof(questOptionsEventSO), this);
            Debug.Assert(sceneLoadedEvent != null, nameof(sceneLoadedEvent), this);
            Debug.Assert(QuestRewardRequest != null, nameof(QuestRewardRequest), this);
        }

        private void OnEnable()
        {
            openQuestEventSO.VoidEvent += OnOpenQuestBoard;
            loadQuestEventSO.LoadQuestEvent += OnRefreshQuestState;
            questOptionsEventSO.questOptionsEvent += OnQuestOptionChosen;
            sceneLoadedEvent.SceneLoadedEvent += OnSceneLoaded;
        }

        private void OnDisable()
        {
            openQuestEventSO.VoidEvent -= OnOpenQuestBoard;
            loadQuestEventSO.LoadQuestEvent -= OnRefreshQuestState;
            questOptionsEventSO.questOptionsEvent -= OnQuestOptionChosen;
            sceneLoadedEvent.SceneLoadedEvent -= OnSceneLoaded;
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
            if (state.CurrentQuest == null) return;
            if (nextState == QuestState.Completed && !IsQuestObjDone(state.CurrentQuest))
            {
#if UNITY_EDITOR
                Debug.Log("任务目标未完成。");
#endif
                return;
            }

            QuestStateChanged(state.CurrentQuest, nextState);
        }

        /// <summary>收到打开任务板请求：数据已加载且界面未打开时才请求显示。</summary>
        private void OnOpenQuestBoard()
        {
            if (!state.HasBoardData || canvasIsActive) return;

            UICacheManager cache = UICacheManager.Instance;
            if (cache == null)
            {
                Debug.LogWarning("任务板：UI 缓存不可用，无法打开。");
                return;
            }

            cache.OpenCanvas(CanvasType.Quest);
        }

        /// <summary>关闭任务板并清空当前选择；下次打开需要重新加载任务板数据。</summary>
        public void CloseQuestBoard()
        {
            UICacheManager cache = UICacheManager.Instance;
            if (cache == null)
                Debug.LogWarning("任务板：UI 缓存不可用，无法通过界面关闭。");
            else
                cache.CloseCanvas(CanvasType.Quest);
            // 没有已打开的视图时也要清理已装载的数据；关闭回调已清理时会直接返回。
            OnCachedViewClosed();
        }

        /// <summary>判断当前任务板是否就是传入的这批任务，用于避免重复加载。</summary>
        public bool IsDisplayingQuestBoard(List<QuestSO> quests)
        {
            return state.boardQuests.Value == quests;
        }

        /// <summary>选中某个任务并把它的当前状态同步到界面。</summary>
        public void OpenQuest(QuestSO quest)
        {
            if (quest == null) return;
            state.SetCurrentQuest(quest);
            state.SetCurrentQuestState(GetQuestStateFromProgress(quest));
            RefreshObjectiveProgress(quest);
        }

        /// <summary>缓存界面打开后回调：登记激活状态、补全进度并选中第一个未完成任务。</summary>
        public void OnCachedViewOpened()
        {
            if (!state.HasBoardData) return;

            canvasIsActive = true;
            InitializeQuestProgress(state.boardQuests.Value);
            if (state.CurrentQuest == null)
                state.SetCurrentQuest(GetFirstIncompletedQuest());

            if (state.CurrentQuest != null)
            {
                state.SetCurrentQuestState(GetQuestStateFromProgress(state.CurrentQuest));
                RefreshObjectiveProgress(state.CurrentQuest);
            }
        }

        /// <summary>缓存界面关闭后回调：清空任务板引用，避免继续操作已经关闭的界面。</summary>
        public void OnCachedViewClosed()
        {
            if (!canvasIsActive && !state.HasBoardData && state.CurrentQuest == null)
                return;

            canvasIsActive = false;
            state.SetBoardQuests(null);
            state.SetCurrentQuest(null);
            state.SetCurrentQuestState(QuestState.Idle);
        }

        /// <summary>
        /// 加载任务板数据：登记进度、把已满足目标的任务推进到“待提交”，
        /// 并刷新所有依赖任务状态的界面。
        /// </summary>
        private void OnRefreshQuestState(List<QuestSO> quests)
        {
            state.SetBoardQuests(quests);
            InitializeQuestProgress(quests);
            if (quests != null)
            {
                foreach (QuestSO quest in quests)
                {
                    if (quest != null && IsQuestObjDone(quest) &&
                        GetQuestStateFromProgress(quest) == QuestState.Accepted)
                        QuestStateChanged(quest, QuestState.IsToComplete);
                }
            }
        }

        /// <summary>向背包系统发放任务奖励；奖励内容由任务配置决定。</summary>
        private void RaiseRewardEvent(QuestSO quest)
        {
            if (quest == null || quest.rewards == null) return;
            foreach (Reward reward in quest.rewards)
            {
                if (reward?.rewardItem != null)
                    QuestRewardRequest.RaiseInventoryUpdateRequest(reward.rewardItem, 0, reward.quantity);
            }
        }

        /// <summary>为任务板里的每个任务建立进度记录；已经存在的记录保持不变。</summary>
        private void InitializeQuestProgress(List<QuestSO> quests)
        {
            if (quests == null) return;
            foreach (QuestSO quest in quests)
            {
                if (quest != null && !questProgress.ContainsKey(quest))
                    questProgress.Add(quest, new QuestProgressData(quest.questObjectives));
            }
        }

        /// <summary>返回任务板中第一个未完成的任务；全部完成时返回 null。</summary>
        public QuestSO GetFirstIncompletedQuest()
        {
            if (!state.HasBoardData) return null;
            for (int i = 0; i < state.BoardQuestCount; i++)
            {
                QuestSO quest = state.GetBoardQuest(i);
                if (quest != null && questProgress.TryGetValue(quest, out QuestProgressData progress) &&
                    progress.questState != QuestState.Completed)
                    return quest;
            }
            return null;
        }

        /// <summary>读取任务进度状态；没有记录时按未接取处理。</summary>
        public QuestState GetQuestStateFromProgress(QuestSO quest)
        {
            if (quest == null) return QuestState.Idle;
            return questProgress.TryGetValue(quest, out QuestProgressData progress)
                ? progress.questState
                : QuestState.Idle;
        }

        /// <summary>
        /// 改变任务状态。只有从“未完成”首次进入“已完成”时才发奖，
        /// 防止重复提交或界面刷新导致奖励重复发放。
        /// </summary>
        public void QuestStateChanged(QuestSO quest, QuestState nextState)
        {
            if (quest == null || !questProgress.TryGetValue(quest, out QuestProgressData progress)) return;

            bool isCurrentQuest = state.CurrentQuest == quest;
            bool stateChanged = progress.questState != nextState;
            bool isCompletingNow = nextState == QuestState.Completed &&
                                   progress.questState != QuestState.Completed;
            progress.questState = nextState;
            if (isCurrentQuest)
                state.SetCurrentQuestState(nextState);

            if (isCompletingNow)
                RaiseRewardEvent(quest);

            RefreshObjectiveProgress(quest);
            // 选择任务只影响详情；状态实际变化才需要刷新列表中的完成态。
            if (stateChanged)
                state.TouchBoardQuests();
        }

        /// <summary>
        /// 重算单个目标的进度：物品目标按历史获得数量，角色目标按是否已经对话。
        /// 其他类型的目标保持原值。
        /// </summary>
        public void UpdateObjectiveProgress(QuestSO quest, QuestObjective objective)
        {
            if (objective == null || !questProgress.TryGetValue(quest, out QuestProgressData progress)) return;

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
            foreach (QuestObjective objective in quest.questObjectives)
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
            return questProgress.TryGetValue(quest, out QuestProgressData progress) &&
                   progress.questObjectives.TryGetValue(objective, out int amount)
                ? amount
                : 0;
        }

        /// <summary>判断任务板上的任务是否全部完成；空列表不算全部完成。</summary>
        public bool IsAllQuestsCompleted()
        {
            if (state.BoardQuestCount == 0) return false;
            for (int i = 0; i < state.BoardQuestCount; i++)
            {
                QuestSO quest = state.GetBoardQuest(i);
                if (GetQuestStateFromProgress(quest) != QuestState.Completed)
                    return false;
            }
            return true;
        }

        /// <summary>判断任务的所有目标是否都已完成；已完成任务直接返回 true。</summary>
        private bool IsQuestObjDone(QuestSO quest)
        {
            if (quest == null) return false;
            if (GetQuestStateFromProgress(quest) == QuestState.Completed) return true;
            if (quest.questObjectives == null) return false;

            foreach (QuestObjective objective in quest.questObjectives)
            {
                if (objective == null) continue;
                if (GetCurrentObjAmount(quest, objective) < objective.requiredAmount)
                    return false;
            }
            return true;
        }
    }
}
