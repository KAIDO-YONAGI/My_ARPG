using Contracts.Base;
using Contracts.State;
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
    /// 槽位红点由管理器按状态键建立，本视图只在开关时绑定和解绑表现图片。
    /// </remarks>
    public sealed class SkillsCanvasView : CachedCanvasViewBase
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
        private VersionReader skillsReader;
        private VersionReader pointsReader;

        /// <summary>
        /// 打开时绑定管理器、把状态路径根节点交给每个槽位，并立即刷新。
        /// 状态路径以画布根为基准，槽位以画布根为基准解析状态路径；
        /// 随后按槽位状态键取红点叶子并绑定槽位红点图片。
        /// </summary>
        protected override void OnOpen()
        {
            manager = SkillTreeManager.RequiredInstance;
            foreach (SkillSlot slot in slots)
                slot.BindStateRoot(transform);

            // 槽位键以画布根为基准，必须在 BindStateRoot 之后读取；
            // 叶子常驻在管理器里，视图只把本实例的图片挂上去，取不到时跳过。
            foreach (SkillSlot slot in slots)
            {
                SkillRedDotLeaf leaf = manager.GetRedDotLeaf(slot.StateKey);
                if (leaf == null) continue;

                leaf.BindImage(slot.RedDotImage);
            }

            SkillSlot.OnMaxSkillLevel += HandleSkillMaxed;
            skillsReader.Invalidate();
            pointsReader.Invalidate();
            RefreshIfChanged();
        }

        private void LateUpdate()
        {
            RefreshIfChanged();
        }

        /// <summary>按本视图记录的版本分别刷新技能点文本和槽位。</summary>
        private void RefreshIfChanged()
        {
            if (!IsOpen || manager == null) return;

            SkillTreeManager.State state = manager.CurrentState;
            ulong skillsVersion = state.SkillsVersion;
            ulong pointsVersion = state.AbilityPointsVersion;

            if (pointsReader.NeedsRefresh(state, pointsVersion))
            {
                pointsText.text = "Skill Points: " + state.AbilityPointsValue;
                pointsReader.Record(state, pointsVersion);
            }

            if (skillsReader.NeedsRefresh(state, skillsVersion))
            {
                foreach (SkillSlot slot in slots)
                    slot.RefreshFromManager();
                skillsReader.Record(state, skillsVersion);
            }

        }

        /// <summary>某个技能升满后，检查并解锁满足前置条件的其他技能。</summary>
        private void HandleSkillMaxed(SkillSlot _)
        {
            foreach (SkillSlot slot in slots)
            {
                if (!slot.IsUnlocked && slot.CanUnlockSkill())
                    slot.Unlock();
            }
        }

        /// <summary>
        /// 关闭时退订静态事件、按槽位状态键解绑红点表现并断开管理器；
        /// 槽位和文本引用保留给下次打开复用。
        /// </summary>
        protected override void OnClose()
        {
            if (manager != null)
            {
                // 关闭后缓存实例可能复用，解绑避免叶子继续持有已隐藏的图片；
                // 必须传打开时绑定的同一个 Image，UnbindImage 只在参数与当前绑定一致时才清除。
                foreach (SkillSlot slot in slots)
                {
                    SkillRedDotLeaf leaf = manager.GetRedDotLeaf(slot.StateKey);
                    if (leaf == null) continue;

                    leaf.UnbindImage(slot.RedDotImage);
                }
            }

            SkillSlot.OnMaxSkillLevel -= HandleSkillMaxed;
            manager = null;
        }
    }
}
