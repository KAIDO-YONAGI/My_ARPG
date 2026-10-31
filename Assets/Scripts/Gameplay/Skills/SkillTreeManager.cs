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

        private readonly Dictionary<string, SkillState> skillState = new();
        private bool listeningToLevelUp;
        private int dirtyVersion;

        /// <summary>状态版本号，界面每帧对比它决定是否需要重绘。</summary>
        public int DirtyVersion => dirtyVersion;

        /// <summary>当前可用技能点；数值服务尚未就绪时返回 0。</summary>
        public int AbilityPoints => StatsService.Instance != null
            ? StatsService.Instance.Stats.SkillPoints
            : 0;

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
            MarkDirty();
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
            if (!string.IsNullOrEmpty(key) && skillState.TryGetValue(key, out var saved))
                return saved;

            return new SkillState(initialLevel, initiallyUnlocked);
        }

        /// <summary>保存技能状态；空键不写入，避免多个槽位互相覆盖。</summary>
        public void SaveSkillState(string key, int level, bool unlocked)
        {
            if (string.IsNullOrEmpty(key)) return;

            skillState[key] = new SkillState(level, unlocked);
            MarkDirty();
        }

        /// <summary>调整技能点并刷新界面；数值服务未就绪时忽略。</summary>
        public void UpdateAbilityPoints(int amount)
        {
            if (StatsService.Instance == null) return;
            StatsService.Instance.UpdateSkillPoints(amount);
            MarkDirty();
        }

        /// <summary>递增状态版本，通知界面下一帧重绘。</summary>
        private void MarkDirty()
        {
            dirtyVersion++;
        }
    }
}
