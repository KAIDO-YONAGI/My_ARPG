using Pipeline.SO;
using TMPro;
using UnityEngine;

namespace Gameplay.Quest
{
    /// <summary>
    /// 任务列表里的单个任务条目：显示任务名与等级，并把点击转给
    /// <see cref="QuestLogPanel"/>。完成后的置灰状态也由本组件维护。
    /// </summary>
    /// <remarks>
    /// 条目本身是 QuestSlot 预制体，CanvasGroup 直接在预制体上拖引用，
    /// 不再在 <c>Awake</c> 或每个方法里用 <c>GetComponent</c> 现取。
    /// </remarks>
    public class QuestLogSlot : MonoBehaviour
    {
        [Header("任务文本")]
        [SerializeField] private TMP_Text questNameText;
        [SerializeField] private TMP_Text questLevelText;

        [Header("状态控制")]
        [Tooltip("条目根节点的 CanvasGroup，用于完成后的置灰和禁用交互。")]
        [SerializeField] private CanvasGroup slotCanvas;

        [Header("引用")]
        [Tooltip("点击条目时通知的任务详情面板。")]
        public QuestLogPanel questLogPanel;

        /// <summary>当前条目绑定的任务；空条目为 null。</summary>
        public QuestSO currentQuest;

        /// <summary>本槽位的 CanvasGroup；保留给外部读取，避免外部再查找组件。</summary>
        public CanvasGroup SlotCanvas => slotCanvas;

        private void Awake()
        {
            ResetSlotState();
        }

        /// <summary>
        /// 编辑器里给条目指定任务时做一次预览刷新。
        /// 这里不调用 SetQuest，避免在编辑模式下反复激活/停用场景对象。
        /// </summary>
        private void OnValidate()
        {
            if (currentQuest == null || questNameText == null || questLevelText == null) return;
            questNameText.text = currentQuest.questName;
            questLevelText.text = "Lv." + currentQuest.lv;
        }

        /// <summary>绑定任务并显示条目；同时把条目的完成态重置为正常。</summary>
        public void SetQuest(QuestSO quest)
        {
            currentQuest = quest;
            if (questNameText != null) questNameText.text = quest.questName;
            if (questLevelText != null) questLevelText.text = "Lv." + quest.lv;
            ResetSlotState();
            gameObject.SetActive(true);
        }

        /// <summary>清空并显示/隐藏条目；隐藏时同时释放当前任务引用。</summary>
        public void SetQuestActive(bool state)
        {
            if (!state)
                currentQuest = null;

            ResetSlotState();
            gameObject.SetActive(state);
        }

        /// <summary>恢复条目的正常显示状态。</summary>
        public void ResetSlotState()
        {
            if (slotCanvas == null) return;
            slotCanvas.alpha = 1f;
            slotCanvas.interactable = true;
            slotCanvas.blocksRaycasts = true;
        }

        /// <summary>把条目置为已完成：降低透明度并禁用点击。</summary>
        public void SetCompletedState()
        {
            if (slotCanvas == null) return;
            slotCanvas.alpha = .2f;
            slotCanvas.interactable = false;
            slotCanvas.blocksRaycasts = false;
        }

        /// <summary>条目点击回调：把当前任务交给详情面板处理。</summary>
        public void OnSlotClicked()
        {
            if (questLogPanel != null && currentQuest != null)
                questLogPanel.HandleQuestClicked(currentQuest);
        }
    }
}
