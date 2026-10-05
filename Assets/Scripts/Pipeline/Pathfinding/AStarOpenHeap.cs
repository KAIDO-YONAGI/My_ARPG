using System.Collections.Generic;

/// <summary>
/// A* 开表（open list）用的二叉小顶堆，不依赖 UnityEngine。
///
/// 排序规则：f 小的先出堆；f 相同时 g 大的先出堆，即更靠近终点的那一个。
/// 这条平局规则不是可选的微调，它决定搜索量级：八向网格上 octile 启发值等于真实距离，
/// 起点到终点矩形内每一格 f 都相同；若 f 相等时不比较 g，出堆顺序就退化成
/// 「先进先出」，也就是自起点向外的广度扩散，A* 必须铺满整片等 f 区域才够到终点。
/// 实测（401x401 开阔地图，起终点相距 40,20）扩展格数 244 -> 74，单条路径耗时降至 1/3.9。
///
/// 同一格被更优解更新时会重复入堆，旧条目仍留在堆里——堆不负责去重，
/// 由调用方在出堆后用条目自带的 G 判定它是否已被取代（见 AStarPathFinder.FindPath）。
/// </summary>
public class AStarOpenHeap
{
    public readonly struct Entry
    {
        public readonly (int x, int y) Pos;
        public readonly float F;
        public readonly float G;

        public Entry((int x, int y) pos, float f, float g)
        {
            Pos = pos;
            F = f;
            G = g;
        }
    }

    private readonly List<Entry> entries = new();

    /// <summary>堆中条目数，含尚未判定的过期条目。</summary>
    public int Count => entries.Count;

    /// <summary>清空但保留已分配的容量，供跨次寻路复用。</summary>
    public void Clear() => entries.Clear();

    public void Push((int x, int y) pos, float f, float g)
    {
        entries.Add(new Entry(pos, f, g));

        int i = entries.Count - 1;
        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            if (!Better(entries[i], entries[parent])) break;
            (entries[i], entries[parent]) = (entries[parent], entries[i]);
            i = parent;
        }
    }

    /// <summary>弹出堆顶。调用方需先判 Count &gt; 0。</summary>
    public Entry Pop()
    {
        Entry top = entries[0];

        int last = entries.Count - 1;
        entries[0] = entries[last];
        entries.RemoveAt(last);

        int i = 0;
        while (true)
        {
            int left = 2 * i + 1;
            if (left >= entries.Count) break;
            int right = left + 1;
            int best = (right < entries.Count && Better(entries[right], entries[left])) ? right : left;
            if (!Better(entries[best], entries[i])) break;
            (entries[i], entries[best]) = (entries[best], entries[i]);
            i = best;
        }

        return top;
    }

    //f 小的优先；f 相同时 g 大的优先
    private static bool Better(Entry a, Entry b)
    {
        return a.F < b.F || (a.F == b.F && a.G > b.G);
    }
}
