using Contracts;
using Pipeline.SO;
using UnityEngine;

namespace Gameplay.Quest
{
    /// <summary>
    /// 任务面板的显示层。由 UI 缓存按需创建，打开时读取 <see cref="QuestManager"/> 的
    /// 脏标记并刷新任务列表、任务详情和接取/放弃/完成按钮。
    /// </summary>
    /// <remarks>
    /// 所有子控件都在 QuestCanvas 预制体里直接拖引用，运行时直接从这些引用读取，且不用
    /// <c>GetComponent</c> 或按名字遍历层级；管理器与视图之间只通过只读状态和脏标记通信。
    /// </remarks>
    public sealed class QuestCanvasView : CachedCanvasView
    {
        [Header("任务列表")]
        [Tooltip("右侧任务详情面板，显示当前任务的目标与奖励。")]
        [SerializeField] private QuestLogPanel questLogPanel;

        [Tooltip("左侧任务条目槽位，顺序与任务板数据一致。")]
        [SerializeField] private QuestLogSlot[] questLogSlots;

        [Header("按钮组显隐")]
        [Tooltip("接取按钮所在的 CanvasGroup。")]
        [SerializeField] private CanvasGroup acceptCanvasGroup;

        [Tooltip("放弃按钮所在的 CanvasGroup。")]
        [SerializeField] private CanvasGroup declineCanvasGroup;

        [Tooltip("完成按钮所在的 CanvasGroup。")]
        [SerializeField] private CanvasGroup completeCanvasGroup;

        [Header("内容区显隐")]
        [Tooltip("有任务时显示的任务详情区域。")]
        [SerializeField] private CanvasGroup detailsCanvasGroup;

        [Tooltip("没有任务时显示的提示文字区域。")]
        [SerializeField] private CanvasGroup promptCanvasGroup;

        /// <summary>只有任务板数据已经加载时才允许打开。</summary>
        protected override bool CanOpen()
        {
            return QuestManager.Instance != null && QuestManager.Instance.CanOpenBoard;
        }

        /// <summary>打开后通知管理器进入面板状态，并立即拉取一次最新数据。</summary>
        protected override void OnOpen()
        {
            QuestManager.Instance?.OnCachedViewOpened();
            RefreshIfDirty();
        }

        private void Update()
        {
            RefreshIfDirty();
        }

        /// <summary>
        /// 按管理器的脏标记刷新界面。任务列表、详情和按钮显隐都从管理器状态推导，
        /// 视图本身不保存任务进度，避免缓存销毁后状态丢失。
        /// </summary>
        private void RefreshIfDirty()
        {
            var manager = QuestManager.Instance;
            if (!IsOpen || manager == null) return;

            var state = manager.CurrentState;
            if (!state.AnyDirty) return;

            var quests = state.BoardQuests.Value;
            int questCount = quests != null ? quests.Count : 0;
            // 列表或任务完成态变化时才填充条目，切换选择只刷新右侧详情。
            if (state.BoardQuests.IsDirty)
            {
                int slotCount = questLogSlots != null ? questLogSlots.Length : 0;
                for (int i = 0; i < slotCount; i++)
                {
                    bool hasQuest = i < questCount && quests[i] != null;
                    if (!hasQuest)
                    {
                        questLogSlots[i].SetQuestActive(false);
                        continue;
                    }

                    QuestSO quest = quests[i];
                    questLogSlots[i].SetQuest(quest);
                    if (manager.GetQuestStateFromProgress(quest) == QuestState.Completed)
                        questLogSlots[i].SetCompletedState();
                }

                bool showNoQuests = questCount == 0 || manager.IsAllQuestsCompleted(quests);
                SetCanvasState(detailsCanvasGroup, !showNoQuests);
                SetCanvasState(promptCanvasGroup, showNoQuests);
            }

            if (state.BoardQuests.IsDirty || state.CurrentQuest.IsDirty || state.CurrentQuestState.IsDirty)
                RefreshDetails(manager);

            state.ClearDirty();
        }

        /// <summary>刷新选中任务的详情和操作按钮；同一任务只重绘目标进度。</summary>
        private void RefreshDetails(QuestManager manager)
        {
            var state = manager.CurrentState;
            var selectedQuest = state.CurrentQuest.Value;
            questLogPanel?.RefreshFromManager(selectedQuest);
            QuestState questState = state.CurrentQuestState.Value;

            // 接取按钮只在任务未接取、或玩家拒绝后重新显示。
            SetCanvasState(acceptCanvasGroup,
                selectedQuest != null && (questState == QuestState.Idle || questState == QuestState.Decline));

            // 放弃按钮在已接取、已拒绝和待提交时都可用。
            SetCanvasState(declineCanvasGroup,
                selectedQuest != null &&
                (questState == QuestState.Accepted || questState == QuestState.Decline ||
                 questState == QuestState.IsToComplete));

            // 完成按钮只在任务进行中或待提交时显示。
            SetCanvasState(completeCanvasGroup,
                selectedQuest != null &&
                (questState == QuestState.Accepted || questState == QuestState.IsToComplete));
        }

        /// <summary>统一设置 CanvasGroup 的透明度、射线阻挡和交互状态。</summary>
        private static void SetCanvasState(CanvasGroup group, bool state)
        {
            if (group == null) return;
            group.alpha = state ? 1 : 0;
            group.blocksRaycasts = state;
            group.interactable = state;
        }

        /// <summary>关闭时通知管理器退出面板状态，下次打开重新拉取数据。</summary>
        protected override void OnClose()
        {
            QuestManager.Instance?.OnCachedViewClosed();
        }
    }
}
