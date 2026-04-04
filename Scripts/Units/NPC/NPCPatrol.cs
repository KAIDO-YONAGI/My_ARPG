using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NPCPatrol : MonoBehaviour
{
    public int patrolRadius = 5;
    public Animator animator;
    public MovementController aStarController;

    public float speed = 2f;
    public float waitTime = 1f;
    private int facingDirec = 1; // 1表示朝右，-1表示朝左
    private Vector3 circleCenter;
    private Vector3 targetPosition;
    Vector3 posToGo;
    private Rigidbody2D rb;
    private bool isWaiting = false;
    float t = 0;
    //TODO: 完全去掉巡逻方框，使用单位向量随机函数随机取点和as寻路

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        circleCenter = transform.position;
        targetPosition = circleCenter + randomDirection();
        posToGo = aStarController.GetPosToGo(Vector3.zero, transform.position, targetPosition);
        t = aStarController.GetThreshold();
    }
    private void Update()
    {
        if (isWaiting)
            return;

        // 计算朝向目标的方向
        Vector2 direction = (posToGo - transform.position).normalized;
        if (direction.x * facingDirec < 0)
        {
            Flip();
        }
        if (posToGo != Vector3.zero)
            rb.velocity = direction * speed;
        else rb.velocity = Vector2.zero;


        Debug.Log(targetPosition);

        if ((transform.position - posToGo).sqrMagnitude < t * t)//到寻路节点则告知controller
        {
            aStarController.ArrivedPos();
            posToGo = aStarController.GetPosToGo(Vector3.zero, transform.position, targetPosition);
        }
        else
        {
            animator.SetBool("isWalking", true);
        }

        if ((transform.position - targetPosition).sqrMagnitude < t * t)//到终点则重新获取巡逻点
        {
            StartCoroutine(WaitAndContinue());
        }
    }
    private void Flip()
    {
        facingDirec *= -1;
        transform.localScale = new Vector3(-1 * transform.localScale.x, transform.localScale.y, transform.localScale.z);
        //这个向量应该乘-1而不是facingDirec，因为facingDirec仅记录当前的状态（朝向），可以为1
    }
    IEnumerator WaitAndContinue()
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);

        //如果没有路径了，说明被堵住了，换点找到有为止
        do
        {
            targetPosition = circleCenter + randomDirection();
            posToGo = aStarController.GetPosToGo(Vector3.zero, transform.position, targetPosition);
            Debug.Log($"[NPCPatrol] 计算新巡逻点 {targetPosition}，得到路径点 {posToGo}");
        } while (posToGo == Vector3.zero);


        isWaiting = false;
    }

    private Vector3 randomDirection()
    {
        Vector2 dir = Random.insideUnitCircle; // 均匀分布在圆内
        return new Vector3(dir.x, dir.y, 0) * patrolRadius;
    }
    private void OnDrawGizmos()
    {
        if (patrolRadius == 0)
            return;
        Gizmos.color = Color.yellow;

        // 编辑器模式（未运行）：圆圈跟随角色
        // 运行游戏后：圆圈固定在初始位置 circleCenter
        Vector3 drawPos = Application.isPlaying ? circleCenter : transform.position;
        Gizmos.DrawWireSphere(drawPos, patrolRadius);
    }
}