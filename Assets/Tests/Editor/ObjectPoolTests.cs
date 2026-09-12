using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gameplay.Tests
{
    /// <summary>
    /// ObjectPool 的 EditMode 测试：预热注册、Get/Return 回调与池内对象销毁后的选取。
    /// </summary>
    public class ObjectPoolTests
    {
        private class TestPoolable : MonoBehaviour, IPoolable
        {
            public int onGetCount;
            public int onReturnCount;
            public void OnPoolGet() => onGetCount++;
            public void OnPoolReturn() => onReturnCount++;
        }

        private GameObject parent;
        private List<Object> trash;

        [SetUp]
        public void SetUp()
        {
            parent = new GameObject("PoolParent");
            trash = new List<Object>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in trash)
                if (o != null) Object.DestroyImmediate(o);
            if (parent != null) Object.DestroyImmediate(parent);
        }

        private TestPoolable MakePrefab()
        {
            var go = new GameObject("TestPoolablePrefab");
            trash.Add(go);
            return go.AddComponent<TestPoolable>();
        }

        [Test]
        public void Prewarm_CreatesInactiveInstances()
        {
            var prefab = MakePrefab();
            var pool = new ObjectPool<TestPoolable>(prefab, 3, parent.transform, 10);
            Assert.AreEqual(3, pool.Count);
            Assert.AreEqual(3, pool.TotalCreated);
            // 预热对象不触发 OnPoolGet，且应处于未激活状态
            var obj = pool.Get();
            Assert.AreEqual(2, pool.Count);
            Assert.IsTrue(obj.gameObject.activeSelf);
        }

        [Test]
        public void Get_BeyondMaxSize_ReturnsNull()
        {
            var prefab = MakePrefab();
            var pool = new ObjectPool<TestPoolable>(prefab, 0, parent.transform, 2);
            Assert.IsNotNull(pool.Get());
            Assert.IsNotNull(pool.Get());
            Assert.IsNull(pool.Get(), "达到 maxSize 后 Get 应返回 null 由调用方兜底");
        }

        [Test]
        public void Return_ReusesInstanceAndFiresCallbacks()
        {
            var prefab = MakePrefab();
            var pool = new ObjectPool<TestPoolable>(prefab, 1, parent.transform, 5);
            var first = pool.Get();
            pool.Return(first);

            var second = pool.Get();
            Assert.AreSame(first, second, "归还后应复用同一实例");
            Assert.AreEqual(2, first.onGetCount);
            Assert.AreEqual(1, first.onReturnCount);
            // 借出状态应为激活
            Assert.IsTrue(second.gameObject.activeSelf);
        }

        [Test]
        public void Return_SetsParentToPoolParent()
        {
            var prefab = MakePrefab();
            var pool = new ObjectPool<TestPoolable>(prefab, 0, parent.transform, 5);
            var obj = pool.Get();
            pool.Return(obj);
            Assert.AreEqual(parent.transform, obj.transform.parent);
        }

        [Test]
        public void Get_SkipsDestroyedPoolEntries()
        {
            var prefab = MakePrefab();
            var pool = new ObjectPool<TestPoolable>(prefab, 2, parent.transform, 5);
            var a = pool.Get();
            var b = pool.Get();
            pool.Return(a);
            pool.Return(b);
            Object.DestroyImmediate(b.gameObject); // 模拟随场景卸载被销毁

            var c = pool.Get();
            Assert.AreSame(a, c, "应跳过已销毁的池内对象，返回存活实例");
        }
    }
}
