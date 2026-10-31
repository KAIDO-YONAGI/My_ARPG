using TMPro;
using UnityEngine;

namespace Gameplay.Quest
{
    /// <summary>
    /// 任务目标槽位：显示目标描述与进度文本，并按完成态切换颜色。
    /// 属于 QuestLogPanel 的子控件，数据由面板传入。
    /// </summary>
    public class QuestObjectiveSlot : MonoBehaviour
    {
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text trackingText;

        /// <summary>刷新目标显示；已完成的目标统一显示为灰色。</summary>
        public void RefreshObjectives(string description, string progress, bool isCompleted)
        {
            if (objectiveText == null || trackingText == null) return;
            objectiveText.text = description;
            trackingText.text = progress;

            Color color = isCompleted ? Color.gray : Color.white;
            objectiveText.color = color;
            trackingText.color = color;
        }
    }
}
