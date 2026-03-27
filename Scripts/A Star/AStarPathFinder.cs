using System.Collections;
using System.Collections.Generic;
using MyEnums;
using Unity.VisualScripting;
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
    public float cellSize = 1.0f;

    public static AStarPathFinder instance;
    private Dictionary<Vector3Int, AStarNode> nodeCellMap;
    private List<AStarNode> openList;
    private List<AStarNode> closeList;
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
                    int layer = currentTilemap.gameObject.layer;
                    ProcessTile(layer, cellPos);
                }
            }
        }
        foreach (var item in nodeCellMap)
        {
            if (item.Value.GetNodeType() == AStarNodeType.Obstacle)
                Debug.Log(item.Key.ToString() + item.Value.GetNodeType().ToString());
        }
    }
    private void ProcessTile(int layerIndex, Vector3Int cellPos)
    {
        Vector3Int key;

        switch (layerIndex)
        {
            case 10: // Obstacle
                key = new Vector3Int(cellPos.x, cellPos.y);

                if (nodeCellMap.ContainsKey(key))
                {
                    nodeCellMap[key].SetNodeType(AStarNodeType.Obstacle);
                }
                else//只在没有节点的时候创建对象
                {
                    nodeCellMap[key] = new AStarNode(cellPos, AStarNodeType.Obstacle);
                }
                break;

            case 11: // Walkable
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
    public List<AStarNode> FindPath(Vector3 startPos, Vector3 endPos)
    {
        Vector3Int startCellPos = WorldToCell(startPos);
        Vector3Int endCellPos = WorldToCell(endPos);
        List<AStarNode> beginList = new();
        Stack<AStarNode> closeStack = new();

        if ((!nodeCellMap.ContainsKey(startCellPos)) || (!nodeCellMap.ContainsKey(endCellPos)) || startCellPos == endCellPos)
            return null;

        AStarNode startNode = nodeCellMap[startCellPos];

        beginList.Add(startNode);//起点加入开启列表
        closeStack.Push(startNode);//起点压栈

        AStarNode relativeFather = startNode;
        Vector3Int currentNodeCellPos = startCellPos;

        while (true)
        {
            AddNodeToList(currentNodeCellPos, beginList, relativeFather);
            // relativeFather=
            break;
        }


        return null;
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


    private void AddNodeToList(Vector3Int nodeCellPos, List<AStarNode> beginList, AStarNode relativeFather)
    {
        nodeCellMap[nodeCellPos].SetFatherNode(null);

        int x = nodeCellMap[nodeCellPos].GetX();
        int y = nodeCellMap[nodeCellPos].GetY();

        int[] dx = { 1, 1, 1, -1, -1, -1, 0, 0 };
        int[] dy = { 0, 1, -1, 0, 1, -1, -1, 1 };

        for (int i = 0; i < 8; i++)
        {
            Vector3Int tempVector = new Vector3Int(x + dx[i], y + dy[i]);
            DealNode(tempVector, beginList, relativeFather);
        }

    }
    private void DealNode(Vector3Int tempVector, List<AStarNode> beginList, AStarNode relativeFather)
    {
        if (nodeCellMap.ContainsKey(tempVector))
        {
            AStarNodeType aStarNodeType = nodeCellMap[tempVector].GetNodeType();
            switch (aStarNodeType)
            {
                case AStarNodeType.Walkable:
                    beginList.Add(nodeCellMap[tempVector]);
                    break;
            }



        }
    }

    private float CalDistance(Vector3Int startPos, Vector3Int endPos)
    {
        return Vector3Int.Distance(startPos, endPos);
    }

}
