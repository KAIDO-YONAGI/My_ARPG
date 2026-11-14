using Contracts.Base;
using Contracts.Enums;
using Contracts.State;
using Pipeline.SO;
using UnityEngine;

namespace Gameplay.Quest
{
    /// <summary>
    /// 任务画布的显示层。由 UI 缓存按需创建，打开时读取 <see cref="QuestManager"/> 的
    /// 数据版本并刷新任务列表、任务详情和接取/放弃/完成按钮。
    /// </summary>
    /// <remarks>
    /// 显示列表可能是某块任务板的任务，也可能是范围外触发时的已接取任务；
    /// <see cref="QuestManager.State.IsTrackerMode"/> 为真时隐藏三组操作按钮，界面只读。
    /// 所有子控件都在 QuestCanvas 预制体里直接拖引用，运行时直接从这些引用读取，且不用
    /// <c>GetComponent</c> 或按名字遍历层级；视图通过只读状态与版本判断需要绘制的部分。
    /// </remarks>
    public sealed class QuestCanvasView : CachedCanvasViewBase
    {
        [Header("任务列表")]
        [Tooltip("右侧任务详情面板，显示当前任务的目标与奖励。")]
        [SerializeField] private QuestLogPanel questLogPanel;

        [Tooltip("左侧任务条目槽位，顺序与当前显示列表一致。")]
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

        private VersionReader questListReader;
        private VersionReader questReader;
        private VersionReader questStateReader;

        private void Awake()
        {
            Debug.Assert(questLogPanel != null, nameof(questLogPanel), this);
            Debug.Assert(questLogSlots != null, nameof(questLogSlots), this);
            Debug.Assert(acceptCanvasGroup != null, nameof(acceptCanvasGroup), this);
            Debug.Assert(declineCanvasGroup != null, nameof(declineCanvasGroup), this);
            Debug.Assert(completeCanvasGroup != null, nameof(completeCanvasGroup), this);
            Debug.Assert(detailsCanvasGroup != null, nameof(detailsCanvasGroup), this);
            Debug.Assert(promptCanvasGroup != null, nameof(promptCanvasGroup), this);

            for (int i = 0; i < questLogSlots.Length; i++)
                Debug.Assert(questLogSlots[i] != null, $"{nameof(questLogSlots)}[{i}]", this);
        }

        /// <summary>显示列表已经准备时才允许打开。</summary>
        protected override bool CanOpen()
        {
            return QuestManager.Instance != null && QuestManager.Instance.CanOpenQuestCanvas;
        }

        /// <summary>打开后通知管理器进入面板状态，并立即拉取一次最新数据。</summary>
        protected override void OnOpen()
        {
            QuestManager.RequiredInstance.OnCachedViewOpened();
            questListReader.Invalidate();
            questReader.Invalidate();
            questStateReader.Invalidate();
            RefreshIfChanged();
        }

        private void LateUpdate()
        {
            RefreshIfChanged();
        }

        /// <summary>
        /// 按本视图记录的版本刷新界面。任务列表、详情和按钮显隐都从管理器状态推导，
        /// 视图本身不保存任务进度，避免缓存销毁后状态丢失。
        /// </summary>
        private void RefreshIfChanged()
        {
            QuestManager manager = QuestManager.Instance;
            if (!IsOpen || manager == null) return;

            QuestManager.State state = manager.CurrentState;
            ulong questListVersion = state.QuestListVersion;
            ulong questVersion = state.CurrentQuestVersion;
            ulong questStateVersion = state.CurrentQuestStateVersion;
            bool questListChanged = questListReader.NeedsRefresh(state, questListVersion);
            bool questChanged = questReader.NeedsRefresh(state, questVersion);
            bool questStateChanged = questStateReader.NeedsRefresh(state, questStateVersion);
            if (!questListChanged && !questChanged && !questStateChanged) return;

            int questCount = state.QuestCount;
            // 列表或任务完成态变化时才填充条目，切换选择只刷新右侧详情。
            if (questListChanged)
            {
                int slotCount = questLogSlots.Length;
                for (int i = 0; i < slotCount; i++)
                {
                    QuestLogSlot slot = questLogSlots[i];
                    QuestSO quest = i < questCount ? state.GetQuest(i) : null;
                    if (quest == null)
                    {
                        // 空条目在收起前按旧任务解绑红点，避免槽位复用后残留红点。
                        UnbindSlotRedDot(manager, slot);
                        slot.SetQuestActive(false);
                        continue;
                    }

                    // 列表重排后槽位可能改绑其他任务，先解绑旧任务再绑定新叶子。
                    if (slot.currentQuest != quest)
                        UnbindSlotRedDot(manager, slot);

                    slot.SetQuest(quest);
                    BindSlotRedDot(manager, slot, quest);
                    if (manager.GetQuestStateFromProgress(quest) == QuestState.Completed)
                        slot.SetCompletedState();
                }

                bool showNoQuests = questCount == 0 || manager.IsAllQuestsCompleted();
                SetCanvasState(detailsCanvasGroup, !showNoQuests);
                SetCanvasState(promptCanvasGroup, showNoQuests);
            }

            RefreshDetails(manager, state.CurrentQuest, state.CurrentQuestState);

            questListReader.Record(state, questListVersion);
            questReader.Record(state, questVersion);
            questStateReader.Record(state, questStateVersion);
        }

