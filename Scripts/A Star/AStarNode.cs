using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyEnums;
public class AStarNode
{
    private int x;
    private int y;

    private float cost;
    private float disToBeg;
    private float disToEnd;
    private AStarNode fatherNode;
    private AStarNodeType nodeType;

    public AStarNode(Vector3Int nodePos, AStarNodeType nodeType)
    {
        this.x = nodePos.x;
        this.y = nodePos.y;
        this.nodeType = nodeType;
    }

    public int GetX()
    {
        return x;
    }

    public int GetY()
    {
        return y;
    }

    public float GetCost()
    {
        return cost;
    }

    public float GetDisToBeg()
    {
        return disToBeg;
    }

    public float GetDisToEnd()
    {
        return disToEnd;
    }

    public AStarNode GetFatherNode()
    {
        return fatherNode;
    }

    public AStarNodeType GetNodeType()
    {
        return nodeType;
    }

    public void SetNodeType(AStarNodeType newType)
    {
        if (CanOverride(nodeType, newType))
        {
            nodeType = newType;
        }
    }
    public void SetFatherNode(AStarNode fatherNode)
    {
        this.fatherNode = fatherNode;
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
