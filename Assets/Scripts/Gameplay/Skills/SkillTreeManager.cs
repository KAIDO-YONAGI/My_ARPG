using System;
using System.Collections.Generic;
using Contracts.Base;
using Contracts.Enums;
using Contracts.State;
using Gameplay.Player.Services;
using Pipeline.UI.RedDot;
using Pipeline.UI.Runtime;
using UnityEngine;

namespace Gameplay.Skills
{
    /// <summary>
    /// 技能树的持久状态管理器。保存每个技能的等级与解锁状态，并把技能点变化
    /// 通过只读状态查询同步给视图；技能画布上的红点叶子也由这里建立与重算。
    /// </summary>
    /// <remarks>
    /// 技能槽位在界面关闭后会被缓存，但状态不能跟着界面走，所以统一存在这里。
    /// 槽位用稳定的层级路径作为键，缓存重建后仍能找回原状态。
    /// 红点叶子按同一套槽位路径索引，技能状态或技能点实际变化后触发
    /// <see cref="SkillsChanged"/>，由管理器自身重算全部叶子。
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

        // 技能画布预制体上的槽位路径到红点叶子的映射；叶子在预制体就绪后建立一次。
        // 键与技能状态相同，画布实例打开后按同一键取回叶子。
        private readonly Dictionary<string, SkillRedDotLeaf> redDotLeaves = new();

        // 叶子是否已建立；界面缓存与红点管理器都就绪后只建一次，之后每帧只做一次判断。
        private bool redDotLeavesBuilt;

        /// <summary>技能树运行状态，供视图读取。</summary>
        public State CurrentState => state;

        /// <summary>技能等级、解锁状态或技能点实际变化后触发；管理器用它重算全部红点叶子。</summary>
        public event Action SkillsChanged;

        private void OnEnable()
        {
            SkillsChanged += RefreshRedDotLeaves;
            TrySubscribeLevelUp();
        }

        private void OnDisable()
        {
            SkillsChanged -= RefreshRedDotLeaves;
            UnsubscribeLevelUp();
        }

        private void Start()
        {
            // OnEnable 可能早于 StatsService 的 Awake，Start 再补一次订阅。
            TrySubscribeLevelUp();
            RefreshAbilityPoints();
            TryBuildRedDotLeaves();
        }