        /// <summary>把条目的红点绑定到任务当前的叶子；任务没有叶子时红点保持隐藏。</summary>
        private static void BindSlotRedDot(QuestManager manager, QuestLogSlot slot, QuestSO quest)
        {
            QuestRedDotLeaf leaf = manager.GetRedDotLeaf(quest);
            if (leaf == null) return;

            leaf.BindImage(slot.RedDotImage);
        }

        /// <summary>解绑条目旧任务的红点并关闭表现，槽位复用时不残留上一个任务的红点。</summary>
        private static void UnbindSlotRedDot(QuestManager manager, QuestLogSlot slot)
        {
            QuestSO previousQuest = slot.currentQuest;
            if (previousQuest != null)
            {
                QuestRedDotLeaf leaf = manager.GetRedDotLeaf(previousQuest);
                if (leaf != null)
                    leaf.UnbindImage(slot.RedDotImage);
            }

            slot.RedDotImage.enabled = false;
        }

        /// <summary>界面关闭前解绑所有条目的红点；叶子计数继续由管理器保存。</summary>
        private void UnbindAllSlotRedDots(QuestManager manager)
        {
            for (int i = 0; i < questLogSlots.Length; i++)
                UnbindSlotRedDot(manager, questLogSlots[i]);
        }

        /// <summary>刷新选中任务的详情和操作按钮；同一任务只重绘目标进度。</summary>
        private void RefreshDetails(QuestManager manager, QuestSO selectedQuest, QuestState questState)
        {
            questLogPanel.RefreshFromManager(selectedQuest);

            // 已接取任务视图只读，接取、放弃、完成都仍由任务板操作。
            bool canOperate = !manager.CurrentState.IsTrackerMode;

            // 接取按钮只在任务未接取、或玩家拒绝后重新显示。
            SetCanvasState(acceptCanvasGroup, canOperate &&
                selectedQuest != null && (questState == QuestState.Idle || questState == QuestState.Decline));

            // 放弃按钮在已接取、已拒绝和待提交时都可用。
            SetCanvasState(declineCanvasGroup, canOperate &&
                selectedQuest != null &&
                (questState == QuestState.Accepted || questState == QuestState.Decline ||
                 questState == QuestState.IsToComplete));

            // 完成按钮只在任务进行中或待提交时显示。
            SetCanvasState(completeCanvasGroup, canOperate &&
                selectedQuest != null &&
                (questState == QuestState.Accepted || questState == QuestState.IsToComplete));
        }

        /// <summary>统一设置 CanvasGroup 的透明度、射线阻挡和交互状态。</summary>
        private static void SetCanvasState(CanvasGroup group, bool state)
        {
            group.alpha = state ? 1 : 0;
            group.blocksRaycasts = state;
            group.interactable = state;
        }

        /// <summary>关闭时解绑条目红点并通知管理器退出面板状态，下次打开重新拉取数据。</summary>
        protected override void OnClose()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("QuestManager 不可用，任务视图无法上报关闭。");
                return;
            }

            UnbindAllSlotRedDots(manager);
            manager.OnCachedViewClosed();
        }
    }
}
