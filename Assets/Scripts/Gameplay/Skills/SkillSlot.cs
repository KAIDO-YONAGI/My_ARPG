using System;
using System.Collections.Generic;
using System.Text;
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
    /// 状态路径以画布根为基准；红点叶子按同一路径索引。
    /// </remarks>
    public class SkillSlot : MonoBehaviour
    {
        [Header("技能配置")]
        /// <summary>槽位对应的技能配置，由预制体逐个拖入。</summary>
        public SkillSO skillSO;

        [Header("控件")]
        [SerializeField] private Image skillIcon;
        [SerializeField] private TMP_Text skillLevelText;
        /// <summary>槽位按钮，点击时尝试升级。</summary>
        public Button skillButton;

        // 技能可升级时显示的红点表现，由预制体按该字段名序列化接线；改名会丢失既有引用。
        [Header("红点")]
        [Tooltip("技能可升级时显示的槽位红点，由技能画布打开时绑定。")]
        [SerializeField] private Image redDotImage;

        [Header("前置技能")]
        [FormerlySerializedAs("preRiquriedForSkillUnlock_List")]
        [SerializeField] private List<SkillSlot> prerequisiteSlots;

        [Header("初始状态")]
        [SerializeField] private int currentLevel;
        public bool isUnlocked = false;

        /// <summary>当前等级，向管理器读取后写入。</summary>
        public int CurrentLevel => currentLevel;
        /// <summary>当前是否已解锁。</summary>
        public bool IsUnlocked => isUnlocked;

        /// <summary>槽位红点表现图片，由技能画布按状态键绑定到红点叶子。</summary>
        public Image RedDotImage => redDotImage;

        /// <summary>技能升满时广播，参数是升满的槽位。</summary>
        public static event Action<SkillSlot> OnMaxSkillLevel;

        /// <summary>状态键的根节点；由技能画布在打开时注入。</summary>
        private Transform stateRoot;

        // 最近一次构建出的状态键；重复刷新与点击升级复用，根节点重新注入时由 BindStateRoot 重建。
        private string stateKey;

        /// <summary>本槽位缓存的状态键；首次读取时按当前根节点构建。</summary>
        public string StateKey => GetStateKey();

        /// <summary>注入状态键根节点，使槽位能生成稳定且不依赖查找的路径。</summary>
        /// <param name="root">画布根节点；状态路径以它为终点。</param>
        public void BindStateRoot(Transform root)
        {
            if (stateRoot == root && stateKey != null) return;

            stateRoot = root;
            stateKey = BuildStateKey(transform, stateRoot);
        }

        /// <summary>校验预制体序列化接线的控件与红点引用，缺引用由断言显式报出。</summary>
        private void Awake()
        {
            Debug.Assert(skillIcon != null, nameof(skillIcon), this);
            Debug.Assert(skillLevelText != null, nameof(skillLevelText), this);
            Debug.Assert(skillButton != null, nameof(skillButton), this);
            Debug.Assert(redDotImage != null, nameof(redDotImage), this);
        }

        /// <summary>编辑器中技能配置变化时刷新显示，便于在预制体上直接查看效果。</summary>
        private void OnValidate()
        {
            if (skillSO != null)
                UpdateUI();
        }

        /// <summary>启用时挂上升级按钮监听，并从管理器恢复本槽位状态。</summary>
        private void OnEnable()
        {
            skillButton.onClick.AddListener(TryUpgradeSkill);
            RefreshFromManager();
        }

        /// <summary>停用时移除升级按钮监听，与 OnEnable 成对管理回调。</summary>
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
        /// 将升级请求交给管理器统一校验、扣点和保存，成功后读取结果刷新显示；
        /// 升到满级时通知画布检查后续技能。
        /// </summary>
        public void TryUpgradeSkill()
        {
            if (skillSO == null) return;
            if (!SkillTreeManager.RequiredInstance.TryUpgradeSkill(GetStateKey(), skillSO.maxLevel,
                    new SkillTreeManager.SkillState(currentLevel, isUnlocked), out var upgraded))
                return;

            RestoreState(upgraded.Level, upgraded.IsUnlocked);
            if (currentLevel >= skillSO.maxLevel)
                OnMaxSkillLevel?.Invoke(this);

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
        /// 读取已缓存的稳定键；首次读取或重新绑定画布根时构建。
        /// 根节点未注入时退回对象名，保证不会因为缺少引用而抛异常。
        /// </summary>
        private string GetStateKey()
        {
            return stateKey ??= BuildStateKey(transform, stateRoot);
        }

        /// <summary>
        /// 从槽位向上走到画布根，记录每级名字与同级序号并拼成路径。
        /// 同级序号区分同名兄弟节点，路径只依赖预制体层级，因此预制体与实例得到同一个键。
        /// 画布根未注入时退回槽位对象名。
        /// </summary>
        /// <param name="slot">槽位自身的节点。</param>
        /// <param name="root">画布根节点；为 null 时退回槽位对象名。</param>
        public static string BuildStateKey(Transform slot, Transform root)
        {
            if (root == null) return slot.name;

            Stack<string> parts = new();
            for (Transform current = slot; current != null && current != root; current = current.parent)
                parts.Push($"{current.name}[{current.GetSiblingIndex()}]");

            StringBuilder key = new();
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
