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
[RequireComponent(typeof(AStarNodeManager))]
public class AStarPathFinder : MonoBehaviour
{
    public static AStarPathFinder instance;
    private AStarNodeManager nodeManager;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        nodeManager = GetComponent<AStarNodeManager>();
    }

    public Dictionary<Vector3Int, AStarNode> GetNodeMap() => nodeManager.GetNodeMap();
    public Vector3Int WorldToCell(Vector3 worldPos) => nodeManager.WorldToCell(worldPos);
    public Vector3 CellToWorld(Vector3Int cellPos) => nodeManager.CellToWorld(cellPos);
    public float GetCellSize() => nodeManager.GetCellSize();

    private Dictionary<Vector3Int, AStarNode> NodeCellMap => nodeManager.GetNodeMap();

    public Stack<PathFinderDetails> FindPath(Vector3 optPos, Vector3 startPos, Vector3 endPos)
    {
        Dictionary<Vector3Int, PathFinderDetails> openDic = new();
        Vector3Int startCellPos = WorldToCell(startPos);
        Vector3Int endCellPos = WorldToCell(endPos);
        Vector3Int optCellPos = WorldToCell(optPos);
        if (NodeCellMap.ContainsKey(optCellPos) && NodeCellMap[optCellPos].GetNodeType() == AStarNodeType.Walkable)
        {
            startCellPos = optCellPos;
        }

        HashSet<Vector3Int> closeSet = new();
        if ((!NodeCellMap.ContainsKey(startCellPos)) || (!NodeCellMap.ContainsKey(endCellPos)) || startCellPos == endCellPos)
        {
            Debug.Log($"[FindPath] 路径检查失败 - 起点存在:{NodeCellMap.ContainsKey(startCellPos)}, 终点存在:{NodeCellMap.ContainsKey(endCellPos)}, 相同:{startCellPos == endCellPos}");
            return null;
        }
        PathFinderDetails startNode = MakePathFinderDetails(startCellPos, endCellPos, null);
        openDic.Add(startCellPos, startNode);

        while (openDic.Count > 0)
        {
            Vector3Int currentPos = SearchCheapestCost(openDic);
            PathFinderDetails current = openDic[currentPos];

            openDic.Remove(currentPos);
            closeSet.Add(currentPos);

            if (currentPos == endCellPos)
            {
                return RetracePath(current);
            }

            AddNodeToOpen(currentPos, endCellPos, openDic, closeSet, current);
        }

        return null;
    }

    private Stack<PathFinderDetails> RetracePath(PathFinderDetails endNode)
    {
        Stack<PathFinderDetails> path = new Stack<PathFinderDetails>();

        PathFinderDetails current = endNode;

        while (current != null)
        {
            path.Push(current);
            current = current.GetFatherNode();
        }

        return path;
    }

    private Vector3Int SearchCheapestCost(Dictionary<Vector3Int, PathFinderDetails> openDic)
    {
        float minCost = float.MaxValue;
        Vector3Int minCostCellPos = new();
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
        Vector3Int currentPos,
        Vector3Int endPos,
        Dictionary<Vector3Int, PathFinderDetails> openDic,
        HashSet<Vector3Int> closeSet,
        PathFinderDetails current)
    {
        int x = currentPos.x;
        int y = currentPos.y;

        int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
        int[] dy = { 1, 1, 0, -1, -1, -1, 0, 1 };

        for (int i = 0; i < 8; i++)
        {
            Vector3Int neighborPos = new Vector3Int(x + dx[i], y + dy[i]);

            if (!NodeCellMap.ContainsKey(neighborPos)) continue;
            if (NodeCellMap[neighborPos].GetNodeType() != AStarNodeType.Walkable) continue;
            if (NodeCellMap[neighborPos].GetNodeType() == AStarNodeType.Walkable && !CanWalkDiagonally(x, y, dx[i], dy[i])) continue;

            if (closeSet.Contains(neighborPos)) continue;

            PathFinderDetails newNode = MakePathFinderDetails(neighborPos, endPos, current);

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
            Vector3Int pos = new Vector3Int(x + dx, y + dy);
            Vector3Int xOffset = new Vector3Int(dx, 0);
            Vector3Int yOffset = new Vector3Int(0, dy);

            if (!(NodeCellMap[pos - xOffset].GetNodeType() == AStarNodeType.Walkable) &&
            !(NodeCellMap[pos - yOffset].GetNodeType() == AStarNodeType.Walkable))
            {
                return false;
            }
        }
        return true;
    }

    private PathFinderDetails MakePathFinderDetails(Vector3Int nodePos, Vector3Int endPos, PathFinderDetails fatherNode)
    {
        return new PathFinderDetails(nodePos, endPos, fatherNode);
    }
}
