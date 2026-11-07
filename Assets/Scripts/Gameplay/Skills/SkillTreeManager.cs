using System.Collections.Generic;
using Contracts.Base;
using Contracts.State;
using Gameplay.Player.Services;

namespace Gameplay.Skills
{
    /// <summary>
    /// 技能树的持久状态管理器。保存每个技能的等级与解锁状态，并把技能点变化
    /// 通过只读状态查询同步给视图。
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
            /// <summary>按稳定路径保存的技能状态及其版本。</summary>
            internal VersionedValue<Dictionary<string, SkillState>> Skills;

            /// <summary>当前可用技能点及其版本。</summary>
            internal VersionedValue<int> AbilityPoints;

            /// <summary>当前可用技能点。</summary>
            public int AbilityPointsValue => AbilityPoints.Value;

            /// <summary>技能状态版本。</summary>
            public ulong SkillsVersion => Skills.Version;

            /// <summary>技能点版本。</summary>
            public ulong AbilityPointsVersion => AbilityPoints.Version;

            public State()
            {
                Skills = new VersionedValue<Dictionary<string, SkillState>>(
                    new Dictionary<string, SkillState>());
                AbilityPoints = new VersionedValue<int>(0);
            }

            /// <summary>读取技能状态；没有记录时返回初始等级与解锁状态。</summary>
            public SkillState GetSkillState(string key, int initialLevel, bool initiallyUnlocked)
            {
                return !string.IsNullOrEmpty(key) && Skills.Value.TryGetValue(key, out SkillState saved)
                    ? saved
                    : new SkillState(initialLevel, initiallyUnlocked);
            }

            /// <summary>技能集合内容修改后推进版本。</summary>
            internal void TouchSkills() => Skills.Touch();

            /// <summary>写入当前技能点并推进版本。</summary>
            internal void SetAbilityPoints(int points) => AbilityPoints.Set(points);

            /// <summary>已保存的技能状态字典。</summary>
            internal Dictionary<string, SkillState> SkillValues => Skills.Value;
        }

        private readonly State state = new();
        private bool listeningToLevelUp;

        /// <summary>技能树运行状态，供视图读取。</summary>
        public State CurrentState => state;

        private void OnEnable()
        {
            TrySubscribeLevelUp();
        }

        private void OnDisable()
        {
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

        /// <summary>升级命令统一校验、保存等级并扣除技能点；参数只包含技能键与数值。</summary>
        public bool TryUpgradeSkill(string key, int maxLevel, SkillState initialState,
            out SkillState upgraded)
        {
            upgraded = state.GetSkillState(key, initialState.Level, initialState.IsUnlocked);
            if (string.IsNullOrEmpty(key) || !upgraded.IsUnlocked || upgraded.Level >= maxLevel)
                return false;

            StatsService stats = StatsService.RequiredInstance;
            if (stats.Stats.SkillPoints <= 0) return false;

            upgraded.Level++;
            SaveSkillState(key, upgraded.Level, upgraded.IsUnlocked);
            stats.UpdateSkillPoints(-1);
            RefreshAbilityPoints();
            return true;
        }

        /// <summary>保存技能状态；空键不写入，避免多个槽位互相覆盖。</summary>
        public void SaveSkillState(string key, int level, bool unlocked)
        {
            if (string.IsNullOrEmpty(key)) return;

            Dictionary<string, SkillState> skills = state.SkillValues;
            skills[key] = new SkillState(level, unlocked);
            state.TouchSkills();
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

    }
}
