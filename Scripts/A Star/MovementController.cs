using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    private Stack<PathFinderDetails> path = null;
    private float threshold = 0.5f;
    [SerializeField] private float pathRebuildDistance = .5f; // 目标移动超过此距离时重新寻路
    [SerializeField] private float pathRebuildCooldown = .5f; // 寻路冷却时间
    private float pathRebuildTimer; // 寻路计时器
    private bool showPath = true;
    private Color pathColor = Color.yellow;
    private Color startColor = Color.green;
    private Color endColor = Color.red;
    private float nodeRadius = 0.2f;
    private Vector3 startPos;
    private Vector3 endPos;
    private bool hasValidPath = false; // 是否有有效路径

    private float GetCellSize()
    {
        return AStarNodeManager.instance.GetCellSize();
    }
/// <summary>
/// 获取下一个目标点，自动处理路径重建逻辑
/// </summary>
/// <param name="optPos">可选，用来更好规划路径，避免动画鬼畜，不用就给Vector3.zero</param>
/// <param name="startPos"></param>
/// <param name="endPos"> 一直传入相同值即可</param>
/// <returns></returns> <summary>

//TODO:用事件系统解耦controller和movement，controller只负责提供路径点，movement负责移动和告知controller到达节点，并只由controller负责操作（重建）路径
    public Vector3 GetPosToGo(Vector3 optPos, Vector3 startPos, Vector3 endPos)
    {
        // 更新计时器
        if (pathRebuildTimer > 0)
            pathRebuildTimer -= Time.deltaTime;

        if (!hasValidPath || path == null || path.Count == 0)
        {
            if (!FindWay(optPos, startPos, endPos)) return Vector3.zero;
        }
        else
        {
            // 检查目标是否移动超过阈值且冷却时间已过
            float distToTarget = (endPos - this.endPos).sqrMagnitude;
            float realPathRebuildDistance=pathRebuildDistance*GetCellSize();
            if (distToTarget > realPathRebuildDistance * realPathRebuildDistance && pathRebuildTimer <= 0)
            {
                // 目标移动太远，重新寻路，并比较新旧路径
                ReFindWay(optPos, startPos, endPos);
                pathRebuildTimer = pathRebuildCooldown;
            }
        }

        if (path == null || path.Count == 0) return Vector3.zero;
        return CellToWorld(path.Peek().GetNodePos());
    }

    public void ArrivedPos()
    {
        if (path != null && path.Count > 0)
        {
            // lastNodeWorldPos = path.Peek().GetNodePos();
            path.Pop();
        }
    }
    public void ResetPath()
    {
        path = null;
        startPos = Vector3.zero;
        endPos = Vector3.zero;
        hasValidPath = false;
    }
    public float GetThreshold() => threshold*AStarNodeManager.instance.GetCellSize();
    private bool FindWay(Vector3 optPos, Vector3 startPos, Vector3 endPos)
    {
        this.startPos = startPos;
        this.endPos = endPos;

        if (AStarPathFinder.instance != null)
            path = AStarPathFinder.instance.FindPath(optPos, startPos, endPos);

        hasValidPath = path != null && path.Count > 0;
        if (!hasValidPath)
        {
            Debug.LogWarning("找不到路径！");
            return false;
        }
        return true;
    }

    // 重新寻路并优化路径选择
    private void ReFindWay(Vector3 optPos, Vector3 startPos, Vector3 endPos)
    {
        if (AStarPathFinder.instance == null)
        {
            path = null;
            hasValidPath = false;
            return;
        }

        // 获取当前路径
        PathFinderDetails[] currentPath = path.ToArray();

        // 重新寻路
        Stack<PathFinderDetails> newPath = AStarPathFinder.instance.FindPath(optPos, startPos, endPos);

        if (newPath == null || newPath.Count == 0)
        {
            Debug.Log("ReFindWay()找不到路径！");
            path = null;
            hasValidPath = false;
            return;
        }

        // 比较新旧路径的前两个节点
        PathFinderDetails[] newPathArray = newPath.ToArray();

        // 如果当前路径还有多个节点，比较新旧路径的第一个节点
        if (currentPath.Length >= 2 && newPathArray.Length >= 2)
        {
            Vector3 currentSecondNode = CellToWorld(currentPath[1].GetNodePos());
            Vector3 newFirstNode = CellToWorld(newPathArray[1].GetNodePos());

            float distCurrent = (currentSecondNode - endPos).sqrMagnitude;
            float distNew = (newFirstNode - endPos).sqrMagnitude;

            // 如果新路径的第一个节点更远，保留当前路径
            if (distCurrent <= distNew)
            {
                // 保持当前路径，只更新终点
                this.endPos = endPos;
                hasValidPath = true;
                return;
            }
        }

        // 使用新路径
        path = newPath;
        this.startPos = startPos;
        this.endPos = endPos;
        hasValidPath = true;
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

    private Vector3 WorldToCell(Vector3 worldPos)
    {

        return
        AStarPathFinder.instance != null ?
        AStarPathFinder.instance.WorldToCell(worldPos) : Vector3.zero;
    }
    // 网格坐标 → 世界坐标（中心点）
    private Vector3 CellToWorld(Vector3 cellPos)
    {
        return
        AStarPathFinder.instance != null ?
        AStarPathFinder.instance.CellToWorld(cellPos) : Vector3.zero;
    }

}
