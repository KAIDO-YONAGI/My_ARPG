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
    /// 任务系统的持久状态管理器。负责任务板与已接取任务列表的加载、任务状态流转、
    /// 目标进度计算和奖励发放，界面由 <see cref="QuestCanvasView"/> 读取只读状态刷新。
    /// </summary>
    /// <remarks>
    /// 管理器不持有任何 UI 控件引用：画布打开/关闭通过 <see cref="UICacheManager"/> 发请求。
    /// 任务板范围内按 C 沿用任务板的装载与打开流程；范围外由管理器把显示列表切换到已接取任务，
    /// 打开只读的进度视图。任务进度与已接取列表都保存在这里，所以界面被缓存销毁后不会丢失。
    /// </remarks>
    public class QuestManager : YSingleton<QuestManager>
    {
        public sealed class State
        {
            /// <summary>当前任务画布显示的任务列表及其版本；来源是任务板或已接取任务。</summary>
            internal VersionedValue<List<QuestSO>> displayQuests;

            /// <summary>当前选中的任务及其版本。</summary>
            private VersionedValue<QuestSO> currentQuest;

            /// <summary>当前选中任务的状态及其版本。</summary>
            private VersionedValue<QuestState> currentQuestState;

            /// <summary>显示列表是否已经准备。</summary>
            public bool HasQuestList => displayQuests.Value != null;

            /// <summary>显示列表中的任务数量。</summary>
            public int QuestCount => displayQuests.Value?.Count ?? 0;

            /// <summary>按下标读取显示列表中的任务；越界返回 null。</summary>
            public QuestSO GetQuest(int index) =>
                index >= 0 && index < QuestCount ? displayQuests.Value[index] : null;

            /// <summary>当前选中的任务。</summary>
            public QuestSO CurrentQuest => currentQuest.Value;

            /// <summary>当前选中任务的状态。</summary>
            public QuestState CurrentQuestState => currentQuestState.Value;

            /// <summary>显示列表及其条目状态版本。</summary>
            public ulong QuestListVersion => displayQuests.Version;

            /// <summary>当前选中任务版本。</summary>
            public ulong CurrentQuestVersion => currentQuest.Version;

            /// <summary>当前任务状态版本。</summary>
            public ulong CurrentQuestStateVersion => currentQuestState.Version;

            /// <summary>显示列表是否为已接取任务追踪视图。</summary>
            public bool IsTrackerMode => trackerMode;

            private bool trackerMode;

            public State() => displayQuests = new VersionedValue<List<QuestSO>>(null);

            /// <summary>写入显示列表并推进版本。</summary>
            internal void SetDisplayQuests(List<QuestSO> value) => displayQuests.Set(value);

            /// <summary>写入当前任务并推进版本。</summary>
            internal void SetCurrentQuest(QuestSO value) => currentQuest.Set(value);

            /// <summary>写入当前任务状态并推进版本。</summary>
            internal void SetCurrentQuestState(QuestState value) => currentQuestState.Set(value);

            /// <summary>任务条目内容修改后推进列表版本。</summary>
            internal void TouchDisplayQuests() => displayQuests.Touch();

            /// <summary>切换显示列表来源；任务板为 false，已接取任务追踪为 true。</summary>
            internal void SetTrackerMode(bool value) => trackerMode = value;
        }

        [Header("接收事件")]
        [Tooltip("玩家请求打开任务板。")]
        [SerializeField] private VoidEventSO openQuestEventSO;

        [Tooltip("任务板数据加载完成后刷新任务进度。")]
        [SerializeField] private LoadQuestEventSO loadQuestEventSO;

        [Tooltip("玩家选择接取、放弃或完成任务。")]
        [SerializeField] private QuestOptionsEventSO questOptionsEventSO;

        [Tooltip("场景加载完成后关闭任务画布。")]
        [SerializeField] private SceneLoadedEventSO sceneLoadedEvent;

        [Header("触发事件")]
        [Tooltip("完成任务时向背包发放奖励。")]
        [SerializeField] private InventorySlotsStatsSO QuestRewardRequest;

        private readonly Dictionary<QuestSO, QuestProgressData> questProgress = new();
        private readonly List<QuestSO> trackedQuests = new();
        private readonly State state = new();
        private ToggleCanvasEventSO questToggleEvent;
        private bool listeningToQuestToggle;
        private bool canvasIsActive;

        /// <summary>任务运行状态，供视图读取。</summary>
        public State CurrentState => state;

        /// <summary>任务画布显示数据已经准备，允许打开界面。</summary>
        public bool CanOpenQuestCanvas => state.HasQuestList;

        /// <summary>任务画布当前是否由管理器记为已打开。</summary>
        public bool IsCanvasActive => canvasIsActive;

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
            TrySubscribeQuestToggle();
        }

        private void OnDisable()
        {
            openQuestEventSO.VoidEvent -= OnOpenQuestBoard;
            loadQuestEventSO.LoadQuestEvent -= OnRefreshQuestState;
            questOptionsEventSO.questOptionsEvent -= OnQuestOptionChosen;
            sceneLoadedEvent.SceneLoadedEvent -= OnSceneLoaded;
            UnsubscribeQuestToggle();
        }

        private void Start()
        {
            // OnEnable 可能早于 UICacheManager 的 Awake，Start 再补一次订阅。
            TrySubscribeQuestToggle();
            Debug.Assert(listeningToQuestToggle, "任务画布开关事件未登记到 UICacheManager。", this);
        }

        /// <summary>订阅任务画布开关；缓存服务未就绪时直接返回，由 Start 补试。</summary>
        private void TrySubscribeQuestToggle()
        {
            if (listeningToQuestToggle) return;

            if (questToggleEvent == null)
            {
                UICacheManager cache = UICacheManager.Instance;
                if (cache == null) return;
                questToggleEvent = cache.GetToggleEvent(CanvasType.Quest);
                if (questToggleEvent == null) return;
            }

            questToggleEvent.toggleCanvasEvent += OnToggleQuestCanvas;
            listeningToQuestToggle = true;
        }

        /// <summary>退订任务画布开关；对同一事件资产解绑，避免事件通道残留。</summary>
        private void UnsubscribeQuestToggle()
        {
            if (!listeningToQuestToggle) return;
            listeningToQuestToggle = false;

            if (questToggleEvent != null)
                questToggleEvent.toggleCanvasEvent -= OnToggleQuestCanvas;
        }

        /// <summary>切换场景时关闭任务画布，避免旧场景的界面跨场景残留。</summary>
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

        /// <summary>
        /// 任务画布开关请求：范围内由任务板装载自己的列表后打开；
        /// 没有任务板覆盖玩家时打开已接取任务的只读进度视图。
        /// </summary>
        private void OnToggleQuestCanvas(bool isOpen)
        {
            if (!isOpen || canvasIsActive) return;

            // 在范围内的任务板会装载自己的任务列表并打开画布。
            if (QuestBoardManager.AnyBoardInRange) return;

            OpenQuestTracker();
        }

        /// <summary>打开已接取任务视图：显示接取中的任务及其当前进度，不提供状态操作。</summary>
        public void OpenQuestTracker()
        {
            if (canvasIsActive) return;

            state.SetTrackerMode(true);
            state.SetDisplayQuests(trackedQuests);
            InitializeQuestProgress(trackedQuests);
            foreach (QuestSO quest in trackedQuests)
                RefreshObjectiveProgress(quest);

            OpenQuestCanvas();
        }

        /// <summary>收到打开任务板请求：显示数据已准备且界面未打开时才请求显示。</summary>
        private void OnOpenQuestBoard()
        {
            if (!state.HasQuestList || canvasIsActive) return;
            OpenQuestCanvas();
        }

        /// <summary>请求缓存打开任务画布。</summary>
        private void OpenQuestCanvas()
        {
            UICacheManager cache = UICacheManager.Instance;
            if (cache == null)
            {
                Debug.LogWarning("任务画布：UI 缓存不可用，无法打开。");
                return;
            }

            cache.OpenCanvas(CanvasType.Quest);
        }

        /// <summary>关闭任务画布并清空当前显示数据；下次打开按触发来源重新准备列表。</summary>
        public void CloseQuestBoard()
        {
            UICacheManager cache = UICacheManager.Instance;
            if (cache == null)
                Debug.LogWarning("任务画布：UI 缓存不可用，无法通过界面关闭。");
            else
                cache.CloseCanvas(CanvasType.Quest);
            // 没有已打开的视图时也要清理已装载的数据；关闭回调已清理时会直接返回。
            OnCachedViewClosed();
        }

        /// <summary>判断当前显示列表是否就是传入的这批任务，用于避免重复加载。</summary>
        public bool IsDisplayingQuestBoard(List<QuestSO> quests)
        {
            return state.displayQuests.Value == quests;
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
            if (!state.HasQuestList) return;

            canvasIsActive = true;
            InitializeQuestProgress(state.displayQuests.Value);
            if (state.CurrentQuest == null)
                state.SetCurrentQuest(GetFirstIncompletedQuest());

            if (state.CurrentQuest != null)
            {
                state.SetCurrentQuestState(GetQuestStateFromProgress(state.CurrentQuest));
                RefreshObjectiveProgress(state.CurrentQuest);
            }
        }

        /// <summary>缓存界面关闭后回调：清空显示列表与追踪模式，避免继续操作已经关闭的界面。</summary>
        public void OnCachedViewClosed()
        {
            if (!canvasIsActive && !state.HasQuestList && state.CurrentQuest == null)
                return;

            canvasIsActive = false;
            state.SetTrackerMode(false);
            state.SetDisplayQuests(null);
            state.SetCurrentQuest(null);
            state.SetCurrentQuestState(QuestState.Idle);
        }

        /// <summary>
        /// 加载任务板数据：登记进度、把已满足目标的任务推进到“待提交”，
        /// 并刷新所有依赖任务状态的界面。
        /// </summary>
        private void OnRefreshQuestState(List<QuestSO> quests)
        {
            state.SetTrackerMode(false);
            state.SetDisplayQuests(quests);
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

        /// <summary>为显示列表里的每个任务建立进度记录；已经存在的记录保持不变。</summary>
        private void InitializeQuestProgress(List<QuestSO> quests)
        {
            if (quests == null) return;
            foreach (QuestSO quest in quests)
            {
                if (quest != null && !questProgress.ContainsKey(quest))
                    questProgress.Add(quest, new QuestProgressData(quest.questObjectives));
            }
        }

        /// <summary>返回显示列表中第一个未完成的任务；全部完成时返回 null。</summary>
        public QuestSO GetFirstIncompletedQuest()
        {
            if (!state.HasQuestList) return null;
            for (int i = 0; i < state.QuestCount; i++)
            {
                QuestSO quest = state.GetQuest(i);
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
            UpdateTrackedQuests(quest, nextState);
            if (isCurrentQuest)
                state.SetCurrentQuestState(nextState);

            if (isCompletingNow)
                RaiseRewardEvent(quest);

            RefreshObjectiveProgress(quest);
            // 选择任务只影响详情；状态实际变化才需要刷新列表中的完成态。
            if (stateChanged)
                state.TouchDisplayQuests();
        }

        /// <summary>
        /// 维护已接取任务列表：接取与待提交进入列表，放弃与完成移出。
        /// 追踪视图正在显示时同步推进显示列表版本。
        /// </summary>
        private void UpdateTrackedQuests(QuestSO quest, QuestState nextState)
        {
            bool changed;
            switch (nextState)
            {
                case QuestState.Accepted:
                case QuestState.IsToComplete:
                    changed = !trackedQuests.Contains(quest);
                    if (changed)
                        trackedQuests.Add(quest);
                    break;
                case QuestState.Decline:
                case QuestState.Completed:
                    changed = trackedQuests.Remove(quest);
                    break;
                default:
                    changed = false;
                    break;
            }

            if (changed && state.IsTrackerMode)
                state.TouchDisplayQuests();
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

        /// <summary>判断显示列表中的任务是否全部完成；空列表不算全部完成。</summary>
        public bool IsAllQuestsCompleted()
        {
            if (state.QuestCount == 0) return false;
            for (int i = 0; i < state.QuestCount; i++)
            {
                QuestSO quest = state.GetQuest(i);
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
