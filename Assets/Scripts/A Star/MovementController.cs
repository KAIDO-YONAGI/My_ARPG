using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    private Stack<PathFinderDetails> path = null;
    private float threshold = 0.5f;
    private float pathRebuildDistance = .5f; // 目标移动超过此距离时重新寻路
    private float pathRebuildCooldown = .5f; // 寻路冷却时间
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
            float realPathRebuildDistance = pathRebuildDistance * GetCellSize();
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
    public float GetThreshold()
    {
        if (AStarNodeManager.instance == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("AStarNodeManager.instance is NULL!");
#endif

            return 0f;
        }

        return threshold * AStarNodeManager.instance.GetCellSize();
    }
    private bool FindWay(Vector3 optPos, Vector3 startPos, Vector3 endPos)
    {
        this.startPos = startPos;
        this.endPos = endPos;

        if (AStarPathFinder.instance != null)
            path = AStarPathFinder.instance.FindPath(optPos, startPos, endPos);

        hasValidPath = path != null && path.Count > 0;
        if (!hasValidPath)
        {
#if UNITY_EDITOR
            Debug.LogWarning("找不到路径！");//编辑器模式下警告，发布后不显示,减少性能浪费
#endif
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

/*
TODO(MovementController，小步重构建议)

这份控制器已经有“路径生产”和“移动消费”解耦的雏形了，后续更适合做
的是接口收口和状态整理，而不是一次性推倒重写。

1. 把“查询下一个点”和“推进内部状态”拆开
   - 当前 GetPosToGo() 同时做了计时器递减、寻路/重寻路、返回当前目标点。
   - 以后可以考虑拆成：
     TickPathState(deltaTime)
     EnsurePath(optPos, startPos, endPos)
     TryGetCurrentWaypoint(out Vector3 waypoint)
   - 目标：减少“查询接口带副作用”的情况，调试时更清楚。

2. 用显式状态替代 hasValidPath + path == null + path.Count == 0
   - 现在这几个条件组合起来才能判断当前状态，读起来有点分散。
   - 后续可以考虑引入：
     enum PathState { Idle, FollowingPath, RebuildCoolingDown, NoPath, Finished }
   - 目标：更容易区分“没路”“走完了”“冷却中”“正常跟随”。

3. 不要让外部同时负责“到达判定”和“节点消费”
   - 现在移动侧需要自己判断是否接近目标点，然后再手动调用 ArrivedPos()。
   - 以后可以改成：
     TryAdvanceIfReached(currentWorldPos)
     或 NotifyReached(currentWorldPos)
   - threshold 也可以由 controller 内部统一使用。
   - 目标：减少 Movement 和 Controller 之间的协议耦合。

4. 把重寻路策略从 ReFindWay() 里再拆清楚一点
   - 现在 ReFindWay() 同时负责：重新寻路、比较新旧路径、决定是否替换。
   - 以后可以拆成：
     ShouldRebuildPath(newTarget)
     ShouldAcceptNewPath(currentPath, newPath, target)
   - 目标：以后调“目标移动多远才重算”“新路径是否值得替换”时更好改。

5. 隐藏 Stack 的顺序约定
   - Peek()/Pop() 和 ToArray() 的顺序语义需要脑内一直换算，后面容易绕晕。
   - 后续可以考虑封装一个 PathCursor / PathBuffer，
     或直接换成 List + currentIndex。
   - 目标：让“当前节点”“下一个节点”“调试绘制顺序”更直观。

6. 把调参项改成 Inspector 可见配置
   - threshold / pathRebuildDistance / pathRebuildCooldown / showPath /
     pathColor / nodeRadius 都适合改成 [SerializeField] private。
   - 目标：以后调 AI 移动手感时不用反复改代码。

7. 把单例依赖集中到少数入口
   - 当前多个方法都在分别访问 AStarPathFinder.instance /
     AStarNodeManager.instance，并重复做空判断。
   - 后续可以整理成少量 helper/property。
   - 目标：减少重复空判断，也让报错位置更集中。

8. 长期方向：把 controller 和 movement 用事件或接口进一步解耦
   - controller 只负责提供 waypoint、管理路径状态、决定何时重建路径。
   - movement 只负责移动，并在到点时通知 controller。
   - 可以考虑的事件：
     OnPathBuilt / OnPathInvalid / OnWaypointConsumed / OnArrivedDestination
   - 目标：让“路径管理”和“实际位移”彻底分工。

建议顺序：
第一步：先拆开“查询”和“推进”
第二步：补一个显式 PathState
第三步：把到达判定收回 controller
第四步：再抽离重寻路策略和路径容器
第五步：最后再做事件化解耦
*/
