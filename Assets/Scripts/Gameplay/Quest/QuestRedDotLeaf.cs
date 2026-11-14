using Pipeline.SO;
using Pipeline.UI.RedDot;

namespace Gameplay.Quest
{
    /// <summary>
    /// 任务红点叶子：只表达已接取任务的可交付待办。
    /// </summary>
    /// <remarks>
    /// 未接取任务即使目标已经满足也不提示，计数只有 0 与 1 两种取值，
    /// 判定统一走 <see cref="QuestManager.IsQuestCompletable"/>。
    /// 叶子随已接取列表建立与移除，事件由 <see cref="RedDotManager"/> 挂接，
    /// 叶子本身不持有界面引用。
    /// </remarks>
    public sealed class QuestRedDotLeaf : RedDotLeafBase
    {
        private readonly QuestSO quest;

        public QuestRedDotLeaf(QuestSO quest)
        {
            this.quest = quest;
        }

        /// <summary>本叶子对应的任务配置。</summary>
        public QuestSO Quest => quest;

        /// <summary>按管理器当前数据重算可交付状态并写入计数。</summary>
        public void Refresh(QuestManager manager)
        {
            SetCount(manager != null && manager.IsQuestCompletable(quest) ? 1 : 0);
        }
    }
}
