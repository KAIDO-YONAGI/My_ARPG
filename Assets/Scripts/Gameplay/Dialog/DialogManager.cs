using Contracts.Base;
using Contracts.Enums;
using Contracts.State;
using Gameplay.Dialog.HistoryManager;
using Pipeline.SO;
using Pipeline.SO.Events;
using Pipeline.UI.Runtime;
using UnityEngine;

namespace Gameplay.Dialog
{
    /// <summary>
    /// 对话系统的持久状态管理器。负责对话条件判定、拒绝对话、逐行推进和选项分支，
    /// 界面由 <see cref="DialogCanvasView"/> 读取只读状态刷新。
    /// </summary>
    /// <remarks>
    /// 管理器不持有任何 UI 控件引用：打开对话时通过 <see cref="UICacheManager"/> 请求显示画布，
    /// 关闭时同样只发请求。对话进度保存在这里，所以界面被缓存销毁后仍能继续。
    /// </remarks>
    public class DialogManager : YSingleton<DialogManager>
    {
        public sealed class State
        {
            /// <summary>当前已显示到第几行及其版本。</summary>
            private VersionedValue<int> currentLineIndex;

            /// <summary>当前对话资产及其版本。</summary>
            private VersionedValue<DialogSO> currentDialog;

            /// <summary>是否正在显示选项及其版本。</summary>
            private VersionedValue<bool> showingChoices;

            /// <summary>当前已显示到第几行。</summary>
            public int CurrentLineIndex => currentLineIndex.Value;

            /// <summary>当前对话资产。</summary>
            public DialogSO CurrentDialog => currentDialog.Value;

            /// <summary>是否正在显示选项。</summary>
            public bool ShowingChoices => showingChoices.Value;

            public ulong LineVersion => currentLineIndex.Version;
            public ulong DialogVersion => currentDialog.Version;
            public ulong ChoicesVersion => showingChoices.Version;

            /// <summary>写入当前行号并推进版本。</summary>
            internal void SetCurrentLineIndex(int value) => currentLineIndex.Set(value);

            /// <summary>写入当前对话并推进版本。</summary>
            internal void SetCurrentDialog(DialogSO value) => currentDialog.Set(value);

            /// <summary>写入选项显隐并推进版本。</summary>
            internal void SetShowingChoices(bool value) => showingChoices.Set(value);
        }

        [Header("接收事件")]
        [Tooltip("场景加载完成后强制结束当前对话，避免跨场景残留。")]
        [SerializeField] private SceneLoadedEventSO sceneLoadedEvent;

        private readonly State state = new();

        /// <summary>对话画布是否处于打开状态，供外部系统查询。</summary>
        public bool isDialogActive;

        /// <summary>供视图按需读取的同一份只读状态对象。</summary>
        public State CurrentState => state;

        /// <summary>是否有活动对话；没有对话时界面拒绝打开。</summary>
        public bool HasActiveDialog => state.CurrentDialog != null;

        protected override void OnSingletonInitialized()
        {
            Debug.Assert(sceneLoadedEvent != null, nameof(sceneLoadedEvent), this);
        }

        private void OnEnable()
        {
            sceneLoadedEvent.SceneLoadedEvent += OnSceneLoaded;
        }

        private void OnDisable()
        {
            sceneLoadedEvent.SceneLoadedEvent -= OnSceneLoaded;
        }

        private void OnSceneLoaded(GameSceneSO _)
        {
            ForceEndDialog();
        }

        /// <summary>
        /// 兼容旧调用：按状态请求打开或关闭对话画布。
        /// 真正的显隐仍由界面缓存决定，管理器不直接操作 CanvasGroup。
        /// </summary>
        public void SetDialogCanvas(bool state)
        {
            if (state)
            {
                if (this.state.CurrentDialog != null)
                    TryOpenDialogCanvas();
                return;
            }

            CloseDialogCanvas();
        }

        /// <summary>缓存界面打开后回调：标记对话画布已激活。</summary>
        public void OnCachedViewOpened()
        {
            isDialogActive = true;
        }

        /// <summary>缓存界面关闭后回调：清除激活标记。</summary>
        public void OnCachedViewClosed()
        {
            isDialogActive = false;
        }

        /// <summary>兼容旧调用：收起选项按钮。</summary>
        public void DisableButtons()
        {
            if (!state.ShowingChoices) return;
            state.SetShowingChoices(false);
        }

        /// <summary>
        /// 开始一段对话：先做拒绝条件判定，再打开对话画布并显示第一行。
        /// 画布打不开时回滚当前对话，避免留下“有对话但没界面”的状态。
        /// </summary>
        public void StartDialog(DialogSO dialog)
        {
            if (dialog == null || !MatchConditionsToStartDialog(dialog)) return;

            state.SetCurrentDialog(dialog);
            state.SetCurrentLineIndex(0);
            state.SetShowingChoices(false);

            if (!TryOpenDialogCanvas())
            {
                ClearCurrentDialog();
                return;
            }

            ShowDialog();
        }

        /// <summary>
        /// 推进对话：还有下一行就继续显示；已经到结尾且没有选项就结束，
        /// 有选项则切换为选项状态。选项显示期间不响应普通推进。
        /// </summary>
        public void AdvanceDialog()
        {
            if (state.CurrentDialog == null || state.ShowingChoices) return;

            if (state.CurrentLineIndex < state.CurrentDialog.dialogLines.Length)
            {
                ShowDialog();
                return;
            }

            if (state.CurrentDialog.nextDialogOptions.Length == 0)
            {
                EndDialog();
                return;
            }

            ShowChoices();
        }

