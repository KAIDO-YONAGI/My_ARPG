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
    /// 界面由 <see cref="QuestCanvasView"/> 读取 <see cref="State"/> 刷新。
    /// </summary>
    /// <remarks>
    /// 管理器不持有任何 UI 控件引用：任务板打开/关闭通过 <see cref="UICacheManager"/> 发请求，
    /// 任务进度保存在这里，所以界面被缓存销毁后不会丢失。
    /// </remarks>
    public class QuestManager : YSingleton<QuestManager>
    {
        /// <summary>任务运行状态：可变化的数值与各自的脏标记放在同一个结构体字段里。</summary>
        public sealed class State
        {
            /// <summary>任务板是否处于打开状态。</summary>
            public DirtyValue<bool> CanvasIsActive;

            /// <summary>当前任务板列表；任务完成态变化时也标脏，以便更新条目与提示。</summary>
            public DirtyValue<List<QuestSO>> BoardQuests;

            /// <summary>当前选中的任务。</summary>
            public DirtyValue<QuestSO> CurrentQuest;

            /// <summary>当前选中任务的状态。</summary>
            public DirtyValue<QuestState> CurrentQuestState;

            /// <summary>任一字段有变化时为 true。</summary>
            public bool AnyDirty =>
                CanvasIsActive.IsDirty || BoardQuests.IsDirty ||
                CurrentQuest.IsDirty || CurrentQuestState.IsDirty;

            /// <summary>把全部字段标记为待刷新。</summary>
            public void MarkAllDirty()
            {
                CanvasIsActive.MarkDirty();
                BoardQuests.MarkDirty();
                CurrentQuest.MarkDirty();
                CurrentQuestState.MarkDirty();
            }

            /// <summary>视图读完后清除全部标记。</summary>
            public void ClearDirty()
            {
                CanvasIsActive.ClearMark();
                BoardQuests.ClearMark();
                CurrentQuest.ClearMark();
                CurrentQuestState.ClearMark();
            }
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

        /// <summary>任务运行状态，供视图读取。</summary>
        public State CurrentState => state;

        public bool CanvasIsActive => state.CanvasIsActive.Value;

        /// <summary>任务板数据已经加载，允许打开界面。</summary>
        public bool CanOpenBoard => state.BoardQuests.Value != null;

        public IReadOnlyList<QuestSO> CurrentBoardQuests => state.BoardQuests.Value;

        /// <summary>当前选中的任务；任务详情面板显示它。</summary>
        public QuestSO CurrentQuest => state.CurrentQuest.Value;

        public QuestState CurrentQuestState => state.CurrentQuestState.Value;

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
            if (state.CurrentQuest.Value == null) return;
            if (nextState == QuestState.Completed && !IsQuestObjDone(state.CurrentQuest.Value))
            {
#if UNITY_EDITOR
                Debug.Log("任务目标未完成。");
#endif
                return;
            }

            QuestStateChanged(state.CurrentQuest.Value, nextState);
        }

        /// <summary>收到打开任务板请求：数据已加载且界面未打开时才请求显示。</summary>
        private void OnOpenQuestBoard()
        {
            if (state.BoardQuests.Value != null && !state.CanvasIsActive.Value)
                UICacheManager.Instance?.OpenCanvas(CanvasToToggle.Quest);
        }

        /// <summary>关闭任务板并清空当前选择；下次打开需要重新加载任务板数据。</summary>
        public void CloseQuestBoard()
        {
            UICacheManager.Instance?.CloseCanvas(CanvasToToggle.Quest);
            // 没有已打开的视图时也要清理已装载的数据；关闭回调已清理时会直接返回。
            OnCachedViewClosed();
        }

        /// <summary>判断当前任务板是否就是传入的这批任务，用于避免重复加载。</summary>
        public bool IsDisplayingQuestBoard(List<QuestSO> quests)
        {
            return state.BoardQuests.Value == quests;
        }

        /// <summary>选中某个任务并把它的当前状态同步到界面。</summary>
        public void OpenQuest(QuestSO quest)
        {
            if (quest == null) return;
            state.CurrentQuest.Value = quest;
            state.CurrentQuestState.Value = GetQuestStateFromProgress(quest);
            RefreshObjectiveProgress(quest);
        }

        /// <summary>缓存界面打开后回调：登记激活状态、补全进度并选中第一个未完成任务。</summary>
        public void OnCachedViewOpened()
        {
            if (state.BoardQuests.Value == null) return;

            state.CanvasIsActive.Value = true;
            InitializeQuestProgress(state.BoardQuests.Value);
            if (state.CurrentQuest.Value == null)
                state.CurrentQuest.Value = GetFirstIncompletedQuest();

            if (state.CurrentQuest.Value != null)
            {
                state.CurrentQuestState.Value = GetQuestStateFromProgress(state.CurrentQuest.Value);
                RefreshObjectiveProgress(state.CurrentQuest.Value);
            }
            MarkDirty();
        }

        /// <summary>缓存界面关闭后回调：清空任务板引用，避免继续操作已经关闭的界面。</summary>
        public void OnCachedViewClosed()
        {
            if (!state.CanvasIsActive.Value && state.BoardQuests.Value == null &&
                state.CurrentQuest.Value == null)
                return;

            state.CanvasIsActive.Value = false;
            state.BoardQuests.Value = null;
            state.CurrentQuest.Value = null;
            state.CurrentQuestState.Value = QuestState.Idle;
        }

        /// <summary>
        /// 加载任务板数据：登记进度、把已满足目标的任务推进到“待提交”，
        /// 并刷新所有依赖任务状态的界面。
        /// </summary>
        private void OnRefreshQuestState(List<QuestSO> quests)
        {
            state.BoardQuests.Value = quests;
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
            List<QuestSO> quests = state.BoardQuests.Value;
            if (quests == null) return null;
            foreach (var quest in quests)
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
        public void QuestStateChanged(QuestSO quest, QuestState nextState)
        {
            if (quest == null || !questProgress.TryGetValue(quest, out var progress)) return;

            bool isCurrentQuest = state.CurrentQuest.Value == quest;
            bool stateChanged = progress.questState != nextState;
            bool isCompletingNow = nextState == QuestState.Completed &&
                                   progress.questState != QuestState.Completed;
            progress.questState = nextState;
            if (isCurrentQuest)
                state.CurrentQuestState.Value = nextState;

            if (isCompletingNow)
                RaiseRewardEvent(quest);

            RefreshObjectiveProgress(quest);
            // 选择任务只影响详情；状态实际变化才需要刷新列表中的完成态。
            if (stateChanged)
                state.BoardQuests.MarkDirty();
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

        /// <summary>把全部状态标记为待刷新。</summary>
        public void MarkDirty()
        {
            state.MarkAllDirty();
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
