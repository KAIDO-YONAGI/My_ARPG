using System;
using System.Collections.Generic;
using MyEnums;
using UnityEngine;

//TODO 可能的优化：小顶堆存开启列表，动态A*，距离算法的优化
//关于网格和世界坐标的转化 由于转化关系，需要先导航到这个网格中心点才能开始导航
//地图数据获取也可以优化，用以解决稀疏地图的遍历问题
//可以用带权路径替换开根计算
//细分单元格
[RequireComponent(typeof(AStarNodeManager))]//依赖保证，不存在时自动添加
public class AStarPathFinder : YSingleton<AStarPathFinder>
{


    public Dictionary<(int x, int y), AStarNode> GetNodeMap() => AStarNodeManager.Instance.GetNodeMap();
    public (int x, int y) WorldToCell(Vector3 worldPos) => AStarNodeManager.Instance.WorldToCell(worldPos);
    public Vector3 CellToWorld(int cx, int cy) => AStarNodeManager.Instance.CellToWorld(cx, cy);
    public float GetCellSize() => AStarNodeManager.Instance.GetCellSize();

    private Dictionary<(int x, int y), AStarNode> NodeCellMap => AStarNodeManager.Instance.GetNodeMap();

    //8 邻域方向，只读共享，避免每次扩点重新分配
    private static readonly int[] dirX = { 0, 1, 1, 1, 0, -1, -1, -1 };
    private static readonly int[] dirY = { 1, 1, 0, -1, -1, -1, 0, 1 };

    public Stack<AStarDetails> FindPath(Vector3 optPos, Vector3 startPos, Vector3 endPos)
    {
        if (optPos == Vector3.zero) optPos = startPos;
        Dictionary<(int x, int y), AStarDetails> openDic = new();

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

        HashSet<(int x, int y)> closeSet = new();
        if ((!NodeCellMap.ContainsKey(startCell)) || (!NodeCellMap.ContainsKey(endCell)) || startCell == endCell)
        {
            return null;
        }
        AStarDetails startNode = new AStarDetails(startCell.x, startCell.y, endCell.x, endCell.y, null);
        openDic.Add(startCell, startNode);

        while (openDic.Count > 0)
        {
            var currentPos = SearchCheapestCost(openDic);
            AStarDetails current = openDic[currentPos];

            openDic.Remove(currentPos);
            closeSet.Add(currentPos);

            if (currentPos == endCell) return RetracePath(current);

            AddNodeToOpen(currentPos, endCell, openDic, closeSet, current);
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

    private (int x, int y) SearchCheapestCost(Dictionary<(int x, int y), AStarDetails> openDic)
    {
        float minCost = float.MaxValue;
        (int x, int y) minCostPos = default;
        foreach (var node in openDic)
        {
            float currentCost = node.Value.GetCost();
            if (minCost > currentCost)
            {
                minCost = currentCost;
                minCostPos = node.Key;
            }
        }
        return minCostPos;
    }

    private void AddNodeToOpen(
        (int x, int y) currentPos,
        (int x, int y) endPos,
        Dictionary<(int x, int y), AStarDetails> openDic,
        HashSet<(int x, int y)> closeSet,
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

            if (!openDic.ContainsKey(neighborPos))
            {
                openDic.Add(neighborPos, newNode);
            }
            else
            {
                if (newNode.GetDisToBeg() < openDic[neighborPos].GetDisToBeg())
                {
                    openDic[neighborPos] = newNode;
                }
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
