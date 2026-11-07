using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Contracts.Base;
using Contracts.Enums;
using Gameplay.Inventory;
using Gameplay.Player.Models;
using Gameplay.Player.Services;
using Gameplay.Quest;
using Gameplay.Shop;
using Gameplay.Skills;
using NUnit.Framework;
using Pipeline.SO;
using Pipeline.SO.Events;
using Pipeline.UI.SystemCanvasManagers;
using Pipeline.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Gameplay.Tests
{
    /// <summary>
    /// 缓存界面的业务回归：独立版本读取、背包槽位地址、快捷栏局部刷新与技能命令。
    /// 临时对象只参与本次验证，结束后恢复原单例并销毁全部对象。
    /// </summary>
    public class UICacheRegressionTests
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public |
                                                    BindingFlags.NonPublic;
        private readonly List<Object> temporaryObjects = new();
        private QuestManager originalQuestManager;
        private InventoryManager originalInventoryManager;
        private UIManager originalUIManager;
        private StatsService originalStatsService;
        private SkillTreeManager originalSkillTreeManager;
        private QuestManager questManager;
        private InventoryManager inventoryManager;

        [SetUp]
        public void SetUp()
        {
            originalQuestManager = QuestManager.Instance;
            originalInventoryManager = InventoryManager.Instance;
            originalUIManager = UIManager.Instance;
            originalStatsService = StatsService.Instance;
            originalSkillTreeManager = SkillTreeManager.Instance;
            questManager = CreateComponent<QuestManager>();
            inventoryManager = CreateComponent<InventoryManager>();
            SetSingleton(questManager);
            SetSingleton(inventoryManager);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = temporaryObjects.Count - 1; i >= 0; i--)
                if (temporaryObjects[i] != null) Object.DestroyImmediate(temporaryObjects[i]);
            temporaryObjects.Clear();
            SetSingleton(originalQuestManager);
            SetSingleton(originalInventoryManager);
            SetSingleton(originalUIManager);
            SetSingleton(originalStatsService);
            SetSingleton(originalSkillTreeManager);
        }

        [Test]
        public void CachedCanvasViewBase_OpenAndClose_ReportsThroughCanvasManagerLifecycle()
        {
            var uiManager = CreateComponent<UIManager>();
            var openedEvent = ScriptableObject.CreateInstance<CanvasOpenedEventSO>();
            temporaryObjects.Add(openedEvent);
            SetField(uiManager, "canvasOpenedEvent", openedEvent);
            SetSingleton(uiManager);

            int openedCount = 0;
            openedEvent.CanvasOpened += _ => openedCount++;

            var view = CreateComponent<CachedCanvasViewBase>();
            var group = view.gameObject.AddComponent<CanvasGroup>();
            var canvas = view.gameObject.AddComponent<Canvas>();
            SetField(view, "canvasId", CanvasToToggle.Shop);
            SetField(view, "viewGroup", group);
            SetField(view, "viewCanvas", canvas);

            typeof(CachedUIViewBase).GetMethod("Open", InstanceFlags).Invoke(view, null);

            Assert.AreEqual(1, openedCount);
            var focusStack = (CanvasFocusStack)typeof(UIManager)
                .GetField("focusStack", InstanceFlags).GetValue(uiManager);
            Assert.IsTrue(focusStack.IsCanvasOpen(CanvasToToggle.Shop));
            Assert.AreEqual(1f, group.alpha);
            Assert.AreEqual(focusStack.GetCanvasOrder(CanvasToToggle.Shop, true), canvas.sortingOrder);

            typeof(CachedUIViewBase).GetMethod("Close", InstanceFlags).Invoke(view, null);

            Assert.IsFalse(focusStack.IsCanvasOpen(CanvasToToggle.Shop));
            Assert.AreEqual(0f, group.alpha);
            Assert.AreEqual(focusStack.GetCanvasOrder(CanvasToToggle.Shop, false), canvas.sortingOrder);
        }

        [Test]
        public void QuestClick_RefreshesNameAndDescription()
        {
            QuestSO first = CreateQuest("任务一", "说明一");
            QuestSO second = CreateQuest("任务二", "说明二");
            LoadBoard(first, second);
            var panel = CreateComponent<QuestLogPanel>();
            var title = CreateComponent<TextMeshProUGUI>();
            var description = CreateComponent<TextMeshProUGUI>();
            SetField(panel, "questNameText", title);
            SetField(panel, "questDescriptionText", description);
            SetField(panel, "objectiveSlots", System.Array.Empty<QuestObjectiveSlot>());
            SetField(panel, "questRewardsSlots", System.Array.Empty<QuestRewardsSlot>());
            panel.DisplayQuest(first);

            panel.HandleQuestClicked(second);
            panel.RefreshFromManager(questManager.CurrentState.CurrentQuest);

            Assert.AreEqual("任务二", title.text);
            Assert.AreEqual("说明二", description.text);
        }

        [Test]
        public void QuestSelection_AdvancesSelectionVersionOnly()
        {
            QuestSO quest = CreateQuest("任务", "说明");
            LoadBoard(quest);
            ulong listVersion = questManager.CurrentState.BoardQuestsVersion;
            ulong selectionVersion = questManager.CurrentState.CurrentQuestVersion;

            questManager.OpenQuest(quest);

            Assert.AreNotEqual(selectionVersion, questManager.CurrentState.CurrentQuestVersion);
            Assert.AreEqual(listVersion, questManager.CurrentState.BoardQuestsVersion,
                "切换选择应只刷新详情，避免重建任务列表。");
        }

        [Test]
        public void QuestCompletion_AdvancesListVersion()
        {
            QuestSO quest = CreateQuest("任务", "说明");
            LoadBoard(quest);
            ulong listVersion = questManager.CurrentState.BoardQuestsVersion;

            questManager.QuestStateChanged(quest, QuestState.Completed);

            Assert.AreNotEqual(listVersion, questManager.CurrentState.BoardQuestsVersion,
                "完成任务需要刷新条目完成态和全部完成的提示。");
        }

        [Test]
        public void QuestClose_WithoutViewClearsBoard_RepeatedCloseKeepsVersions()
        {
            LoadBoard(CreateQuest("任务", "说明"));
            LogAssert.Expect(LogType.Warning,
                "任务板：UI 缓存不可用，无法通过界面关闭。");
            questManager.CloseQuestBoard();
            Assert.IsFalse(questManager.CanOpenBoard);
            var state = questManager.CurrentState;
            ulong listVersion = state.BoardQuestsVersion;
            ulong selectionVersion = state.CurrentQuestVersion;
            ulong questStateVersion = state.CurrentQuestStateVersion;

            questManager.OnCachedViewClosed();

            Assert.AreEqual(listVersion, state.BoardQuestsVersion);
            Assert.AreEqual(selectionVersion, state.CurrentQuestVersion);
            Assert.AreEqual(questStateVersion, state.CurrentQuestStateVersion,
                "缓存关闭回调与主动关闭共用清理，已经关闭时无需重复推进版本。");
        }

        [Test]
        public void BackpackOpen_ConfiguresSlotAddresses()
        {
            var view = CreateComponent<BackpackCanvasView>();
            var first = CreateInventorySlot();
            var second = CreateInventorySlot();
            SetField(view, "slots", new[] { first, second });

            Invoke(view, "OnOpen");

            Assert.IsTrue(first.IsBackpackSlot);
            Assert.AreEqual(0, first.SlotIndex);
            Assert.IsTrue(second.IsBackpackSlot);
            Assert.AreEqual(1, second.SlotIndex);
        }

        [Test]
        public void GoldRefresh_PreservesUnchangedHotbarDisplay()
        {
            var view = CreateComponent<HotbarCanvasView>();
            var slot = CreateInventorySlot();
            var item = ScriptableObject.CreateInstance<ItemSO>();
            temporaryObjects.Add(item);
            inventoryManager.InitializeHotbarState(new[] { CreateInitialSlot(item, 1) });
            SetField(view, "hotbarSlots", new[] { slot });
            Invoke(view, "RefreshIfChanged");

            // 显示副本用不同数量充当探针：只更新金币时不应触碰干净的槽位。
            slot.RestoreState(item, 5);
            inventoryManager.UpdateGold(-10);
            ulong goldVersion = inventoryManager.CurrentState.GoldVersion;
            Invoke(view, "RefreshIfChanged");

            Assert.AreEqual(5, slot.Quantity);
            Assert.AreEqual(goldVersion, inventoryManager.CurrentState.GoldVersion,
                "视图读取不应修改管理器版本。");
        }

        [Test]
        public void InventoryState_ProvidesStableReadOnlySlotQueries()
        {
            var item = ScriptableObject.CreateInstance<ItemSO>();
            temporaryObjects.Add(item);
            inventoryManager.InitializeHotbarState(new[] { CreateInitialSlot(item, 1) });

            var state = inventoryManager.CurrentState;
            Assert.AreSame(state, inventoryManager.CurrentState);
            Assert.AreEqual(1, state.HotbarCount);
            Assert.AreEqual(item, state.GetHotbarSlot(0).item);
            Assert.AreEqual(1, state.GetHotbarSlot(0).quantity);
            Assert.Greater(state.HotbarVersion, 0UL);
            Assert.IsNull(typeof(InventoryManager.State).GetProperty("Hotbar"));
            Assert.IsNull(typeof(InventoryManager.State).GetProperty("Gold"));
        }

        [Test]
        public void TwoHotbarViews_ObserveGoldChangesIndependently()
        {
            var first = CreateComponent<HotbarCanvasView>();
            var second = CreateComponent<HotbarCanvasView>();
            var firstText = CreateComponent<TextMeshProUGUI>();
            var secondText = CreateComponent<TextMeshProUGUI>();
            SetField(first, "hotbarSlots", System.Array.Empty<InventorySlot>());
            SetField(second, "hotbarSlots", System.Array.Empty<InventorySlot>());
            SetField(first, "goldAmountText", firstText);
            SetField(second, "goldAmountText", secondText);
            Invoke(first, "RefreshIfChanged");
            Invoke(second, "RefreshIfChanged");

            inventoryManager.UpdateGold(-10);
            ulong version = inventoryManager.CurrentState.GoldVersion;
            Invoke(first, "RefreshIfChanged");
            Assert.AreEqual("10", firstText.text);
            Assert.AreEqual("0", secondText.text);

            Invoke(second, "RefreshIfChanged");
            Assert.AreEqual("10", secondText.text);
            Assert.AreEqual(version, inventoryManager.CurrentState.GoldVersion);
        }

        [Test]
        public void BackpackReopen_RedrawsSameVersionWithoutResettingModel()
        {
            var item = ScriptableObject.CreateInstance<ItemSO>();
            temporaryObjects.Add(item);
            inventoryManager.InitializeBackpackState(new[] { CreateInitialSlot(item, 3) });
            var view = CreateComponent<BackpackCanvasView>();
            var slot = CreateInventorySlot();
            SetField(view, "slots", new[] { slot });
            typeof(CachedUIViewBase).GetProperty("IsOpen", InstanceFlags).SetValue(view, true);
            Invoke(view, "OnOpen");
            Assert.AreEqual(3, slot.Quantity);
            ulong version = inventoryManager.CurrentState.BackpackVersion;

            slot.RestoreState(item, 7);
            Invoke(view, "RefreshIfChanged");
            Assert.AreEqual(7, slot.Quantity, "同版本的常规检查应跳过绘制。");
            Invoke(view, "OnOpen");

            Assert.AreEqual(3, slot.Quantity, "重开必须重绘当前状态。");
            Assert.AreEqual(version, inventoryManager.CurrentState.BackpackVersion);
            Assert.AreEqual(3, inventoryManager.CurrentState.GetBackpackSlot(0).quantity);
        }

        [Test]
        public void InventoryInitialization_CopiesValuesAndRunsOnce()
        {
            var item = ScriptableObject.CreateInstance<ItemSO>();
            temporaryObjects.Add(item);
            var initial = new[] { CreateInitialSlot(item, 2) };
            inventoryManager.InitializeHotbarState(initial);
            inventoryManager.InitializeBackpackState(initial);
            ulong hotbarVersion = inventoryManager.CurrentState.HotbarVersion;
            ulong backpackVersion = inventoryManager.CurrentState.BackpackVersion;

            initial[0] = CreateInitialSlot(item, 99);
            inventoryManager.InitializeHotbarState(initial);
            inventoryManager.InitializeBackpackState(initial);

            Assert.IsTrue(inventoryManager.IsHotbarInitialized);
            Assert.IsTrue(inventoryManager.IsBackpackInitialized);
            Assert.AreEqual(2, inventoryManager.CurrentState.GetHotbarSlot(0).quantity);
            Assert.AreEqual(2, inventoryManager.CurrentState.GetBackpackSlot(0).quantity);
            Assert.AreEqual(hotbarVersion, inventoryManager.CurrentState.HotbarVersion);
            Assert.AreEqual(backpackVersion, inventoryManager.CurrentState.BackpackVersion);
        }

        [Test]
        public void SkillUpgrade_UsesSavedLevelAndSpendsOnePointPerSuccess()
        {
            var manager = CreateSkillManager(2);
            var initial = new SkillTreeManager.SkillState(0, true);

            Assert.IsTrue(manager.TryUpgradeSkill("root/skill", 3, initial, out var first));
            Assert.AreEqual(1, first.Level);
            Assert.AreEqual(1, StatsService.RequiredInstance.Stats.SkillPoints);
            Assert.AreEqual(1, manager.CurrentState.AbilityPointsValue);
            Assert.IsTrue(manager.TryUpgradeSkill("root/skill", 3, initial, out var second));
            Assert.AreEqual(2, second.Level, "重复命令使用管理器保存的等级。");
            Assert.AreEqual(0, StatsService.RequiredInstance.Stats.SkillPoints);
            Assert.AreEqual(2, manager.CurrentState.GetSkillState("root/skill", 0, false).Level);
            ulong skillsVersion = manager.CurrentState.SkillsVersion;
            ulong pointsVersion = manager.CurrentState.AbilityPointsVersion;

            Assert.IsFalse(manager.TryUpgradeSkill("root/skill", 3, initial, out var rejected));
            Assert.AreEqual(2, rejected.Level);
            Assert.AreEqual(skillsVersion, manager.CurrentState.SkillsVersion);
            Assert.AreEqual(pointsVersion, manager.CurrentState.AbilityPointsVersion);
        }

        [TestCase(0, true, 0, 3)]
        [TestCase(2, false, 0, 3)]
        [TestCase(2, true, 3, 3)]
        public void SkillUpgrade_RejectedCommandKeepsStateAndPoints(
            int points, bool unlocked, int level, int maxLevel)
        {
            var manager = CreateSkillManager(points);
            manager.SaveSkillState("root/skill", level, unlocked);
            ulong skillsVersion = manager.CurrentState.SkillsVersion;
            ulong pointsVersion = manager.CurrentState.AbilityPointsVersion;

            Assert.IsFalse(manager.TryUpgradeSkill("root/skill", maxLevel,
                new SkillTreeManager.SkillState(0, true), out var result));

            Assert.AreEqual(level, result.Level);
            Assert.AreEqual(unlocked, result.IsUnlocked);
            Assert.AreEqual(points, StatsService.RequiredInstance.Stats.SkillPoints);
            Assert.AreEqual(skillsVersion, manager.CurrentState.SkillsVersion);
            Assert.AreEqual(pointsVersion, manager.CurrentState.AbilityPointsVersion);
        }

        [Test]
        public void SkillStateKey_ReusesCachedStringAndRebuildsWhenRootChanges()
        {
            var root = CreateComponent<Transform>();
            var branch = CreateComponent<Transform>();
            branch.SetParent(root);
            var slot = CreateComponent<SkillSlot>();
            slot.transform.SetParent(branch);
            slot.BindStateRoot(root);
            var keyMethod = typeof(SkillSlot).GetMethod("GetStateKey", InstanceFlags);
            string first = (string)keyMethod.Invoke(slot, null);

            slot.BindStateRoot(root);
            Assert.AreSame(first, keyMethod.Invoke(slot, null));
            slot.BindStateRoot(branch);
            string rebound = (string)keyMethod.Invoke(slot, null);
            Assert.AreNotEqual(first, rebound);
            Assert.AreSame(rebound, keyMethod.Invoke(slot, null));
        }

        [TestCase("DialogCanvas")]
        [TestCase("QuestCanvas")]
        public void BusinessCanvas_ToggleWaitsForData_StillAcceptsClose(string prefabName)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/UI/Runtime/{prefabName}.prefab");
            var prefab = asset.GetComponent<CachedCanvasViewBase>();
            Assert.IsFalse(prefab.OpenOnToggleEvent);

            var view = CreateComponent<CachedCanvasViewBase>();
            view.gameObject.SetActive(true);
            var cache = new UIViewInstanceCache(
                new[] { new CachedViewBinding { prefab = prefab, anchor = view.gameObject } },
                view.transform, 3, 120f);
            cache.Connect();
            try
            {
                prefab.ToggleEvent.RaiseToggleCanvasEvent(true);
                Assert.AreEqual(0, cache.InstanceCount, "业务数据准备前不应创建界面。");

                // 注入已打开的临时界面，验证关闭路由，无需在编辑模式创建整棵业务预制体。
                typeof(CachedUIViewBase).GetMethod("Open", InstanceFlags).Invoke(view, null);
                var entries = (IDictionary)typeof(UIViewInstanceCache).GetField("entries", InstanceFlags)
                    .GetValue(cache);
                object entry = entries[prefab.CanvasId];
                entry.GetType().GetField("View").SetValue(entry, view);
                Assert.IsTrue(view.IsOpen);

                prefab.ToggleEvent.RaiseToggleCanvasEvent(false);
                Assert.IsFalse(view.IsOpen, "关闭和 ESC 请求必须始终能收起界面。");
            }
            finally
            {
                cache.Disconnect();
            }
        }

        [Test]
        public void ShopOpen_WithoutKeeperDoesNotInstantiateOrRunAwake()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/Runtime/ShopCanvas.prefab").GetComponent<CachedUIViewBase>();
            var originalShopManager = ShopManager.Instance;
            SetSingleton<ShopManager>(null);

            var anchor = new GameObject("ShopCacheAnchor") { hideFlags = HideFlags.HideAndDontSave };
            temporaryObjects.Add(anchor);
            var cache = new UIViewInstanceCache(
                new[] { new CachedViewBinding { prefab = prefab, anchor = anchor } },
                anchor.transform, 3, 120f);

            try
            {
                Assert.IsFalse(cache.OpenCanvas(prefab.CanvasId), "没有店主时商店应拒绝打开。");
                Assert.AreEqual(0, cache.InstanceCount,
                    "CanOpen 拒绝后不应实例化界面，也不应触发 ShopSlot.Awake。");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                cache.Dispose();
                SetSingleton(originalShopManager);
            }
        }

        [Test]
        public void ShopCanvasPrefab_ContainsOnlyConfiguredProductSlots()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/Runtime/ShopCanvas.prefab");
            var slots = (ShopSlot[])typeof(ShopCanvasView)
                .GetField("slots", InstanceFlags).GetValue(prefab.GetComponent<ShopCanvasView>());

            Assert.AreEqual(8, slots.Length, "详情面板的属性文本不能混入商品槽数组。");
            foreach (var slot in slots)
            {
                Assert.IsNotNull(slot);
                Assert.IsNotNull(typeof(ShopSlot).GetField("itemNameText", InstanceFlags).GetValue(slot));
                Assert.IsNotNull(typeof(ShopSlot).GetField("priceText", InstanceFlags).GetValue(slot));
                Assert.IsNotNull(typeof(ShopSlot).GetField("itemImage", InstanceFlags).GetValue(slot));
            }
        }

        /// <summary>直接装载任务板数据，隔离场景触发器和输入事件。</summary>
        private void LoadBoard(params QuestSO[] quests)
        {
            Invoke(questManager, "OnRefreshQuestState", new List<QuestSO>(quests));
        }

        private QuestSO CreateQuest(string title, string description)
        {
            var quest = ScriptableObject.CreateInstance<QuestSO>();
            temporaryObjects.Add(quest);
            quest.questName = title;
            quest.questDescription = description;
            quest.questObjectives = new List<QuestObjective>();
            quest.rewards = new List<Reward>();
            return quest;
        }

        private static InventoryManager.InventorySlotState CreateInitialSlot(ItemSO item, int quantity)
        {
            return new InventoryManager.InventorySlotState(item, quantity);
        }

        private InventorySlot CreateInventorySlot()
        {
            // 从已接好控件的预制体创建，避免 AddComponent 立即触发 OnValidate 时引用未赋值。
            var parent = CreateComponent<Transform>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/Slots/InventorySlot.prefab");
            return Object.Instantiate(prefab, parent).GetComponent<InventorySlot>();
        }

        private SkillTreeManager CreateSkillManager(int points)
        {
            var stats = CreateComponent<StatsService>();
            SetField(stats, "model", new PlayerStatsModel(new PlayerStatsData
            {
                maxHealth = 8, currentHealth = 8, expToUpgrade = 10, expMultiplier = 1f,
                skillPoints = points
            }));
            SetSingleton(stats);
            var manager = CreateComponent<SkillTreeManager>();
            SetSingleton(manager);
            return manager;
        }

        /// <summary>停用临时对象，避免场景生命周期和事件订阅干扰定向调用。</summary>
        private T CreateComponent<T>() where T : Component
        {
            var go = new GameObject(typeof(T).Name) { hideFlags = HideFlags.HideAndDontSave };
            temporaryObjects.Add(go);
            go.SetActive(false);
            return typeof(T) == typeof(Transform) ? (T)(Component)go.transform : go.AddComponent<T>();
        }

        private static void SetSingleton<T>(T instance) where T : MonoBehaviour
        {
            typeof(YSingleton<T>).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)
                .GetSetMethod(true).Invoke(null, new object[] { instance });
        }

        private static void SetField(object target, string field, object value)
        {
            target.GetType().GetField(field, InstanceFlags).SetValue(target, value);
        }

        private static void Invoke(object target, string method, params object[] arguments)
        {
            target.GetType().GetMethod(method, InstanceFlags).Invoke(target, arguments);
        }
    }
}