        // 技能画布预制体与红点管理器可能在 Start 之后才就绪，LateUpdate 继续尝试直到叶子建成。
        private void LateUpdate()
        {
            TryBuildRedDotLeaves();
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

        /// <summary>
        /// 共用升级资格判定：技能已解锁、未满级且当前有可用技能点。
        /// 红点显示与升级命令共用本判定，保证两条路径的规则一致。
        /// </summary>
        /// <param name="key">槽位路径键，与技能状态字典使用同一套键。</param>
        /// <param name="maxLevel">该技能配置的等级上限。</param>
        /// <param name="initialState">没有存档记录时使用的初始等级与解锁状态。</param>
        public bool CanUpgradeSkill(string key, int maxLevel, SkillState initialState)
        {
            if (string.IsNullOrEmpty(key)) return false;

            SkillState current = state.GetSkillState(key, initialState.Level, initialState.IsUnlocked);
            if (!current.IsUnlocked || current.Level >= maxLevel) return false;

            // 数值服务未就绪时按不可升级处理，红点重算不要求它已完成初始化。
            StatsService stats = StatsService.Instance;
            return stats != null && stats.Stats.SkillPoints > 0;
        }

        /// <summary>升级命令统一校验、保存等级并扣除技能点；参数只包含技能键与数值。</summary>
        public bool TryUpgradeSkill(string key, int maxLevel, SkillState initialState,
            out SkillState upgraded)
        {
            upgraded = state.GetSkillState(key, initialState.Level, initialState.IsUnlocked);
            if (!CanUpgradeSkill(key, maxLevel, initialState)) return false;

            StatsService stats = StatsService.RequiredInstance;
            upgraded.Level++;
            SaveSkillState(key, upgraded.Level, upgraded.IsUnlocked);
            stats.UpdateSkillPoints(-1);
            RefreshAbilityPoints();
            return true;
        }

        /// <summary>
        /// 保存技能状态；空键不写入，避免多个槽位互相覆盖。
        /// 内容实际变化时推进版本并广播技能变化。
        /// </summary>
        public void SaveSkillState(string key, int level, bool unlocked)
        {
            if (string.IsNullOrEmpty(key)) return;

            Dictionary<string, SkillState> skills = state.SkillValues;
            if (skills.TryGetValue(key, out SkillState saved)
                && saved.Level == level
                && saved.IsUnlocked == unlocked)
                return;

            skills[key] = new SkillState(level, unlocked);
            state.TouchSkills();
            SkillsChanged?.Invoke();
        }

        /// <summary>调整技能点并刷新界面；数值服务未就绪时忽略。</summary>
        public void UpdateAbilityPoints(int amount)
        {
            if (StatsService.Instance == null) return;
            StatsService.Instance.UpdateSkillPoints(amount);
            RefreshAbilityPoints();
        }

        /// <summary>从数值服务读取当前技能点写入状态；点数实际变化后广播技能变化。</summary>
        private void RefreshAbilityPoints()
        {
            int points = StatsService.Instance != null ? StatsService.Instance.Stats.SkillPoints : 0;
            if (points == state.AbilityPointsValue) return;

            state.SetAbilityPoints(points);
            SkillsChanged?.Invoke();
        }

        /// <summary>按槽位路径取红点叶子；叶子尚未建立或路径没有对应槽位时返回 null。</summary>
        /// <param name="stateKey">槽位路径键，由技能画布槽位按同一套路径生成。</param>
        public SkillRedDotLeaf GetRedDotLeaf(string stateKey)
        {
            return !string.IsNullOrEmpty(stateKey) && redDotLeaves.TryGetValue(stateKey, out SkillRedDotLeaf leaf)
                ? leaf
                : null;
        }

        /// <summary>按当前技能状态与技能点重算全部红点叶子；叶子尚未建立时字典为空。</summary>
        private void RefreshRedDotLeaves()
        {
            foreach (SkillRedDotLeaf leaf in redDotLeaves.Values)
                leaf.Refresh(this);
        }

        /// <summary>
        /// 从技能画布的预制体收集槽位，按槽位路径建立红点叶子并写入初始计数。
        /// 用预制体而不是画布实例：实例要到界面首次打开才创建，菜单红点需要在打开前聚合。
        /// 界面缓存或红点管理器尚未就绪时返回 false，由下一帧继续尝试，不抛异常。
        /// 叶子建成后立即按当前数据重算一次。
        /// </summary>
        private bool TryBuildRedDotLeaves()
        {
            if (redDotLeavesBuilt) return true;

            // 两个单例在各自 Awake 中注册，常驻场景内 Awake 顺序不保证，缺一个就先返回。
            UICacheManager cache = UICacheManager.Instance;
            RedDotManager redDot = RedDotManager.Instance;
            if (cache == null || redDot == null) return false;

            SkillsCanvasView prefab = cache.GetPrefab(CanvasType.Skills) as SkillsCanvasView;
            if (prefab == null) return false;

            Transform root = prefab.transform;
            // 包含未激活的子节点：技能树里部分分支在预制体上默认收起。
            SkillSlot[] prefabSlots = prefab.GetComponentsInChildren<SkillSlot>(true);
            for (int i = 0; i < prefabSlots.Length; i++)
            {
                SkillSlot slot = prefabSlots[i];
                if (slot.skillSO == null) continue;

                // 预制体与实例的层级名、同级序号一致，视图在实例上按同一算法得到同一个键。
                string key = SkillSlot.BuildStateKey(slot.transform, root);
                SkillState initialState = new SkillState(slot.CurrentLevel, slot.IsUnlocked);
                SkillRedDotLeaf leaf = new SkillRedDotLeaf(key, slot.skillSO, initialState);
                int count = CanUpgradeSkill(key, slot.skillSO.maxLevel, initialState) ? 1 : 0;
                if (!redDot.AddLeaf(CanvasType.Skills, leaf, count)) continue;

                redDotLeaves[key] = leaf;
            }

            redDotLeavesBuilt = true;
            // 建立期间技能状态可能已变化，用当前数据覆盖一次预制体初值。
            RefreshRedDotLeaves();
            return true;
        }
    }
}
