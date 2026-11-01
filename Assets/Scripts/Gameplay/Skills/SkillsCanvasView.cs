using Contracts;
using TMPro;
using UnityEngine;

namespace Gameplay.Skills
{
    /// <summary>
    /// 技能画布的显示层。由 UI 缓存按需创建，槽位和技能点文本全部在 SkillsCanvas
    /// 预制体里拖引用，打开时读取 <see cref="SkillTreeManager"/> 的当前状态并刷新。
    /// </summary>
    /// <remarks>
    /// 技能等级与解锁状态保存在管理器里，视图关闭后不保留业务数据；
    /// 重新打开时槽位会向管理器重新拉取状态，因此缓存销毁和重建不会重置技能。
    /// </remarks>
    public sealed class SkillsCanvasView : CachedCanvasView
    {
        [Header("技能控件")]
        [Tooltip("技能槽位，顺序即技能树中的显示顺序。")]
        [SerializeField] private SkillSlot[] slots;

        [Tooltip("技能点数量文本。")]
        [SerializeField] private TMP_Text pointsText;

        private void Awake()
        {
            Debug.Assert(slots != null, nameof(slots), this);
            Debug.Assert(pointsText != null, nameof(pointsText), this);
            for (int i = 0; i < slots.Length; i++)
                Debug.Assert(slots[i] != null, $"{nameof(slots)}[{i}]", this);
        }

        private SkillTreeManager manager;

        /// <summary>
        /// 打开时绑定管理器、把状态路径根节点交给每个槽位，并立即刷新。
        /// 状态路径以画布根为基准，槽位以画布根为基准解析状态路径。
        /// </summary>
        protected override void OnOpen()
        {
            manager = SkillTreeManager.RequiredInstance;
            foreach (var slot in slots)
                slot.BindStateRoot(transform);

            SkillSlot.OnMaxSkillLevel += HandleSkillMaxed;
            manager.CurrentState.MarkDirty();
            RefreshIfDirty();
        }

        private void Update()
        {
            RefreshIfDirty();
        }

        /// <summary>按管理器脏标记刷新技能点文本和所有槽位。</summary>
        private void RefreshIfDirty()
        {
            if (!IsOpen || manager == null) return;

            var state = manager.CurrentState;
            if (!state.AnyDirty) return;

            if (state.IsAbilityPointsDirty)
            {
                pointsText.text = "Skill Points: " + state.AbilityPointsValue;
            }

            if (state.IsSkillsDirty)
            {
                foreach (var slot in slots)
                    slot.RefreshFromManager();
            }

            state.ClearDirty();
        }

        /// <summary>某个技能升满后，检查并解锁满足前置条件的其他技能。</summary>
        private void HandleSkillMaxed(SkillSlot _)
        {
            foreach (var slot in slots)
            {
                if (!slot.IsUnlocked && slot.CanUnlockSkill())
                    slot.Unlock();
            }
        }

        /// <summary>关闭时退订静态事件并断开管理器；槽位和文本引用保留给下次打开复用。</summary>
        protected override void OnClose()
        {
            SkillSlot.OnMaxSkillLevel -= HandleSkillMaxed;
            manager = null;
        }
    }
}
