using System.Collections.Generic;
using Contracts.Base;
using Contracts.State;
using Gameplay.Player.Services;
using Pipeline.SO;
using UnityEngine;

namespace Gameplay.Skills
{
    /// <summary>
    /// 技能树的持久状态管理器。保存每个技能的等级与解锁状态，并把技能点变化
    /// 通过只读状态查询同步给 <see cref="SkillsCanvasView"/>。
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

        public sealed class State
        {
            /// <summary>按稳定路径保存的技能状态及其脏标记。</summary>
            internal DirtyValue<Dictionary<string, SkillState>> Skills;

            /// <summary>当前可用技能点及其脏标记。</summary>
            internal DirtyValue<int> AbilityPoints;

            /// <summary>当前可用技能点。</summary>
            public int AbilityPointsValue => AbilityPoints.Value;

            /// <summary>技能状态是否需要刷新。</summary>
            public bool IsSkillsDirty => Skills.IsDirty;

            /// <summary>技能点是否需要刷新。</summary>
            public bool IsAbilityPointsDirty => AbilityPoints.IsDirty;

            /// <summary>任一技能字段是否需要刷新。</summary>
            public bool AnyDirty => IsSkillsDirty || IsAbilityPointsDirty;

            public State()
            {
                Skills = new DirtyValue<Dictionary<string, SkillState>>(
                    new Dictionary<string, SkillState>(), true);
                AbilityPoints = new DirtyValue<int>(0, true);
            }

            /// <summary>读取技能状态；没有记录时返回初始等级与解锁状态。</summary>
            public SkillState GetSkillState(string key, int initialLevel, bool initiallyUnlocked)
            {
                return !string.IsNullOrEmpty(key) && Skills.Value.TryGetValue(key, out SkillState saved)
                    ? saved
                    : new SkillState(initialLevel, initiallyUnlocked);
            }

            /// <summary>把技能状态与技能点都标记为待刷新。</summary>
            public void MarkDirty()
            {
                Skills.MarkDirty();
                AbilityPoints.MarkDirty();
            }

            /// <summary>视图刷新完成后清除全部脏标记。</summary>
            public void ClearDirty()
            {
                Skills.ClearMark();
                AbilityPoints.ClearMark();
            }

            /// <summary>值未变但需要重画技能槽位时单独置脏。</summary>
            internal void MarkSkillsDirty() => Skills.MarkDirty();

            /// <summary>写入当前技能点并置脏。</summary>
            internal void SetAbilityPoints(int points) => AbilityPoints.Set(points);

            /// <summary>当前可用技能点。</summary>
            internal int CurrentAbilityPoints => AbilityPoints.Value;

            /// <summary>已保存的技能状态字典。</summary>
            internal Dictionary<string, SkillState> SkillValues => Skills.Value;
        }

        private readonly State state = new();
        private bool listeningToLevelUp;

        /// <summary>技能树运行状态，供视图读取。</summary>
        public State CurrentState => state;

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
            if (state.CurrentAbilityPoints > 0)
                UpdateAbilityPoints(-1);
        }

        /// <summary>
        /// 读取技能状态。没有保存记录时返回槽位在预制体上配置的初始等级与解锁状态。
        /// </summary>
        /// <summary>保存技能状态；空键不写入，避免多个槽位互相覆盖。</summary>
        public void SaveSkillState(string key, int level, bool unlocked)
        {
            if (string.IsNullOrEmpty(key)) return;

            Dictionary<string, SkillState> skills = state.SkillValues;
            skills[key] = new SkillState(level, unlocked);
            state.MarkSkillsDirty();
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
            state.SetAbilityPoints(points);
        }

        /// <summary>从数值服务刷新技能点，并把全部状态标记为待刷新。</summary>
    }
}
