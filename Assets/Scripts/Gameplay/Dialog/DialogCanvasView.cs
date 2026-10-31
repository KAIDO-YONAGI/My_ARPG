using System;
using Pipeline.UI.Runtime;
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
    public sealed class DialogCanvasView : CachedCanvasView
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

        /// <summary>上次刷新时的管理器脏版本。</summary>
        private int observedDirtyVersion = -1;

        private void Awake()
        {
            optionButtons ??= Array.Empty<Button>();
            optionTexts ??= Array.Empty<TMP_Text>();
            optionListeners = new UnityAction[optionButtons.Length];

            // 闭包捕获的是循环变量副本，避免所有按钮都指向最后一个选项。
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int optionIndex = i;
                optionListeners[i] = () => DialogManager.Instance?.SelectOption(optionIndex);
                if (optionButtons[i] != null)
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
            DialogManager.Instance?.OnCachedViewOpened();
            observedDirtyVersion = -1;
            RefreshIfDirty();
        }

        private void Update()
        {
            RefreshIfDirty();
        }

        /// <summary>
        /// 按脏版本刷新当前对话。管理器在显示一行前会把行号加一，
        /// 所以这里取 <c>CurrentLineIndex - 1</c> 并夹到合法范围。
        /// </summary>
        private void RefreshIfDirty()
        {
            var manager = DialogManager.Instance;
            if (!IsOpen || manager == null || observedDirtyVersion == manager.DirtyVersion) return;

            var dialog = manager.CurrentDialog;
            if (dialog == null || dialog.dialogLines == null || dialog.dialogLines.Length == 0)
            {
                SetChoicesVisible(false);
                observedDirtyVersion = manager.DirtyVersion;
                return;
            }

            int lineIndex = Mathf.Clamp(manager.CurrentLineIndex - 1, 0, dialog.dialogLines.Length - 1);
            var line = dialog.dialogLines[lineIndex];
            if (speakerPortrait != null && line.speaker != null)
                speakerPortrait.sprite = line.speaker.characterPortrait;
            if (speakerNameText != null && line.speaker != null)
                speakerNameText.text = line.speaker.characterName;
            if (dialogText != null)
                dialogText.text = line.text;

            bool showChoices = manager.IsShowingChoices;
            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool hasOption = showChoices && i < dialog.nextDialogOptions.Length;
                if (optionButtons[i] != null)
                    optionButtons[i].gameObject.SetActive(hasOption);
                if (hasOption && i < optionTexts.Length && optionTexts[i] != null)
                    optionTexts[i].text = dialog.nextDialogOptions[i].optionText;
            }

            observedDirtyVersion = manager.DirtyVersion;
        }

        /// <summary>统一隐藏或显示所有选项按钮。</summary>
        private void SetChoicesVisible(bool visible)
        {
            foreach (var button in optionButtons)
                if (button != null)
                    button.gameObject.SetActive(visible);
        }

        /// <summary>关闭时通知管理器退出对话状态。</summary>
        protected override void OnClose()
        {
            DialogManager.Instance?.OnCachedViewClosed();
        }

        /// <summary>销毁实例时移除按钮监听，避免缓存重建后旧监听残留。</summary>
        private void OnDestroy()
        {
            for (int i = 0; i < optionButtons.Length; i++)
                if (optionButtons[i] != null && optionListeners[i] != null)
                    optionButtons[i].onClick.RemoveListener(optionListeners[i]);
        }
    }
}
