using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AStarController : MonoBehaviour
{
    private Stack<PathFinderDetails> path = null;
    private float cellSize;
    private float threshold = 0.1f;
    private void OnEnable()
    {
        if (AStarPathFinder.instance != null)
            cellSize = AStarPathFinder.instance.GetCellSize();
    }
    public Vector3 GetPosToGo(Vector3 startPos, Vector3 endPos)
    {
        if (path == null||path.Count==0)
            FindWay(startPos, endPos);
        if (path != null && path.Count > 0)
            return CellToWorld(path.Peek().GetNodePos());
        else return Vector3.zero;
    }
    public void ArrivedPos()
    {
        if (path != null && path.Count > 0)
            path.Pop();
    }
    public void ResetPath()
    {
        path=null;
    }
    public float GetThreshold() => threshold;
    private void FindWay(Vector3 startPos, Vector3 endPos)
    {

        if (AStarPathFinder.instance != null)
            path = AStarPathFinder.instance.FindPath(startPos, endPos);
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("找不到路径！");
        }
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
