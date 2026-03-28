using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AStarController : MonoBehaviour
{
    private Stack<PathFinderDetails> path;
    private float cellSize;
    private float threshold = 0.1f;
    private void OnEnable()
    {
        if (AStarPathFinder.instance != null)
            cellSize = AStarPathFinder.instance.GetCellSize();
    }
    public Vector3 GetPosToGo(Vector3 startPos, Vector3 endPos)
    {
        if (path == null)
            FindWay(startPos, endPos);
        if (path.Count > 0)
            return CellToWorld(path.Peek().GetNodePos());
        else return Vector3.zero;
    }
    public void ArrivedPos()
    {
        if (path.Count > 0)
            path.Pop();
    }
    public float GetThreshold() => threshold;
    private void FindWay(Vector3 startPos, Vector3 endPos)
    {
        path = AStarPathFinder.instance.FindPath(startPos, endPos);

    }
    // 网格坐标 → 世界坐标（中心点）
    private Vector3 CellToWorld(Vector3Int cellPos)
    {
        return new Vector3(
            cellPos.x * cellSize + cellSize * 0.5f,
            cellPos.y * cellSize + cellSize * 0.5f,
            0
        );
    }

}
