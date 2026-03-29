using UnityEngine;

public class PathFinderDetails
{
    private Vector3 nodePos;
    private float cost;
    private float disToBeg;
    private float disToEnd;
    private PathFinderDetails fatherNode;

    public float GetCost() => cost;
    public float GetDisToBeg() => disToBeg;
    public float GetDisToEnd() => disToEnd;
    public Vector3 GetNodePos() => nodePos;

    public PathFinderDetails GetFatherNode() => fatherNode;
    public void SetFatherNode(PathFinderDetails fatherNode)
    {
        this.fatherNode = fatherNode;
    }
    public PathFinderDetails(Vector3 nodePos, Vector3 endPos, PathFinderDetails fatherNode)
    {
        this.nodePos = nodePos;
        this.fatherNode = fatherNode;

        if (fatherNode == null)
        {
            disToBeg = 0;
        }
        else
        {
            disToBeg = fatherNode.disToBeg +
                CalDistance(nodePos, fatherNode.nodePos);
        }

        disToEnd = CalDistance(nodePos, endPos);
        cost = disToBeg + disToEnd;
    }

    private float CalDistance(Vector3 startPos, Vector3 endPos)
    {
        float distance = Mathf.Abs(endPos.x - startPos.x) + Mathf.Abs(endPos.y - startPos.y);
        return distance >= 2 ? 1.414f : 1;//曼哈顿距离。遇到大于二的就赋斜线值，精度更高
    }
}
