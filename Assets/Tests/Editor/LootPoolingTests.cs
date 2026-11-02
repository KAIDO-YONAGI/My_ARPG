using Gameplay.Inventory;
using NUnit.Framework;
using Pipeline.SO;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gameplay.Tests
{
    /// <summary>
    /// Loot 池复用的显示状态复原：Pickup 片段把子物体的透明度动画到 0、把排序层级临时抬高、
    /// 把缩放压扁，而 Animator 关闭了禁用时写回默认值，这些值会留在池对象上。
    /// 重新取用必须按预制体基线写回，否则掉落物是一张全透明精灵。
    /// </summary>
    public class LootPoolingTests
    {
        private GameObject root;
        private Loot loot;
        private ItemSO item;

        [SetUp]
        public void SetUp()
        {
            // 编辑模式下 Animator 未初始化，复位调用会打错误日志；断言只看显示状态。
            LogAssert.ignoreFailingMessages = true;

            root = new GameObject("Loot");
            GameObject spriteObject = new GameObject("Sprite");
            spriteObject.transform.SetParent(root.transform, false);

            loot = root.AddComponent<Loot>();
            loot.sr = spriteObject.AddComponent<SpriteRenderer>();
            loot.animator = root.AddComponent<Animator>();
            item = ScriptableObject.CreateInstance<ItemSO>();
            item.itemName = "TestItem";
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            if (item != null) Object.DestroyImmediate(item);
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void Initialize_AfterPickupAnimation_RestoresRestDisplayState()
        {
            loot.Initialize(item, 1);//首次取用建立静止基线
            Color restColor = loot.sr.color;
            int restSortingOrder = loot.sr.sortingOrder;
            Vector3 restScale = loot.sr.transform.localScale;
            Vector3 restPosition = loot.sr.transform.localPosition;

            // 模拟 Pickup 片段播完写下的状态
            loot.sr.color = new Color(1f, 1f, 1f, 0f);
            loot.sr.sortingOrder = 50;
            loot.sr.transform.localScale = new Vector3(0.7f, 1f, 1f);
            loot.sr.transform.localPosition = Vector3.zero;

            loot.Initialize(item, 2);//池复用

            Assert.AreEqual(restColor, loot.sr.color, "透明度必须写回，否则掉落物全透明");
            Assert.AreEqual(restSortingOrder, loot.sr.sortingOrder);
            Assert.AreEqual(restScale, loot.sr.transform.localScale);
            Assert.AreEqual(restPosition, loot.sr.transform.localPosition);
            Assert.IsTrue(loot.sr.enabled);
        }
    }
}
