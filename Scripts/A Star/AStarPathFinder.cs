using System.Collections.Generic;
using MyEnums;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Tilemaps;

//可能的优化：小顶堆存开启列表，动态A*，距离算法的优化
//关于网格和世界坐标的转化 由于转化关系，需要先导航到这个网格中心点才能开始导航
//地图数据获取也可以优化，用以解决稀疏地图的遍历问题
//可以用带权路径替换开根计算
//细分单元格
public class AStarPathFinder : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap[] tilemaps;
    private float cellSize = 1.0f;

    public static AStarPathFinder instance;
    private Dictionary<Vector3Int, AStarNode> nodeCellMap;


    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
        InitMapInfo();
        InitiateNodes();

    }
    public Dictionary<Vector3Int, AStarNode> GetNodeMap()
    {
        return nodeCellMap;
    }
    public float GetCellSize()=>cellSize;

    public Stack<PathFinderDetails> FindPath(Vector3 startPos, Vector3 endPos)
    {
        Dictionary<Vector3Int, PathFinderDetails> openDic = new();
        Vector3Int startCellPos = WorldToCell(startPos);
        Vector3Int endCellPos = WorldToCell(endPos);
        HashSet<Vector3Int> closeSet = new();
        if ((!nodeCellMap.ContainsKey(startCellPos)) || (!nodeCellMap.ContainsKey(endCellPos)) || startCellPos == endCellPos)
            return null;
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
    private void InitMapInfo()
    {
        nodeCellMap = new Dictionary<Vector3Int, AStarNode>();
    }


    private void InitiateNodes()
    {
        if (tilemaps == null || tilemaps.Length == 0)
        {
            return;
        }

        // 遍历所有 Tilemap
        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap currentTilemap = tilemaps[i];
            if (currentTilemap == null) continue;

            // 获取 Tilemap 边界
            BoundsInt bounds = currentTilemap.cellBounds;

            // 遍历 Tilemap 中的所有单元格
            foreach (Vector3Int cellPos in bounds.allPositionsWithin)
            {
                if (currentTilemap.HasTile(cellPos))
                {
                    string layerName = LayerMask.LayerToName(currentTilemap.gameObject.layer);
                    ProcessTile(layerName, cellPos);

                }
            }
        }
        // foreach (var item in nodeCellMap)
        // {
        //     if (item.Value.GetNodeType() == AStarNodeType.Obstacle)
        //         Debug.Log(item.Key.ToString() + item.Value.GetNodeType().ToString());
        // }
        // Debug.Log(nodeCellMap[new Vector3Int(0, -4, 0)].GetNodeType());
    }
    private void ProcessTile(string layerName, Vector3Int cellPos)
    {
        Vector3Int key;

        switch (layerName)
        {
            case "Obstacle":
                key = new Vector3Int(cellPos.x, cellPos.y);

                if (nodeCellMap.ContainsKey(key))
                {
                    nodeCellMap[key].SetNodeType(AStarNodeType.Obstacle);
                }
                else
                {
                    nodeCellMap[key] = new AStarNode(cellPos, AStarNodeType.Obstacle);
                }
                break;

            case "Walkable":
                key = new Vector3Int(cellPos.x, cellPos.y);

                if (nodeCellMap.ContainsKey(key))
                {
                    nodeCellMap[key].SetNodeType(AStarNodeType.Walkable);
                }
                else
                {
                    nodeCellMap[key] = new AStarNode(cellPos, AStarNodeType.Walkable);
                }
                break;
        }
    }

    private Stack<PathFinderDetails> RetracePath(PathFinderDetails endNode)
    {
        Stack<PathFinderDetails> path = new Stack<PathFinderDetails>();

        PathFinderDetails current = endNode;

        while (current != null)
        {
            path.Push(current);
            current = current.GetFatherNode(); // 确保你有这个方法
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

        int[] dx = { 1, 1, 1, -1, -1, -1, 0, 0 };
        int[] dy = { 0, 1, -1, 0, 1, -1, -1, 1 };

        for (int i = 0; i < 8; i++)
        {
            Vector3Int neighborPos = new Vector3Int(x + dx[i], y + dy[i]);

            //  不存在 or 障碍
            if (!nodeCellMap.ContainsKey(neighborPos)) continue;
            if (nodeCellMap[neighborPos].GetNodeType() != AStarNodeType.Walkable) continue;

            //  已经处理过
            if (closeSet.Contains(neighborPos)) continue;//在set里的都是最优路径点（遍历完了，剪枝）

            PathFinderDetails newNode = MakePathFinderDetails(neighborPos, endPos, current);//current作为父节点构造一个新的节点

            //  不在 open，直接加
            if (!openDic.ContainsKey(neighborPos))
            {
                openDic.Add(neighborPos, newNode);
            }
            else
            {
                // 更优路径更新
                if (newNode.GetDisToBeg() < openDic[neighborPos].GetDisToBeg())
                {
                    openDic[neighborPos] = newNode;//会更新父节点、寻路代价等信息
                }
            }
        }
    }
    private Vector3Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int y = Mathf.FloorToInt(worldPos.y / cellSize);
        return new Vector3Int(x, y);
    }

    private PathFinderDetails MakePathFinderDetails(Vector3Int nodePos, Vector3Int endPos, PathFinderDetails fatherNode)
    {
        return new PathFinderDetails(nodePos, endPos, fatherNode);
    }


}
