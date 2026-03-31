using System.Collections.Generic;
using MyEnums;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// AStar 节点管理器，负责地图初始化和节点数据管理
/// </summary>
public class AStarNodeManager : MonoBehaviour
{


    public static AStarNodeManager instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
        InitMapInfo();
        InitiateNodes();
    }

    [Header("Tilemaps")]
    public Tilemap[] tilemaps;
    [Header("Grid Settings")]
    public LayerMask obstacleLayers; // 需要识别为障碍物的层

    private float cellSize = 1f;
    private float safetyMargin = 0.3f; // 路径与障碍物的安全距离
    private Dictionary<Vector3, AStarNode> nodeCellMap;

    public Dictionary<Vector3, AStarNode> GetNodeMap() => nodeCellMap;
    public float GetCellSize() => cellSize;

    public Vector3 WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int y = Mathf.FloorToInt(worldPos.y / cellSize);
        return new Vector3(x, y);
    }

    public Vector3 CellToWorld(Vector3 cellPos)
    {
        Vector3 basePos = new Vector3(
            cellPos.x * cellSize + cellSize * 0.5f,
            cellPos.y * cellSize + cellSize * 0.5f,
            0
        );

        // 应用安全边距
        return ApplySafetyMargin(cellPos, basePos);
    }

    private Vector3 ApplySafetyMargin(Vector3 cellPos, Vector3 worldPos)//为了让路径与障碍物保持一定距离，避免贴边行走导致的卡顿或碰撞问题，添加了安全边距的计算
    {
        if (safetyMargin <= 0) return worldPos;

        // 检查是否有障碍物

        int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
        int[] dy = { 1, 1, 0, -1, -1, -1, 0, 1 };
        float marginX = 0, marginY = 0;

        for (int i = 0; i < 8; i++)
        {
            Vector3 neighborPos = new Vector3(cellPos.x + dx[i], cellPos.y + dy[i]);
            if (nodeCellMap.TryGetValue(neighborPos, out AStarNode neighbor))
            {
                if (neighbor.GetNodeType() == AStarNodeType.Obstacle)
                {
                    float realsafetyMargin = safetyMargin * cellSize;
                    if (dx[i] > 0) marginX = -realsafetyMargin;
                    else if (dx[i] < 0) marginX = realsafetyMargin;
                    else if (dy[i] > 0) marginY = -realsafetyMargin;
                    else if (dy[i] < 0) marginY = realsafetyMargin;
                }
            }
        }
        Vector3 optNode = new Vector3(worldPos.x + marginX, worldPos.y + marginY, 0);
        if (nodeCellMap[WorldToCell(optNode)].GetNodeType() != AStarNodeType.Obstacle)
            return optNode;
        else return worldPos;
    }

    private void InitMapInfo()
    {
        nodeCellMap = new Dictionary<Vector3, AStarNode>();
    }

    private void InitiateNodes()
    {
        if (tilemaps == null || tilemaps.Length == 0)
        {
            Debug.LogError("Tilemaps 未赋值！");
            return;
        }

        int subdivision = Mathf.RoundToInt(1f / cellSize);

        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap currentTilemap = tilemaps[i];
            if (currentTilemap == null) continue;

            BoundsInt bounds = currentTilemap.cellBounds;

            foreach (Vector3Int cellPos in bounds.allPositionsWithin)
            {
                if (currentTilemap.HasTile(cellPos))
                {
                    string layerName = LayerMask.LayerToName(currentTilemap.gameObject.layer);
                    ProcessTileWithSubdivision(layerName, cellPos, subdivision);
                }
            }
        }

        // 处理带有Collider的障碍物
        ProcessColliderObstacles();
    }

    private void ProcessColliderObstacles()
    {
        Collider2D[] colliders = FindObjectsOfType<Collider2D>();
        int obstacleCount = 0;

        foreach (Collider2D col in colliders)
        {
            // 检查层是否在障碍层中
            if ((obstacleLayers.value & (1 << col.gameObject.layer)) == 0) continue;

            Vector3 worldPos = col.transform.position;
            Vector3 cellPos = WorldToCell(worldPos);
            Vector3 key = new Vector3(cellPos.x, cellPos.y);

            if (nodeCellMap.TryGetValue(key, out AStarNode node))
            {
                node.SetNodeType(AStarNodeType.Obstacle);
            }
            else
            {
                nodeCellMap[key] = new AStarNode(cellPos, AStarNodeType.Obstacle);
            }
            obstacleCount++;
        }

        // Debug.Log($"A* 寻路地图已录入 {obstacleCount} 个Collider障碍物");
    }

    private void ProcessTileWithSubdivision(string layerName, Vector3 cellPos, int subdivision)
    {
        float startX = cellPos.x * subdivision;
        float startY = cellPos.y * subdivision;

        for (int i = 0; i < subdivision; i++)
        {
            for (int j = 0; j < subdivision; j++)
            {
                Vector3 subCellPos = new(startX + i, startY + j);
                ProcessTile(layerName, subCellPos);
            }
        }
    }

    private void ProcessTile(string layerName, Vector3 cellPos)
    {
        Vector3 key = new Vector3(cellPos.x, cellPos.y);

        switch (layerName)
        {
            case "Obstacle":
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
}
