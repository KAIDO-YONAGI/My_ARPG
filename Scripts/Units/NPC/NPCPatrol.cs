using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCPatrol : MonoBehaviour
{
    public Vector2[] patrolPoints;
    public Animator animator;
    public float speed = 2f;
    public float waitTime = 1f;

    private Rigidbody2D rb;
    private Vector2 currentTarget;
    private bool isPatrolling = true;
    private int currentPointIndex = 0;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            currentTarget = patrolPoints[0];
            animator.SetBool("isWalking", true);
        }
        else
        {
            isPatrolling = false;
            animator.SetBool("isWalking", false);
        }
    }

    private void Update()
    {

        if (!isPatrolling || patrolPoints.Length == 0)
            return;

        // 计算朝向目标的方向
        Vector2 direction = (currentTarget - rb.position).normalized;
        if ((direction.x > 0 && facingDirec < 0) || (direction.x < 0 && facingDirec > 0))
        {
            Flip();
        }
        // 施加速度
        rb.velocity = direction * speed;

        // 检查是否到达当前目标点
        if (Vector2.Distance(rb.position, currentTarget) < 0.1f)
        {
            // 到达目标点
            rb.velocity = Vector2.zero;
            animator.SetBool("isWalking", false);

            // 切换到下一个巡逻点
            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
            currentTarget = patrolPoints[currentPointIndex];

            // 等待后继续巡逻
            StartCoroutine(WaitAndContinue());
        }
    }
    int facingDirec = 1; // 1表示朝右，-1表示朝左
    private void Flip()
    {
        facingDirec *= -1;
        transform.localScale = new Vector3(-1 * transform.localScale.x, transform.localScale.y, transform.localScale.z);
        //这个向量应该乘-1而不是facingDirec，因为facingDirec仅记录当前的状态（朝向），可以为1
    }
    IEnumerator WaitAndContinue()
    {
        isPatrolling = false;
        yield return new WaitForSeconds(waitTime);

        if (patrolPoints.Length > 0)
        {
            isPatrolling = true;
            animator.SetBool("isWalking", true);
        }
    }
}