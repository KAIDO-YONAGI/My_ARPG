using System.Collections;
using System.Collections.Generic;
using MyEnums;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class NodeMapManager : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap[] tilemaps;
    public static NodeMapManager instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
        InitMapInfo();
    }
    public void InitMapInfo()
    {
        nodeMap = new Dictionary<Vector2Int, AStarNode>();
    }
    private Dictionary<Vector2Int, AStarNode> nodeMap;
    private List<AStarNode> openList;
    private List<AStarNode> closeList;

    void Start()
    {
        InitiateNodes();
    }
    void InitiateNodes()
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
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                if (currentTilemap.HasTile(pos))
                {
                    // 获取瓦片的世界位置
                    int layer = currentTilemap.gameObject.layer;
                    // 这里可以处理每个瓦片
                    ProcessTile(layer, pos);
                }
            }
        }
        foreach (var item in nodeMap)
        {
            if(item.Value.GetNodeType()==AStarNodeType.Obstacle)
            Debug.Log(item.Key.ToString()+item.Value.GetNodeType().ToString());
        }
    }
    void ProcessTile(int layerIndex, Vector3Int cellPos)
    {
        Vector2Int key;

        switch (layerIndex)
        {
            case 10: // Obstacle
                key = new Vector2Int(cellPos.x, cellPos.y);

                if (nodeMap.ContainsKey(key))
                {
                    nodeMap[key].SetNodeType(AStarNodeType.Obstacle);
                }
                else//只在没有节点的时候创建对象
                {
                    nodeMap[key] = new AStarNode(cellPos, AStarNodeType.Obstacle);
                }
                break;

            case 11: // Walkable
                key = new Vector2Int(cellPos.x, cellPos.y);

                if (nodeMap.ContainsKey(key))
                {
                    nodeMap[key].SetNodeType(AStarNodeType.Walkable);
                }
                else
                {
                    nodeMap[key] = new AStarNode(cellPos, AStarNodeType.Walkable);
                }
                break;
        }
    }



    public List<AStarNode> FindPath(Vector3 startPos, Vector3 endPos)
    {


        return new List<AStarNode>();
    }
}
