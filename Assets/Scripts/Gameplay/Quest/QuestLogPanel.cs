using Pipeline.SO;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gameplay.Quest
{
    /// <summary>
    /// 任务详情面板：显示当前任务的名称、描述、目标进度和奖励。
    /// 它是 QuestCanvas 预制体内部的子视图，由 <see cref="QuestCanvasView"/> 驱动刷新。
    /// </summary>
    /// <remarks>
    /// 任务数据来自 <see cref="QuestManager"/>，本组件只负责把数据写到文本和槽位。
    /// 所有控件引用都在预制体上拖好，运行时不查找子物体。
    /// </remarks>
    public class QuestLogPanel : MonoBehaviour
    {
        [Header("任务信息")]
        [SerializeField] private TMP_Text questNameText;
        [SerializeField] private TMP_Text questDescriptionText;

        [Header("任务条目槽位")]
        [Tooltip("任务目标槽位，按任务目标顺序显示。")]
        [SerializeField] private QuestObjectiveSlot[] objectiveSlots;

        [Tooltip("任务奖励槽位，按奖励配置顺序显示。")]
        [FormerlySerializedAs("questRewardsSlot")]
        [SerializeField] private QuestRewardsSlot[] questRewardsSlots;

        // 只记录已经绘制的任务，默认选择由 QuestManager 在界面打开时决定。
        private QuestSO currentQuest;

        private void Awake()
        {
            Debug.Assert(questNameText != null, nameof(questNameText), this);
            Debug.Assert(questDescriptionText != null, nameof(questDescriptionText), this);
            Debug.Assert(objectiveSlots != null, nameof(objectiveSlots), this);
            Debug.Assert(questRewardsSlots != null, nameof(questRewardsSlots), this);

            for (int i = 0; i < objectiveSlots.Length; i++)
                Debug.Assert(objectiveSlots[i] != null, $"{nameof(objectiveSlots)}[{i}]", this);
            for (int i = 0; i < questRewardsSlots.Length; i++)
                Debug.Assert(questRewardsSlots[i] != null, $"{nameof(questRewardsSlots)}[{i}]", this);
        }

        /// <summary>任务条目被点击：向管理器提交选择，由视图读取脏标记后刷新详情。</summary>
        public void HandleQuestClicked(QuestSO quest)
        {
            // currentQuest 记录已经绘制的任务；提前修改会让刷新误判为同一任务。
            QuestManager.RequiredInstance.OpenQuest(quest);
        }

        /// <summary>
        /// 显示任务详情。传入 null 时清空文本并隐藏所有目标、奖励槽位，
        /// 用于“没有任务”或任务板关闭后的收尾。
        /// </summary>
        public void DisplayQuest(QuestSO quest)
        {
            currentQuest = quest;
            if (currentQuest == null)
            {
                questNameText.text = string.Empty;
                questDescriptionText.text = string.Empty;
                SetSlotsActive(objectiveSlots, false);
                SetSlotsActive(questRewardsSlots, false);
                return;
            }

            questNameText.text = currentQuest.questName;
            questDescriptionText.text = currentQuest.questDescription;
            DisplayRewards();
            DisplayObjectives();
        }

        /// <summary>
        /// 管理器状态变化后刷新。任务切换时整页重绘，仍显示同一任务时只刷新目标进度，
        /// 避免每次进度变化都重写名称和描述。
        /// </summary>
        public void RefreshFromManager(QuestSO quest)
        {
            if (currentQuest != quest)
                DisplayQuest(quest);
            else if (currentQuest != null)
                DisplayObjectives();
        }

        /// <summary>按管理器中的当前进度刷新所有目标槽位；多余槽位隐藏。</summary>
        public void DisplayObjectives()
        {
            QuestManager manager = QuestManager.Instance;
            if (currentQuest == null || manager == null) return;

            for (int i = 0; i < objectiveSlots.Length; i++)
            {
                if (i < currentQuest.questObjectives.Count)
                {
                    QuestObjective objective = currentQuest.questObjectives[i];
                    int currentAmount = manager.GetCurrentObjAmount(currentQuest, objective);
                    string progress = manager.GetProgressText(currentQuest, objective);
                    bool isCompleted = currentAmount >= objective.requiredAmount;

                    objectiveSlots[i].gameObject.SetActive(true);
                    objectiveSlots[i].RefreshObjectives(objective.description, progress, isCompleted);
                }
                else
                {
                    objectiveSlots[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>按任务奖励配置刷新奖励槽位；多余槽位隐藏。</summary>
        private void DisplayRewards()
        {
            if (currentQuest == null) return;

            for (int i = 0; i < questRewardsSlots.Length; i++)
            {
                if (i < currentQuest.rewards.Count)
                {
                    Reward reward = currentQuest.rewards[i];
                    questRewardsSlots[i].DisplayReward(reward.rewardItem.icon, reward.quantity);
                    questRewardsSlots[i].gameObject.SetActive(true);
                }
                else
                {
                    questRewardsSlots[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>批量显示或隐藏一组任务槽位。</summary>
        private static void SetSlotsActive(Component[] slots, bool state)
        {
            foreach (Component slot in slots)
                slot.gameObject.SetActive(state);
        }
    }
}
