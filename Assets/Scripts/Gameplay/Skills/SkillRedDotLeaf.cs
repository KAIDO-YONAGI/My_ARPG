using Pipeline.SO;
using Pipeline.UI.RedDot;

namespace Gameplay.Skills
{
    /// <summary>
    /// 技能树的红点叶子。按槽位路径保存技能配置与槽位预制体初始状态，
    /// 用 <see cref="SkillTreeManager"/> 的共用升级资格判定决定是否显示红点。
    /// </summary>
    /// <remarks>
    /// 计数只有 0 和 1：技能已解锁、尚未满级且当前有可用技能点时为 1。
    /// 叶子只读取技能数据，不写状态；技能等级、解锁状态与技能点变化后由管理器统一重算。
    /// </remarks>
    public sealed class SkillRedDotLeaf : RedDotLeafBase
    {
        // 槽位对应的技能配置，来自技能画布预制体上的槽位引用；判定需要它的等级上限。
        private readonly SkillSO skillSO;

        // 槽位在预制体上的初始等级与解锁状态，管理器没有存档记录时作为当前状态使用。
        private readonly SkillTreeManager.SkillState initialState;

        /// <summary>槽位在技能状态字典里的稳定路径键，与技能状态共用同一套键。</summary>
        public string StateKey { get; }

        /// <summary>用槽位路径、技能配置与槽位预制体初始状态建立叶子。</summary>
        /// <param name="stateKey">槽位路径键，与管理器的技能状态共用同一套键。</param>
        /// <param name="skillSO">槽位对应的技能配置，判定需要它的等级上限。</param>
        /// <param name="initialState">没有存档记录时使用的初始等级与解锁状态。</param>
        public SkillRedDotLeaf(string stateKey, SkillSO skillSO, SkillTreeManager.SkillState initialState)
        {
            StateKey = stateKey;
            this.skillSO = skillSO;
            this.initialState = initialState;
        }

        /// <summary>按管理器的共用资格判定重算红点计数：可升级为 1，否则为 0。</summary>
        /// <param name="manager">持有技能状态与技能点的管理器。</param>
        public void Refresh(SkillTreeManager manager)
        {
            SetCount(manager.CanUpgradeSkill(StateKey, skillSO.maxLevel, initialState) ? 1 : 0);
        }
    }
}
