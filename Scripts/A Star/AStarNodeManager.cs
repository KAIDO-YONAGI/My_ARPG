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
        return new Vector3(
            cellPos.x * cellSize + cellSize * 0.5f,
            cellPos.y * cellSize + cellSize * 0.5f,
            0
        );
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
