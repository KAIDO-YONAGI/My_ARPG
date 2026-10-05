using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Gameplay.Tests
{
    /// <summary>
    /// AStarOpenHeap 的 EditMode 测试：出堆顺序（f 升序，f 相同时 g 降序）、
    /// 同格重复入堆后旧条目仍留在堆里由调用方判过期、以及 Clear 复用。
    /// 本类不依赖 UnityEngine，可脱离编辑器运行。
    /// </summary>
    public class AStarOpenHeapTests
    {
        //与 AStarOpenHeap 内部规则一致的期望值计算，用于随机用例求「当前最小值」
        private static bool ExpectedBetter((float f, float g) a, (float f, float g) b)
        {
            return a.f < b.f || (a.f == b.f && a.g > b.g);
        }

        [Test]
        public void Pop_OrdersByFAscending()
        {
            var heap = new AStarOpenHeap();
            heap.Push((0, 0), 3f, 1f);
            heap.Push((1, 1), 1f, 1f);
            heap.Push((2, 2), 2f, 1f);

            Assert.AreEqual(3, heap.Count);
            Assert.AreEqual(1f, heap.Pop().F);
            Assert.AreEqual(2f, heap.Pop().F);
            Assert.AreEqual(3f, heap.Pop().F);
            Assert.AreEqual(0, heap.Count);
        }

        [Test]
        public void Pop_EqualF_PrefersLargerG()
        {
            var heap = new AStarOpenHeap();
            heap.Push((0, 0), 5f, 1f);  //f 相同，离起点最近
            heap.Push((1, 1), 5f, 9f);  //f 相同，离终点最近
            heap.Push((2, 2), 5f, 4f);

            //这条规则是搜索量的关键：违反它，A* 会退化成自起点的广度扩散
            Assert.AreEqual(9f, heap.Pop().G, "f 相同时必须先出 g 更大的条目");
            Assert.AreEqual(4f, heap.Pop().G);
            Assert.AreEqual(1f, heap.Pop().G);
        }

        [Test]
        public void Push_SameCellTwice_KeepsBothEntries()
        {
            var heap = new AStarOpenHeap();
            heap.Push((7, 7), 10f, 5f);
            heap.Push((7, 7), 9f, 4f); //同一格找到更优解，旧条目不删除

            Assert.AreEqual(2, heap.Count, "堆不去重，过期条目由调用方按 G 丢弃");

            AStarOpenHeap.Entry first = heap.Pop();
            Assert.AreEqual(4f, first.G);
            Assert.AreEqual(7, first.Pos.x);
            Assert.AreEqual(7, first.Pos.y);

            Assert.AreEqual(5f, heap.Pop().G, "被取代的旧条目仍留在堆里");
        }

        [Test]
        public void Clear_EmptiesAndStaysUsable()
        {
            var heap = new AStarOpenHeap();
            for (int i = 0; i < 100; i++) heap.Push((i, -i), i, i);

            heap.Clear();
            Assert.AreEqual(0, heap.Count);

            heap.Push((3, 4), 1f, 2f);
            Assert.AreEqual(1, heap.Count);

            AStarOpenHeap.Entry e = heap.Pop();
            Assert.AreEqual(3, e.Pos.x);
            Assert.AreEqual(4, e.Pos.y);
            Assert.AreEqual(1f, e.F);
            Assert.AreEqual(2f, e.G);
            Assert.AreEqual(0, heap.Count);
        }

        [Test]
        public void Pop_IsSorted_OnRandomInput()
        {
            var rng = new Random(20240613);
            var heap = new AStarOpenHeap();
            const int n = 2000;

            for (int i = 0; i < n; i++)
                heap.Push((i, -i), rng.Next(0, 50), rng.Next(0, 50));

            Assert.AreEqual(n, heap.Count);

            AStarOpenHeap.Entry prev = heap.Pop();
            for (int i = 1; i < n; i++)
            {
                AStarOpenHeap.Entry cur = heap.Pop();
                bool ordered = cur.F > prev.F || (cur.F == prev.F && cur.G <= prev.G);
                Assert.IsTrue(ordered,
                    $"第 {i} 次出堆违反顺序：prev(f={prev.F},g={prev.G}) -> cur(f={cur.F},g={cur.G})");
                prev = cur;
            }
            Assert.AreEqual(0, heap.Count);
        }

        [Test]
        public void InterleavedPushPop_AlwaysPopsCurrentMinimum()
        {
            var rng = new Random(4242);
            var heap = new AStarOpenHeap();
            var live = new List<(float f, float g)>();

            for (int step = 0; step < 3000; step++)
            {
                if (live.Count == 0 || rng.Next(100) < 60)
                {
                    float f = rng.Next(0, 20);
                    float g = rng.Next(0, 20);
                    heap.Push((step, step), f, g);
                    live.Add((f, g));
                    Assert.AreEqual(live.Count, heap.Count);
                }
                else
                {
                    int minIdx = 0;
                    for (int i = 1; i < live.Count; i++)
                        if (ExpectedBetter(live[i], live[minIdx])) minIdx = i;

                    AStarOpenHeap.Entry got = heap.Pop();
                    Assert.AreEqual(live[minIdx].f, got.F, $"step {step} 出堆 f 不是当前最小");
                    Assert.AreEqual(live[minIdx].g, got.G, $"step {step} 出堆 g 不是当前最优");
                    live.RemoveAt(minIdx);
                    Assert.AreEqual(live.Count, heap.Count);
                }
            }
        }
    }
}
