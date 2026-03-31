using System;
using System.Collections.Generic;
using MyEnums;
using UnityEngine;

//可能的优化：小顶堆存开启列表，动态A*，距离算法的优化
//关于网格和世界坐标的转化 由于转化关系，需要先导航到这个网格中心点才能开始导航
//地图数据获取也可以优化，用以解决稀疏地图的遍历问题
//可以用带权路径替换开根计算
//细分单元格
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(AStarNodeManager))]//依赖保证（不存在会自动添加）
public class AStarPathFinder : MonoBehaviour
{
    public static AStarPathFinder instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public Dictionary<Vector3, AStarNode> GetNodeMap() => AStarNodeManager.instance.GetNodeMap();
    public Vector3 WorldToCell(Vector3 worldPos) => AStarNodeManager.instance.WorldToCell(worldPos);
    public Vector3 CellToWorld(Vector3 cellPos) => AStarNodeManager.instance.CellToWorld(cellPos);
    public float GetCellSize() => AStarNodeManager.instance.GetCellSize();

    private Dictionary<Vector3, AStarNode> NodeCellMap => AStarNodeManager.instance.GetNodeMap();

    public Stack<PathFinderDetails> FindPath(Vector3 optPos, Vector3 startPos, Vector3 endPos)
    {
        if (optPos == Vector3.zero) optPos = startPos;
        Dictionary<Vector3, PathFinderDetails> openDic = new();

        Vector3 startCellPos = WorldToCell(startPos);
        Vector3 endCellPos = WorldToCell(endPos);
        Vector3 optCellPos = WorldToCell(optPos);


        if (NodeCellMap.ContainsKey(optCellPos)
            && NodeCellMap[optCellPos].GetNodeType() == AStarNodeType.Walkable
            && NoCoverObstacleNodes(startCellPos,optCellPos))
        {
            startCellPos = optCellPos;
        }

        HashSet<Vector3> closeSet = new();
        if ((!NodeCellMap.ContainsKey(startCellPos)) || (!NodeCellMap.ContainsKey(endCellPos)) || startCellPos == endCellPos)
        {
            Debug.Log($"[FindPath] 路径检查失败 - 起点存在:{NodeCellMap.ContainsKey(startCellPos)}, 终点存在:{NodeCellMap.ContainsKey(endCellPos)}, 相同:{startCellPos == endCellPos}");
            return null;
        }
        PathFinderDetails startNode = MakePathFinderDetails(startCellPos, endCellPos, null);
        openDic.Add(startCellPos, startNode);

        while (openDic.Count > 0)
        {
            Vector3 currentPos = SearchCheapestCost(openDic);
            PathFinderDetails current = openDic[currentPos];

            openDic.Remove(currentPos);
            closeSet.Add(currentPos);

            if (currentPos == endCellPos) return RetracePath(current);

            AddNodeToOpen(currentPos, endCellPos, openDic, closeSet, current);
        }

        return null;
    }
    private bool NoCoverObstacleNodes(Vector3 startCellPos, Vector3 optCellPos)
    {
        // 计算两点之间的距离
        float distance = Vector3.Distance(startCellPos, optCellPos);

        // 使用射线检测方式检查两点之间的路径
        Vector3 direction = (optCellPos - startCellPos).normalized;
        float step = 1*GetCellSize(); // 步长，根据需要调整

        for (float t = step; t < distance; t += step)
        {
            Vector3 checkPos = startCellPos + direction * t;
            Vector3 cellPos = new Vector3(Mathf.Round(checkPos.x), Mathf.Round(checkPos.y), Mathf.Round(checkPos.z));

            if (NodeCellMap.ContainsKey(cellPos))
            {
                if (NodeCellMap[cellPos].GetNodeType() == AStarNodeType.Obstacle)
                {
                    return false;
                }
            }
        }

        return true;
    }
    private Stack<PathFinderDetails> RetracePath(PathFinderDetails endNode)
    {
        Stack<PathFinderDetails> path = new Stack<PathFinderDetails>();
        PathFinderDetails current = endNode;
        // PathFinderDetails lastNodeWorldNode;

        while (current != null)
        {
            path.Push(current);
            current = current.GetFatherNode();
        }

        return path;
    }

    private Vector3 SearchCheapestCost(Dictionary<Vector3, PathFinderDetails> openDic)
    {
        float minCost = float.MaxValue;
        Vector3 minCostCellPos = new();
        foreach (var node in openDic)
        {
            float currentCost = node.Value.GetCost();
            if (minCost > currentCost)
            {
                minCost = currentCost;
                minCostCellPos = node.Key;
            }
        }
        return minCostCellPos;
    }

    private void AddNodeToOpen(
        Vector3 currentPos,
        Vector3 endPos,
        Dictionary<Vector3, PathFinderDetails> openDic,
        HashSet<Vector3> closeSet,
        PathFinderDetails current)
    {
        float x = currentPos.x;
        float y = currentPos.y;

        int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
        int[] dy = { 1, 1, 0, -1, -1, -1, 0, 1 };

        for (int i = 0; i < 8; i++)
        {
            Vector3 neighborPos = new Vector3(x + dx[i], y + dy[i]);

            if (closeSet.Contains(neighborPos)) continue;//已经在关闭列表中
            if (!NodeCellMap.ContainsKey(neighborPos)) continue;//节点不在地图中
            if (NodeCellMap[neighborPos].GetNodeType() != AStarNodeType.Walkable) continue;//节点不可行走
            if (NodeCellMap[neighborPos].GetNodeType() == AStarNodeType.Walkable && !CanWalkDiagonally(x, y, dx[i], dy[i])) continue;//判断能不能走对角线

            PathFinderDetails newNode = MakePathFinderDetails(neighborPos, endPos, current);

            if (!openDic.ContainsKey(neighborPos))//不含临近节点
            {
                openDic.Add(neighborPos, newNode);
            }
            else
            {
                if (newNode.GetDisToBeg() < openDic[neighborPos].GetDisToBeg())//如果新节点到起点的距离更短，更新父节点和距离
                {
                    openDic[neighborPos] = newNode;
                }
            }
        }
    }

    private bool CanWalkDiagonally(float x, float y, float dx, float dy)
    {
        if (Math.Abs(dx * dy) == 1)
        {
            Vector3 pos = new Vector3(x + dx, y + dy);
            Vector3 xOffset = new Vector3(dx, 0);
            Vector3 yOffset = new Vector3(0, dy);

            if (!(NodeCellMap[pos - xOffset].GetNodeType() == AStarNodeType.Walkable) &&
            !(NodeCellMap[pos - yOffset].GetNodeType() == AStarNodeType.Walkable))
            {
                return false;
            }
        }
        return true;
    }

    private PathFinderDetails MakePathFinderDetails(Vector3 nodePos, Vector3 endPos, PathFinderDetails fatherNode)
    {
        return new PathFinderDetails(nodePos, endPos, fatherNode);
    }
}
