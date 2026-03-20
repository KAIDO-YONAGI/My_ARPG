using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private Transform player;
    private Animator animator;

    private int facingDirec = 1;//敌人朝向
    private EnemyState enemyState;//存储敌人唯一的状态
    private float attackCoolDownTimer;//攻击间隔冷却计时器

    public int speed = 5;
    public float attackRange = 2;
    public float attackCoolDown = 1;//每次恢复计时会被赋给计时器
    public float playerDetectRange = 5;
    public Transform detectionPoint;//侦测点，可以代替OnCollisionEnter2D碰撞触发
    public LayerMask playerMask;//创建公共玩家层，在unity中完成绑定

    public void AnimatorSM(EnemyState newState)
    {
        //退出当前动画
        if (enemyState == EnemyState.Idle)
            animator.SetBool("isIdle", false);
        else if (enemyState == EnemyState.Chasing)
            animator.SetBool("isChasing", false);
        else if (enemyState == EnemyState.Attacking)
            animator.SetBool("isAttacking", false);
        //更新状态
        enemyState = newState;
        //进入新动画
        if (enemyState == EnemyState.Idle)
            animator.SetBool("isIdle", true);
        else if (enemyState == EnemyState.Chasing)
            animator.SetBool("isChasing", true);
        else if (enemyState == EnemyState.Attacking)
            animator.SetBool("isAttacking", true);
    }
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        AnimatorSM(EnemyState.Idle);//注意状态改变需要在找到animator之后才开始
    }
    private void Update()
    {
        if (enemyState != EnemyState.KnockBack)
        {
            CheckForPlayer();

            if (attackCoolDownTimer > 0)
            {
                attackCoolDownTimer -= Time.deltaTime;//减去了实际的时间，保证间隔一致，不会因为游戏帧数/刷新率而变化
            }
            if (enemyState == EnemyState.Chasing)
                Chase();
            else if (enemyState == EnemyState.Attacking)
                rb.velocity = Vector2.zero;
        }
    }

    private void CheckForPlayer()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(detectionPoint.position, playerDetectRange, playerMask);
        if (hits.Length > 0)
        {
            player = hits[0].transform;
            if (Vector2.Distance(player.position, transform.position) <= attackRange && attackCoolDownTimer <= 0)
            {
                AnimatorSM(EnemyState.Attacking);
                attackCoolDownTimer = attackCoolDown;//重置时间
                return;
            }
            else if (Vector2.Distance(player.position, transform.position) > attackRange && enemyState == EnemyState.Idle)
            {//if条件设置为只能从Idle状态进入chasing，这是因为攻击状态的末尾会执行状态切换，恰好可以作为一个保证攻击动画播放完成的条件
             //但是需要注意的是，如果以后有其它状态的相关动画，一定也要注意把状态切换添加到动画末尾的位置
                AnimatorSM(EnemyState.Chasing);
            }
            //攻击完成后再unity动画添加脚本ChangeState()切换到Idle状态
        }
        else
        {
            rb.velocity = Vector2.zero;
            AnimatorSM(EnemyState.Idle);
        }
    }

    void Chase()
    {

        if ((player.position.x - transform.position.x) * facingDirec < 0)
        {
            Flip();
        }
        Vector2 direction = (player.position - transform.position).normalized;
        //使用两者坐标相减创建一个向量，并且使用normalized归一化
        rb.velocity = direction * speed;

    }
    private void Flip()
    {
        facingDirec *= -1;
        transform.localScale = new Vector3(-1 * transform.localScale.x, transform.localScale.y, transform.localScale.z);
        //这个向量应该乘-1而不是facingDirec，因为facingDirec仅记录当前的状态（朝向），可以为1
    }
    private void OnDrawGizmosSelected()//为查找角色的环着色
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(detectionPoint.position, playerDetectRange);
    }
}
public enum EnemyState
{
    Idle,
    Chasing,
    Attacking,
    KnockBack
}
