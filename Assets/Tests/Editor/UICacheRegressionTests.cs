using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Contracts;
using Gameplay.Inventory;
using Gameplay.Quest;
using NUnit.Framework;
using Pipeline.SO;
using Pipeline.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay.Tests
{
    /// <summary>
    /// 缓存界面的业务回归：任务选择与脏标记、背包槽位地址、快捷栏局部刷新。
    /// 临时对象只参与本次验证，结束后恢复原单例并销毁全部对象。
    /// </summary>
    public class UICacheRegressionTests
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public |
                                                    BindingFlags.NonPublic;
        private readonly List<Object> temporaryObjects = new();
        private QuestManager originalQuestManager;
        private InventoryManager originalInventoryManager;
        private QuestManager questManager;
        private InventoryManager inventoryManager;

        [SetUp]
        public void SetUp()
        {
            originalQuestManager = QuestManager.Instance;
            originalInventoryManager = InventoryManager.Instance;
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
            panel.DisplayQuest(first);

            panel.HandleQuestClicked(second);
            panel.RefreshFromManager(questManager.CurrentQuest);

            Assert.AreEqual("任务二", title.text);
            Assert.AreEqual("说明二", description.text);
        }

        [Test]
        public void QuestSelection_LeavesListClean()
        {
            QuestSO quest = CreateQuest("任务", "说明");
            LoadBoard(quest);
            questManager.CurrentState.ClearDirty();

            questManager.OpenQuest(quest);

            Assert.IsTrue(questManager.CurrentState.CurrentQuest.IsDirty);
            Assert.IsFalse(questManager.CurrentState.BoardQuests.IsDirty,
                "切换选择应只刷新详情，避免重建任务列表。");
        }

        [Test]
        public void QuestCompletion_MarksListDirty()
        {
            QuestSO quest = CreateQuest("任务", "说明");
            LoadBoard(quest);
            questManager.CurrentState.ClearDirty();

            questManager.QuestStateChanged(quest, QuestState.Completed);

            Assert.IsTrue(questManager.CurrentState.BoardQuests.IsDirty,
                "完成任务需要刷新条目完成态和全部完成的提示。");
        }

        [Test]
        public void QuestClose_WithoutViewClearsBoard_RepeatedCloseStaysClean()
        {
            LoadBoard(CreateQuest("任务", "说明"));
            questManager.CloseQuestBoard();
            Assert.IsNull(questManager.CurrentBoardQuests);
            Assert.IsNull(questManager.CurrentQuest);
            Assert.IsFalse(questManager.CanvasIsActive);
            questManager.CurrentState.ClearDirty();

            questManager.OnCachedViewClosed();

            Assert.IsFalse(questManager.CurrentState.AnyDirty,
                "缓存关闭回调与主动关闭共用清理，已经关闭时无需重复标脏。");
        }

        [Test]
        public void BackpackOpen_ConfiguresSlotAddresses()
        {
            var view = CreateComponent<BackpackCanvasView>();
            var first = CreateComponent<InventorySlot>();
            var second = CreateComponent<InventorySlot>();
            SetField(view, "slots", new[] { first, second });

            Invoke(view, "OnOpen");

            Assert.IsTrue(first.IsBackpackSlot);
            Assert.AreEqual(0, first.SlotIndex);
            Assert.IsTrue(second.IsBackpackSlot);
            Assert.AreEqual(1, second.SlotIndex);
        }

        [Test]
        public void GoldRefresh_PreservesCleanHotbarDisplay()
        {
            var view = CreateComponent<HotbarCanvasView>();
            var slot = CreateComponent<InventorySlot>();
            var item = ScriptableObject.CreateInstance<ItemSO>();
            temporaryObjects.Add(item);
            inventoryManager.InitializeHotbarState(new[] { item }, new[] { 1 });
            SetField(view, "hotbarSlots", new[] { slot });
            Invoke(view, "RefreshIfDirty", true);

            // 显示副本用不同数量充当探针：只更新金币时不应触碰干净的槽位。
            slot.RestoreState(item, 5);
            inventoryManager.CurrentState.Gold.Value = 10;
            Invoke(view, "RefreshIfDirty", false);

            Assert.AreEqual(5, slot.Quantity);
            Assert.IsFalse(inventoryManager.CurrentState.Gold.IsDirty);
        }

        [TestCase("DialogCanvas")]
        [TestCase("QuestCanvas")]
        public void BusinessCanvas_ToggleWaitsForData_StillAcceptsClose(string prefabName)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/UI/Runtime/{prefabName}.prefab");
            var prefab = asset.GetComponent<CachedCanvasView>();
            Assert.IsFalse(prefab.OpenOnToggleEvent);

            var view = CreateComponent<CachedCanvasView>();
            view.gameObject.SetActive(true);
            var cache = new UIViewCache(
                new[] { new CachedViewBinding { prefab = prefab, anchor = view.gameObject } },
                view.transform, 3, 120f, (_, _, _, _) => { }, (_, _) => 0);
            cache.Connect();
            try
            {
                prefab.ToggleEvent.RaiseToggleCanvasEvent(true);
                Assert.AreEqual(0, cache.InstanceCount, "业务数据准备前不应创建界面。");

                // 注入已打开的临时界面，验证关闭路由，无需在编辑模式创建整棵业务预制体。
                typeof(CachedUIView).GetMethod("Open", InstanceFlags).Invoke(view, null);
                var entries = (IDictionary)typeof(UIViewCache).GetField("entries", InstanceFlags)
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

        /// <summary>停用临时对象，避免场景生命周期和事件订阅干扰定向调用。</summary>
        private T CreateComponent<T>() where T : Component
        {
            var go = new GameObject(typeof(T).Name) { hideFlags = HideFlags.HideAndDontSave };
            temporaryObjects.Add(go);
            go.SetActive(false);
            return go.AddComponent<T>();
        }

        private static void SetSingleton<T>(T instance) where T : MonoBehaviour
        {
            typeof(YSingleton<T>).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, instance);
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
