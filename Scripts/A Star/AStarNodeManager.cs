using System.Collections.Generic;
using MyEnums;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// AStar 节点管理器，负责地图初始化和节点数据管理
/// </summary>
public class AStarNodeManager : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap[] tilemaps;
    [Header("Grid Settings")]
    [Range(0.1f, 1f)]
    public float cellSize = 1.0f;
    [Range(0f, 1f)]
    public float safetyMargin = 0.3f; // 路径与障碍物的安全距离

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

    private Vector3 ApplySafetyMargin(Vector3 cellPos, Vector3 worldPos)
    {
        if (safetyMargin <= 0) return worldPos;

        // 检查四个方向是否有障碍物
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        float marginX = 0, marginY = 0;

        for (int i = 0; i < 4; i++)
        {
            Vector3 neighborPos = new Vector3(cellPos.x + dx[i], cellPos.y + dy[i]);
            if (nodeCellMap.TryGetValue(neighborPos, out AStarNode neighbor))
            {
                if (neighbor.GetNodeType() == AStarNodeType.Obstacle)
                {
                    if (dx[i] > 0) marginX = -safetyMargin;
                    else if (dx[i] < 0) marginX = safetyMargin;
                    else if (dy[i] > 0) marginY = -safetyMargin;
                    else if (dy[i] < 0) marginY = safetyMargin;
                }
            }
        }

        return new Vector3(worldPos.x + marginX, worldPos.y + marginY, 0);
    }

    private void Awake()
    {
        InitMapInfo();
        InitiateNodes();
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
