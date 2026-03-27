using UnityEngine;
using MyEnums;
public class AStarNode
{
    private int x;
    private int y;
    private AStarNodeType nodeType;
    Vector3Int nodePos;

    public AStarNode(Vector3Int nodePos, AStarNodeType nodeType)
    {
        x = nodePos.x;
        y = nodePos.y;
        nodePos = new Vector3Int(x, y, 0);
        this.nodeType = nodeType;

    }

    public int GetX() => x;
    public int GetY() => y;
    public Vector3Int GetNodePos() => nodePos;
    public AStarNodeType GetNodeType() => nodeType;

    public void SetNodeType(AStarNodeType newType)
    {
        if (CanOverride(nodeType, newType))
        {
            nodeType = newType;
        }
    }

    private bool CanOverride(AStarNodeType existing, AStarNodeType newType)
    {
        // 障碍物不能被覆盖
        if (existing == AStarNodeType.Obstacle) return false;

        // 其他情况的覆盖规则
        switch (existing)
        {
            case AStarNodeType.Walkable:
                return newType != AStarNodeType.Obstacle;

            default:
                return true;
        }
    }
}