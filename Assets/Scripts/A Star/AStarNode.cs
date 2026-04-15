using UnityEngine;
using MyEnums;
public class AStarNode
{
    private float x;
    private float y;
    private AStarNodeType nodeType;
    Vector3 nodePos;

    public AStarNode(Vector3 nodePos, AStarNodeType nodeType)
    {
        x = nodePos.x;
        y = nodePos.y;
        this.nodePos = new Vector3(x, y, 0);
        this.nodeType = nodeType;
    }

    public float GetX() => x;
    public float GetY() => y;
    public Vector3 GetNodePos() => nodePos;
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
        // Obstacle 永远最高优先级
        if (newType == AStarNodeType.Obstacle) return true;

        // 已经是 Obstacle，不能被覆盖
        if (existing == AStarNodeType.Obstacle) return false;

        return true;
    }
}