        /// <summary>选择第 <paramref name="index"/> 个选项；只有选项状态且下标合法时才生效。</summary>
        public void SelectOption(int index)
        {
            if (state.CurrentDialog == null || !state.ShowingChoices ||
                index < 0 || index >= state.CurrentDialog.nextDialogOptions.Length)
                return;

            DialogSO nextDialog = state.CurrentDialog.nextDialogOptions[index].nextDialogNode;
            if (nextDialog != null)
                StartDialog(nextDialog);
            else
                EndDialog();
        }

        /// <summary>
        /// 正常结束对话：记录角色与一次性对话历史，请求关闭画布并清空当前对话。
        /// </summary>
        public void EndDialog()
        {
            DialogSO dialog = state.CurrentDialog;
            if (dialog != null)
            {
                if (ConversationHistoryManager.Instance != null)
                    ConversationHistoryManager.Instance.RecordCharacter(dialog.mainCharacter);

                if (dialog.onlyTriggeredOnce && dialog.parentDialog != null &&
                    ConversationHistoryManager.Instance != null)
                    ConversationHistoryManager.Instance.RecordDialogHasChated(dialog.parentDialog);
            }

            CloseDialogCanvas();
            ClearCurrentDialog();
        }

        /// <summary>强制结束对话：不记录历史，用于切场景或外部中断。</summary>
        public void ForceEndDialog()
        {
            CloseDialogCanvas();
            ClearCurrentDialog();
        }

        /// <summary>兼容旧拼写，内部转到 <see cref="ForceEndDialog"/>。</summary>
        public void ForeceEndDialog()
        {
            ForceEndDialog();
        }

        /// <summary>清空当前对话与行号，并标记界面刷新。</summary>
        private void ClearCurrentDialog()
        {
            state.SetCurrentDialog(null);
            state.SetCurrentLineIndex(0);
            state.SetShowingChoices(false);
        }

        /// <summary>
        /// 判定是否满足开始对话的条件。遍历拒绝对话配置：
        /// 默认聊天按“是否已经聊过”判断，普通拒绝按角色/物品条件是否满足判断。
        /// 命中拒绝时直接进入拒绝对话并返回 false。
        /// </summary>
        private bool MatchConditionsToStartDialog(DialogSO dialog)
        {
            foreach (RefuseDialogSO refuse in dialog.refuseDialogs)
            {
                bool shouldRefuse;
                if (refuse.isDefaultChat)
                {
                    shouldRefuse = ConversationHistoryManager.Instance != null &&
                                   ConversationHistoryManager.Instance.HasDialogChated(dialog);
                }
                else
                {
                    shouldRefuse = !HasRefuseConditions(refuse);
                }

                if (!shouldRefuse) continue;
                StartRefuseDialog(refuse);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 拒绝对话的前置条件是否全部满足。
        /// 角色要求：必须都聊过；物品要求：累计获得数量必须达到配置值。
        /// 两类条件都为空时视为满足。
        /// </summary>
        private bool HasRefuseConditions(RefuseDialogSO refuse)
        {
            if (refuse.requireCharacters != null && refuse.requireCharacters.Count > 0)
            {
                foreach (CharacterSO character in refuse.requireCharacters)
                {
                    if (ConversationHistoryManager.Instance == null ||
                        !ConversationHistoryManager.Instance.HasChatedWith(character))
                        return false;
                }
            }

            if (refuse.requireItems != null && refuse.requireItems.Count > 0)
            {
                foreach (Item item in refuse.requireItems)
                {
                    if (ItemHistoryManager.Instance == null ||
                        !ItemHistoryManager.Instance.HasPickedOverAmount(item.itemSO, item.quantity))
                        return false;
                }
            }

            return true;
        }

        /// <summary>进入拒绝对话：重置行号并打开画布，打不开时回滚。</summary>
        private void StartRefuseDialog(RefuseDialogSO refuse)
        {
            state.SetCurrentDialog(refuse);
            state.SetCurrentLineIndex(0);
            state.SetShowingChoices(false);

            if (!TryOpenDialogCanvas())
            {
                ClearCurrentDialog();
                return;
            }

            ShowDialog();
        }

        /// <summary>切换到选项状态，等待玩家点击。</summary>
        private void ShowChoices()
        {
            state.SetShowingChoices(true);
        }

        /// <summary>
        /// 显示下一行。调用前会先校验行号范围，只有真正取到一行才递增，
        /// 避免在结尾处反复调用导致行号越界。
        /// </summary>
        private void ShowDialog()
        {
            DialogSO dialog = state.CurrentDialog;
            if (dialog == null || dialog.dialogLines.Length == 0)
            {
                EndDialog();
                return;
            }

            if (state.CurrentLineIndex < 0 ||
                state.CurrentLineIndex >= dialog.dialogLines.Length)
                return;

            state.SetCurrentLineIndex(state.CurrentLineIndex + 1);
            state.SetShowingChoices(false);
        }

        /// <summary>请求界面缓存打开对话画布；缓存缺失时返回 false。</summary>
        private static bool TryOpenDialogCanvas()
        {
            UICacheManager cache = UICacheManager.Instance;
            if (cache == null)
            {
                Debug.LogWarning("对话画布：UI 缓存不可用，无法打开。");
                return false;
            }

            return cache.OpenCanvas(CanvasToToggle.Dialog);
        }

        /// <summary>请求界面缓存关闭对话画布；缓存缺失时只记录警告。</summary>
        private static void CloseDialogCanvas()
        {
            UICacheManager cache = UICacheManager.Instance;
            if (cache == null)
            {
                Debug.LogWarning("对话画布：UI 缓存不可用，无法通过界面关闭。");
                return;
            }

            cache.CloseCanvas(CanvasToToggle.Dialog);
        }
    }
}
