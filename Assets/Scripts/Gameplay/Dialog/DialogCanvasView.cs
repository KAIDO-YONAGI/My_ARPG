using System;
using Contracts.Base;
using Pipeline.SO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Gameplay.Dialog
{
    /// <summary>
    /// 对话面板的显示层。由 UI 缓存按需创建，打开时读取 <see cref="DialogManager"/>
    /// 的当前对话行和选项状态，负责立绘、名字、正文和选项按钮的显示。
    /// </summary>
    /// <remarks>
    /// 选项按钮在 <c>Awake</c> 里一次性注册点击监听，按钮和文本全部来自预制体序列化引用；
    /// 对话数据本身由管理器持有，缓存销毁界面不会丢失剧情进度。
    /// </remarks>
    public sealed class DialogCanvasView : CachedCanvasViewBase
    {
        [Header("对话内容")]
        [Tooltip("说话者立绘。")]
        [SerializeField] private Image speakerPortrait;

        [Tooltip("对话正文。")]
        [SerializeField] private TMP_Text dialogText;

        [Tooltip("说话者名字。")]
        [SerializeField] private TMP_Text speakerNameText;

        [Header("选项按钮")]
        [Tooltip("选项按钮，按界面顺序与对话选项一一对应。")]
        [SerializeField] private Button[] optionButtons = Array.Empty<Button>();

        [Tooltip("选项按钮上的文本，顺序与 optionButtons 一致。")]
        [SerializeField] private TMP_Text[] optionTexts = Array.Empty<TMP_Text>();

        /// <summary>注册到按钮上的监听，销毁时按同一委托移除。</summary>
        private UnityAction[] optionListeners = Array.Empty<UnityAction>();

        private void Awake()
        {
            Debug.Assert(speakerPortrait != null, nameof(speakerPortrait), this);
            Debug.Assert(dialogText != null, nameof(dialogText), this);
            Debug.Assert(speakerNameText != null, nameof(speakerNameText), this);

            optionButtons ??= Array.Empty<Button>();
            optionTexts ??= Array.Empty<TMP_Text>();
            optionListeners = new UnityAction[optionButtons.Length];

            for (int i = 0; i < optionButtons.Length; i++)
                Debug.Assert(optionButtons[i] != null, $"{nameof(optionButtons)}[{i}]", this);
            for (int i = 0; i < optionTexts.Length; i++)
                Debug.Assert(optionTexts[i] != null, $"{nameof(optionTexts)}[{i}]", this);

            // 闭包捕获的是循环变量副本，避免所有按钮都指向最后一个选项。
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int optionIndex = i;
                optionListeners[i] = () => DialogManager.RequiredInstance.SelectOption(optionIndex);
                optionButtons[i].onClick.AddListener(optionListeners[i]);
            }
        }

        /// <summary>没有活动对话时不允许打开，避免出现空白对话框。</summary>
        protected override bool CanOpen()
        {
            return DialogManager.Instance != null && DialogManager.Instance.HasActiveDialog;
        }

        /// <summary>打开后通知管理器，并立即刷新当前行。</summary>
        protected override void OnOpen()
        {
            DialogManager.RequiredInstance.OnCachedViewOpened();
            RefreshIfDirty();
        }

        private void Update()
        {
            RefreshIfDirty();
        }

        /// <summary>
        /// 按脏标记刷新当前对话。管理器在显示一行前会把行号加一，
        /// 所以这里取 <c>CurrentLineIndex - 1</c> 并夹到合法范围。
        /// </summary>
        private void RefreshIfDirty()
        {
            DialogManager manager = DialogManager.Instance;
            if (!IsOpen || manager == null) return;

            DialogManager.State state = manager.CurrentState;
            if (!state.IsDirty) return;

            DialogSO dialog = state.CurrentDialog;
            if (dialog == null || dialog.dialogLines == null || dialog.dialogLines.Length == 0)
            {
                SetChoicesVisible(false);
                state.ClearDirty();
                return;
            }

            int lineIndex = Mathf.Clamp(state.CurrentLineIndex - 1, 0, dialog.dialogLines.Length - 1);
            DialogLine line = dialog.dialogLines[lineIndex];
            if (line.speaker != null)
            {
                speakerPortrait.sprite = line.speaker.characterPortrait;
                speakerNameText.text = line.speaker.characterName;
            }
            dialogText.text = line.text;

            bool showChoices = state.ShowingChoices;
            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool hasOption = showChoices && i < dialog.nextDialogOptions.Length;
                optionButtons[i].gameObject.SetActive(hasOption);
                if (hasOption && i < optionTexts.Length)
                    optionTexts[i].text = dialog.nextDialogOptions[i].optionText;
            }

            state.ClearDirty();
        }

        /// <summary>统一隐藏或显示所有选项按钮。</summary>
        private void SetChoicesVisible(bool visible)
        {
            foreach (Button button in optionButtons)
                button.gameObject.SetActive(visible);
        }

        /// <summary>关闭时通知管理器退出对话状态。</summary>
        protected override void OnClose()
        {
            DialogManager manager = DialogManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("DialogManager 不可用，对话视图无法上报关闭。");
                return;
            }

            manager.OnCachedViewClosed();
        }

        /// <summary>销毁实例时移除按钮监听，避免缓存重建后旧监听残留。</summary>
        private void OnDestroy()
        {
            for (int i = 0; i < optionButtons.Length; i++)
                optionButtons[i].onClick.RemoveListener(optionListeners[i]);
        }
    }
}
