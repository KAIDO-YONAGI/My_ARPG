using System.Collections.Generic;
using Contracts;
using Gameplay.Player.Services;
using Pipeline.SO;
using UnityEngine;

namespace Gameplay.Skills
{
    /// <summary>
    /// 技能树的持久状态管理器。保存每个技能的等级与解锁状态，并把技能点变化
    /// 同步给 <see cref="SkillsCanvasView"/>。
    /// </summary>
    /// <remarks>
    /// 技能槽位在界面关闭后会被缓存，但状态不能跟着界面走，所以统一存在这里。
    /// 槽位用稳定的层级路径作为键，缓存重建后仍能找回原状态。
    /// </remarks>
    public class SkillTreeManager : YSingleton<SkillTreeManager>
    {
        /// <summary>一个技能槽位的运行期状态。</summary>
        public struct SkillState
        {
            public int Level;
            public bool IsUnlocked;

            public SkillState(int level, bool isUnlocked)
            {
                Level = level;
                IsUnlocked = isUnlocked;
            }
        }

        /// <summary>技能树运行状态：可变化的数值与各自的脏标记放在同一个结构体字段里。</summary>
        public sealed class State
        {
            /// <summary>按稳定路径保存的技能状态。</summary>
            public DirtyValue<Dictionary<string, SkillState>> Skills;

            /// <summary>当前可用技能点。</summary>
            public DirtyValue<int> AbilityPoints;

            public State()
            {
                Skills = new DirtyValue<Dictionary<string, SkillState>>(
                    new Dictionary<string, SkillState>(), true);
                AbilityPoints = new DirtyValue<int>(0, true);
            }

            /// <summary>任一字段有变化时为 true。</summary>
            public bool AnyDirty => Skills.IsDirty || AbilityPoints.IsDirty;

            /// <summary>把全部字段标记为待刷新。</summary>
            public void MarkAllDirty()
            {
                Skills.MarkDirty();
                AbilityPoints.MarkDirty();
            }

            /// <summary>视图读完后清除全部标记。</summary>
            public void ClearDirty()
            {
                Skills.ClearMark();
                AbilityPoints.ClearMark();
            }
        }

        private readonly State state = new();
        private bool listeningToLevelUp;

        /// <summary>技能树运行状态，供视图读取。</summary>
        public State CurrentState => state;

        /// <summary>当前可用技能点；数值服务尚未就绪时返回 0。</summary>
        public int AbilityPoints => state.AbilityPoints.Value;

        private void OnEnable()
        {
            SkillSlot.OnAbilityPointSpent += HandleAbilityPointSpent;
            TrySubscribeLevelUp();
        }

        private void OnDisable()
        {
            SkillSlot.OnAbilityPointSpent -= HandleAbilityPointSpent;
            UnsubscribeLevelUp();
        }

        private void Start()
        {
            // OnEnable 可能早于 StatsService 的 Awake，Start 再补一次订阅。
            TrySubscribeLevelUp();
            RefreshAbilityPoints();
        }

        /// <summary>订阅玩家升级事件；已经订阅或数值服务未就绪时直接返回。</summary>
        private void TrySubscribeLevelUp()
        {
            if (listeningToLevelUp || StatsService.Instance == null) return;

            listeningToLevelUp = true;
            StatsService.Instance.Stats.LevelUp += UpdateAbilityPoints;
        }

        /// <summary>退订升级事件；对同一对象退订，避免静态事件残留。</summary>
        private void UnsubscribeLevelUp()
        {
            if (!listeningToLevelUp) return;
            listeningToLevelUp = false;

            if (StatsService.Instance != null)
                StatsService.Instance.Stats.LevelUp -= UpdateAbilityPoints;
        }

        /// <summary>有技能点被消耗时扣减一点；技能点不足时忽略。</summary>
        private void HandleAbilityPointSpent(SkillSlot _)
        {
            if (AbilityPoints > 0)
                UpdateAbilityPoints(-1);
        }

        /// <summary>
        /// 读取技能状态。没有保存记录时返回槽位在预制体上配置的初始等级与解锁状态。
        /// </summary>
        public SkillState GetSkillState(string key, int initialLevel, bool initiallyUnlocked)
        {
            Dictionary<string, SkillState> skills = state.Skills.Value;
            if (!string.IsNullOrEmpty(key) && skills.TryGetValue(key, out var saved))
                return saved;

            return new SkillState(initialLevel, initiallyUnlocked);
        }

        /// <summary>保存技能状态；空键不写入，避免多个槽位互相覆盖。</summary>
        public void SaveSkillState(string key, int level, bool unlocked)
        {
            if (string.IsNullOrEmpty(key)) return;

            Dictionary<string, SkillState> skills = state.Skills.Value;
            skills[key] = new SkillState(level, unlocked);
            state.Skills.MarkDirty();
        }

        /// <summary>调整技能点并刷新界面；数值服务未就绪时忽略。</summary>
        public void UpdateAbilityPoints(int amount)
        {
            if (StatsService.Instance == null) return;
            StatsService.Instance.UpdateSkillPoints(amount);
            RefreshAbilityPoints();
        }

        /// <summary>从数值服务读取当前技能点并写入状态。</summary>
        private void RefreshAbilityPoints()
        {
            int points = StatsService.Instance != null ? StatsService.Instance.Stats.SkillPoints : 0;
            state.AbilityPoints.Value = points;
        }

        /// <summary>从数值服务刷新技能点，并把全部状态标记为待刷新。</summary>
        public void MarkDirty()
        {
            RefreshAbilityPoints();
            state.MarkAllDirty();
        }
    }
}
