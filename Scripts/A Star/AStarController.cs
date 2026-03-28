using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AStarController : MonoBehaviour
{
    private Stack<PathFinderDetails> path = null;
    private float cellSize;
    private float threshold = 0.1f;
    [SerializeField] private float pathRebuildDistance = 2f; // 目标移动超过此距离时重新寻路
    [SerializeField] private float pathRebuildCooldown = 0.5f; // 寻路冷却时间
    private float pathRebuildTimer; // 寻路计时器

    [Header("可视化设置")]
    [SerializeField] private bool showPath = true;
    [SerializeField] private Color pathColor = Color.yellow;
    [SerializeField] private Color startColor = Color.green;
    [SerializeField] private Color endColor = Color.red;
    [SerializeField] private float nodeRadius = 0.2f;

    private Vector3 startPos;
    private Vector3 endPos;
    private bool hasValidPath = false; // 是否有有效路径

    private void OnEnable()
    {
        if (AStarPathFinder.instance != null)
            cellSize = AStarPathFinder.instance.GetCellSize();
    }
    public Vector3 GetPosToGo(Vector3 startPos, Vector3 endPos)
    {
        // 更新计时器
        if (pathRebuildTimer > 0)
            pathRebuildTimer -= Time.deltaTime;

        // 检查是否需要重新寻路
        if (!hasValidPath || path == null || path.Count == 0)
        {
            FindWay(startPos, endPos);
        }
        else
        {
            // 检查目标是否移动超过阈值且冷却时间已过
            float distToTarget = (endPos - this.endPos).sqrMagnitude;
            if (distToTarget > pathRebuildDistance * pathRebuildDistance && pathRebuildTimer <= 0)
            {
                // 目标移动太远，重新寻路
                FindWay(startPos, endPos);
                pathRebuildTimer = pathRebuildCooldown;
            }
        }
        
        if (path != null && path.Count > 0)
        {
            return CellToWorld(path.Peek().GetNodePos());
        }
        else return Vector3.zero;
    }
    public void ArrivedPos()
    {
        if (path != null && path.Count > 0)
            path.Pop();
    }
    public void ResetPath()
    {
        path = null;
        startPos = Vector3.zero;
        endPos = Vector3.zero;
        hasValidPath = false;
    }
    public float GetThreshold() => threshold;
    private void FindWay(Vector3 startPos, Vector3 endPos)
    {
        this.startPos = startPos;
        this.endPos = endPos;

        if (AStarPathFinder.instance != null)
            path = AStarPathFinder.instance.FindPath(startPos, endPos);

        hasValidPath = path != null && path.Count > 0;
        if (!hasValidPath)
        {
            Debug.LogWarning("找不到路径！");
        }
    }

    private void OnDrawGizmos()
    {
        if (!showPath || path == null || path.Count == 0) return;

        // 复制路径数组避免修改原始Stack
        PathFinderDetails[] pathArray = path.ToArray();

        // 绘制起点
        Gizmos.color = startColor;
        Gizmos.DrawWireSphere(CellToWorld(WorldToCell(startPos)), nodeRadius);

        // 绘制终点
        Gizmos.color = endColor;
        Gizmos.DrawWireSphere(CellToWorld(WorldToCell(endPos)), nodeRadius);

        // 绘制路径节点（从后往前，ToArray返回的是栈顶到栈底）
        Gizmos.color = pathColor;
        for (int i = pathArray.Length - 1; i >= 0; i--)
        {
            Vector3 worldPos = CellToWorld(pathArray[i].GetNodePos());
            Gizmos.DrawWireSphere(worldPos, nodeRadius);
        }

        // 绘制路径连线
        Gizmos.color = pathColor;
        for (int i = 0; i < pathArray.Length - 1; i++)
        {
            Vector3 from = CellToWorld(pathArray[i].GetNodePos());
            Vector3 to = CellToWorld(pathArray[i + 1].GetNodePos());
            Gizmos.DrawLine(from, to);
        }
    }

    private Vector3Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int y = Mathf.FloorToInt(worldPos.y / cellSize);
        return new Vector3Int(x, y);
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
