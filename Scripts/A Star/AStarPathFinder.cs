using System.Collections.Generic;
using MyEnums;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Tilemaps;

//可能的优化：小顶堆存开启列表，动态A*，距离算法的优化
//关于网格和世界坐标的转化 由于转化关系，需要先导航到这个网格中心点才能开始导航
//地图数据获取也可以优化，用以解决稀疏地图的遍历问题
//可以用带权路径替换开根计算
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
        foreach (var item in nodeCellMap)
        {
            if (item.Value.GetNodeType() == AStarNodeType.Obstacle)
                Debug.Log(item.Key.ToString() + item.Value.GetNodeType().ToString());
        }
        Debug.Log(nodeCellMap[new Vector3Int(0,-3,0)].GetNodeType());
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
    public Dictionary<Vector3Int, AStarNode> GetNodeMap()
    {
        return nodeCellMap;
    }
    public Stack<PathFinderDetails> FindPath(Vector3 startPos, Vector3 endPos)
    {
        Dictionary<Vector3Int, PathFinderDetails> openDic = new();
        Stack<PathFinderDetails> closeStack = new();
        Vector3Int startCellPos = WorldToCell(startPos);
        Vector3Int endCellPos = WorldToCell(endPos);

        if ((!nodeCellMap.ContainsKey(startCellPos)) || (!nodeCellMap.ContainsKey(endCellPos)) || startCellPos == endCellPos)
            return null;

        PathFinderDetails startNodeDetails = MakePathFinderDetails(startCellPos, endCellPos, null);

        closeStack.Push(startNodeDetails);//起点压栈
        PathFinderDetails relativeFather = startNodeDetails;
        Vector3Int currentNodeCellPos = startCellPos;
        bool isDone=false;    

        while (true)
        {

            if (closeStack.Count > 0 && closeStack.Peek().GetNodePos() == endCellPos) break;

            AddNodeToOpen(currentNodeCellPos, endCellPos, openDic, relativeFather);

            Vector3Int minCostCellPos = SearchCheapestCost(openDic);

            closeStack.Push(openDic[minCostCellPos]);

            //找到下一步之后更新
            relativeFather = openDic[minCostCellPos];
            currentNodeCellPos = minCostCellPos;
            openDic.Remove(minCostCellPos);
        }


        return closeStack;
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
    private void AddNodeToOpen(Vector3Int currentNodeCellPos, Vector3Int endCellPos, Dictionary<Vector3Int, PathFinderDetails> openDic, PathFinderDetails relativeFather)
    {
        int x = nodeCellMap[currentNodeCellPos].GetX();
        int y = nodeCellMap[currentNodeCellPos].GetY();

        int[] dx = { 1, 1, 1, -1, -1, -1, 0, 0 };
        int[] dy = { 0, 1, -1, 0, 1, -1, -1, 1 };

        Vector3Int newNodeCellPos;

        for (int i = 0; i < 8; i++)
        {
            newNodeCellPos = new Vector3Int(x + dx[i], y + dy[i]);
            if (nodeCellMap.ContainsKey(newNodeCellPos))
            {
                AStarNodeType aStarNodeType = nodeCellMap[newNodeCellPos].GetNodeType();
                switch (aStarNodeType)
                {
                    case AStarNodeType.Walkable:
                        if (!openDic.ContainsKey(newNodeCellPos))
                            openDic.Add(newNodeCellPos, MakePathFinderDetails(newNodeCellPos, endCellPos, relativeFather));
                        // else//可能需要更新
                        // {
                        //     openDic.Remove(newNodeCellPos);
                        //     openDic.Add(newNodeCellPos, MakePathFinderDetails(newNodeCellPos, endCellPos, relativeFather));
                        // }
                        break;
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

    // 网格坐标 → 世界坐标（中心点）
    private Vector3 CellToWorld(Vector3Int cellPos)
    {
        return new Vector3(
            cellPos.x * cellSize + cellSize * 0.5f,
            cellPos.y * cellSize + cellSize * 0.5f,
            0
        );
    }
    private PathFinderDetails MakePathFinderDetails(Vector3Int nodePos, Vector3Int endPos, PathFinderDetails fatherNode)
    {
        return new PathFinderDetails(nodePos, endPos, fatherNode);
    }


}
