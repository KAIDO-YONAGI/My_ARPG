using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyEnums;
public class AStarNode
{
    int x;
    int y;

    float cost;
    float disToBeg;
    float disToEnd;
    public AStarNode fatherNode;
    AStarNodeType nodeType;

    public AStarNode(int x,int y, AStarNodeType nodeType)
    {
        this.x=x;
        this.y=y;
        this.nodeType=nodeType;
    }
}
