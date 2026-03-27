using UnityEngine;

public class PathFinderDetails
{
    private Vector3Int nodePos;
    private float cost;
    private float disToBeg;
    private float disToEnd;
    private PathFinderDetails fatherNode;

    public float GetCost() => cost;
    public float GetDisToBeg() => disToBeg;
    public float GetDisToEnd() => disToEnd;
    public PathFinderDetails FatherNode => fatherNode;
    public void SetFatherNode(PathFinderDetails fatherNode)
    {
        this.fatherNode = fatherNode;
    }
    public PathFinderDetails(Vector3Int nodePos, Vector3Int endPos, PathFinderDetails fatherNode)
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
                (CalDistance(nodePos, fatherNode.nodePos) >= 2 ? 1.414f : 1);
        }

        disToEnd = CalDistance(nodePos, endPos);
        cost = disToBeg + disToEnd;
    }

    private int CalDistance(Vector3Int startPos, Vector3Int endPos)
    {
        int distance = Mathf.Abs(endPos.x - startPos.x) + Mathf.Abs(endPos.y - startPos.y);
        return distance;
    }
}
