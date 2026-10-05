using System;
using System.Collections.Generic;
using MyEnums;
using UnityEngine;

//TODO 可能的优化：动态A*，距离算法的优化
//关于网格和世界坐标的转化 由于转化关系，需要先导航到这个网格中心点才能开始导航
//地图数据获取也可以优化，用以解决稀疏地图的遍历问题
//细分单元格
[RequireComponent(typeof(AStarNodeManager))]//依赖保证，不存在时自动添加
public class AStarPathFinder : YSingleton<AStarPathFinder>
{


    public Dictionary<(int x, int y), AStarNode> GetNodeMap() => AStarNodeManager.Instance.GetNodeMap();
    public (int x, int y) WorldToCell(Vector3 worldPos) => AStarNodeManager.Instance.WorldToCell(worldPos);
    public Vector3 CellToWorld(int cx, int cy) => AStarNodeManager.Instance.CellToWorld(cx, cy);
    public Vector3 GetWaypoint(int cx, int cy) => AStarNodeManager.Instance.GetWaypoint(cx, cy);
    public float GetCellSize() => AStarNodeManager.Instance.GetCellSize();

    private Dictionary<(int x, int y), AStarNode> NodeCellMap => AStarNodeManager.Instance.GetNodeMap();

    //8 邻域方向，只读共享，避免每次扩点重新分配
    private static readonly int[] dirX = { 0, 1, 1, 1, 0, -1, -1, -1 };
    private static readonly int[] dirY = { 1, 1, 0, -1, -1, -1, 0, 1 };

    //开表簿记：某格是否仍在开表中、以及它当前最优的 g。扩展顺序交给 AStarOpenHeap。
    //三者都跨次寻路复用：FindPath 全程同步、不可重入，清空即可，避免每次寻路重新分配（原先单条路径 15~80KB）。
    private readonly Dictionary<(int x, int y), AStarDetails> openDic = new();
    private readonly HashSet<(int x, int y)> closeSet = new();
    private readonly AStarOpenHeap heap = new();

    public Stack<AStarDetails> FindPath(Vector3 optPos, Vector3 startPos, Vector3 endPos)
    {
        if (optPos == Vector3.zero) optPos = startPos;

        openDic.Clear();
        closeSet.Clear();
        heap.Clear();

        var startCell = WorldToCell(startPos);
        var endCell = WorldToCell(endPos);
        var optCell = WorldToCell(optPos);


        if (startCell != optCell
            && NodeCellMap.ContainsKey(optCell)
            && NodeCellMap[optCell].GetNodeType() == AStarNodeType.Walkable
            && NoCoverObstacleNodes(startCell, optCell))
        {
            startCell = optCell;
        }

        if ((!NodeCellMap.ContainsKey(startCell)) || (!NodeCellMap.ContainsKey(endCell)) || startCell == endCell)
        {
            return null;
        }
        AStarDetails startNode = new AStarDetails(startCell.x, startCell.y, endCell.x, endCell.y, null);
        openDic.Add(startCell, startNode);
        heap.Push(startCell, startNode.GetCost(), startNode.GetDisToBeg());

        while (heap.Count > 0)
        {
            AStarOpenHeap.Entry entry = heap.Pop();
            //已关闭的格子（openDic 里没了），或被更优 g 取代的过期条目，直接丢弃
            if (!openDic.TryGetValue(entry.Pos, out AStarDetails current)) continue;
            if (entry.G > current.GetDisToBeg()) continue;

            var currentPos = entry.Pos;
            openDic.Remove(currentPos);
            closeSet.Add(currentPos);

            if (currentPos == endCell) return RetracePath(current);

            AddNodeToOpen(currentPos, endCell, current);
        }

        return null;
    }
    private bool NoCoverObstacleNodes((int x, int y) startCell, (int x, int y) optCell)
    {
        float vecX = optCell.x - startCell.x;
        float vecY = optCell.y - startCell.y;
        float distance = Mathf.Sqrt(vecX * vecX + vecY * vecY);
        if (distance > 0) { vecX /= distance; vecY /= distance; }

        //逐格采样，步长单位为格
        float step = 1f;

        for (float threshold = step; threshold < distance; threshold += step)
        {
            int cx = (int)Math.Round(startCell.x + vecX * threshold);
            int cy = (int)Math.Round(startCell.y + vecY * threshold);

            var key = (cx, cy);
            if (NodeCellMap.TryGetValue(key, out AStarNode node))
            {
                if (node.GetNodeType() == AStarNodeType.Obstacle)
                {
                    return false;
                }
            }
        }

        return true;
    }
    private Stack<AStarDetails> RetracePath(AStarDetails endNode)
    {
        Stack<AStarDetails> path = new Stack<AStarDetails>();
        AStarDetails current = endNode;

        while (current != null)
        {
            path.Push(current);
            current = current.GetFatherNode();
        }

        return path;
    }

    private void AddNodeToOpen(
        (int x, int y) currentPos,
        (int x, int y) endPos,
        AStarDetails current)
    {
        int cx = currentPos.x;
        int cy = currentPos.y;

        for (int i = 0; i < 8; i++)
        {
            int nx = cx + dirX[i];
            int ny = cy + dirY[i];
            var neighborPos = (x: nx, y: ny);

            if (closeSet.Contains(neighborPos)) continue;
            if (!NodeCellMap.TryGetValue(neighborPos, out AStarNode neighbor)
                || neighbor.GetNodeType() != AStarNodeType.Walkable) continue;
            if (!CanWalkDiagonally(cx, cy, dirX[i], dirY[i])) continue;

            AStarDetails newNode = new AStarDetails(nx, ny, endPos.x, endPos.y, current);

            if (!openDic.TryGetValue(neighborPos, out AStarDetails existing))
            {
                openDic.Add(neighborPos, newNode);
                heap.Push(neighborPos, newNode.GetCost(), newNode.GetDisToBeg());
            }
            else if (newNode.GetDisToBeg() < existing.GetDisToBeg())
            {
                //旧条目留在堆里，出堆时按 G 判过期；平局规则见 AStarOpenHeap
                openDic[neighborPos] = newNode;
                heap.Push(neighborPos, newNode.GetCost(), newNode.GetDisToBeg());
            }
        }
    }

    private bool CanWalkDiagonally(int x, int y, int dx, int dy)
    {
        if (Math.Abs(dx * dy) == 1)
        {
            //缺失的格子按不可走处理，避免地图边缘直接索引抛异常
            NodeCellMap.TryGetValue((x + dx, y), out AStarNode xNeighbor);
            NodeCellMap.TryGetValue((x, y + dy), out AStarNode yNeighbor);

            if ((xNeighbor == null || xNeighbor.GetNodeType() != AStarNodeType.Walkable) &&
                (yNeighbor == null || yNeighbor.GetNodeType() != AStarNodeType.Walkable))
            {
                return false;
            }
        }
        return true;
    }
}
