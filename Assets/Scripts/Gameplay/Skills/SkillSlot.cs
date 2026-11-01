using System;
using System.Collections.Generic;
using System.Text;
using Gameplay.Player.Services;
using Pipeline.SO;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Gameplay.Skills
{
    /// <summary>
    /// 单个技能槽位：显示图标、等级和解锁状态，处理升级点击与前置技能检查。
    /// 状态由 <see cref="SkillTreeManager"/> 保存，槽位只负责显示和把操作转给管理器。
    /// </summary>
    /// <remarks>
    /// 槽位是 SkillsCanvas 预制体里的嵌套预制体。状态键以画布根为基准生成，
    /// 根节点由 <see cref="SkillsCanvasView"/> 在打开时通过 <see cref="BindStateRoot"/> 注入，
    /// 状态路径以画布根为基准。
    /// </remarks>
    public class SkillSlot : MonoBehaviour
    {
        [Header("技能配置")]
        public SkillSO skillSO;

        [Header("控件")]
        [SerializeField] private Image skillIcon;
        [SerializeField] private TMP_Text skillLevelText;
        public Button skillButton;

        [Header("前置技能")]
        [FormerlySerializedAs("preRiquriedForSkillUnlock_List")]
        [SerializeField] private List<SkillSlot> prerequisiteSlots;

        [Header("初始状态")]
        [SerializeField] private int currentLevel;
        public bool isUnlocked = false;

        public int CurrentLevel => currentLevel;
        public bool IsUnlocked => isUnlocked;

        /// <summary>技能点被消耗时广播，参数是触发消耗的槽位。</summary>
        public static event Action<SkillSlot> OnAbilityPointSpent;

        /// <summary>技能升满时广播，参数是升满的槽位。</summary>
        public static event Action<SkillSlot> OnMaxSkillLevel;

        /// <summary>状态键的根节点；由技能画布在打开时注入。</summary>
        private Transform stateRoot;

        /// <summary>注入状态键根节点，使槽位能生成稳定且不依赖查找的路径。</summary>
        public void BindStateRoot(Transform root)
        {
            stateRoot = root;
        }

        private void Awake()
        {
            Debug.Assert(skillIcon != null, nameof(skillIcon), this);
            Debug.Assert(skillLevelText != null, nameof(skillLevelText), this);
            Debug.Assert(skillButton != null, nameof(skillButton), this);
        }

        private void OnValidate()
        {
            if (skillSO != null)
                UpdateUI();
        }

        private void OnEnable()
        {
            skillButton.onClick.AddListener(TryUpgradeSkill);
            RefreshFromManager();
        }

        private void OnDisable()
        {
            skillButton.onClick.RemoveListener(TryUpgradeSkill);
        }

        /// <summary>用管理器状态覆盖当前等级与解锁状态，并刷新显示。</summary>
        public void RestoreState(int level, bool unlocked)
        {
            currentLevel = Mathf.Clamp(level, 0, skillSO != null ? skillSO.maxLevel : int.MaxValue);
            isUnlocked = unlocked;
            UpdateUI();
        }

        /// <summary>向管理器拉取本槽位保存的状态；没有记录时保留预制体初始值。</summary>
        public void RefreshFromManager()
        {
            if (skillSO == null || SkillTreeManager.Instance == null) return;
            SkillTreeManager.SkillState state = SkillTreeManager.Instance.CurrentState.GetSkillState(
                GetStateKey(), currentLevel, isUnlocked);
            RestoreState(state.Level, state.IsUnlocked);
        }

        /// <summary>把图标、等级文本、按钮交互和颜色刷新为当前状态。</summary>
        private void UpdateUI()
        {
            if (skillSO == null)
                return;

            skillIcon.sprite = skillSO.skillIcon;
            if (isUnlocked)
            {
                skillButton.interactable = true;
                skillLevelText.text = currentLevel + "/" + skillSO.maxLevel;
                skillIcon.color = Color.white;
            }
            else
            {
                skillButton.interactable = false;
                skillLevelText.text = "Locked";
                skillIcon.color = Color.grey;
            }
        }

        /// <summary>
        /// 尝试升级：需要有点数、技能已解锁且未满级。升级后保存状态并广播技能点消耗；
        /// 升到满级时再广播一次，供画布解锁后续技能。
        /// </summary>
        public void TryUpgradeSkill()
        {
            if (StatsService.Instance == null || StatsService.Instance.Stats.SkillPoints <= 0 ||
                skillSO == null || !isUnlocked || currentLevel >= skillSO.maxLevel)
                return;

            currentLevel++;
            SaveState();

            OnAbilityPointSpent?.Invoke(this);
            if (currentLevel >= skillSO.maxLevel)
                OnMaxSkillLevel?.Invoke(this);

            UpdateUI();
        }

        /// <summary>解锁技能并保存状态；已经解锁时直接返回。</summary>
        public void Unlock()
        {
            if (isUnlocked) return;
            isUnlocked = true;
            SaveState();
            UpdateUI();
        }

        /// <summary>把当前状态写回管理器。</summary>
        private void SaveState()
        {
            SkillTreeManager.RequiredInstance.SaveSkillState(GetStateKey(), currentLevel, isUnlocked);
        }

        /// <summary>
        /// 生成技能状态的稳定键：从槽位向上走到画布根，记录每级名字与同级序号。
        /// 根节点未注入时退回对象名，保证不会因为缺少引用而抛异常。
        /// </summary>
        private string GetStateKey()
        {
            if (stateRoot == null) return name;

            Stack<string> parts = new Stack<string>();
            for (Transform current = transform; current != null && current != stateRoot; current = current.parent)
                parts.Push($"{current.name}[{current.GetSiblingIndex()}]");

            StringBuilder key = new StringBuilder();
            while (parts.Count > 0)
            {
                if (key.Length > 0) key.Append('/');
                key.Append(parts.Pop());
            }
            return key.ToString();
        }

        /// <summary>检查所有前置技能是否已解锁且升满；没有配置前置时视为满足。</summary>
        public bool CanUnlockSkill()
        {
            if (prerequisiteSlots == null) return true;
            foreach (SkillSlot slot in prerequisiteSlots)
            {
                if (slot == null || slot.skillSO == null)
                    return false;
                if (!slot.isUnlocked || slot.currentLevel < slot.skillSO.maxLevel)
                    return false;
            }
            return true;
        }
    }
}
