using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody2D rb;//刚体对象，在unity中拖动绑定
    public Animator animator;//动画状态机对象，同上
    public PlayerCombat playerCombat;
    public PlayerBow playerBow;

    private int facingDirection = 1;//默认朝向为右
    private bool canBeInterrupted = true;//是否可以被打断，攻击和射击动画期间不可被打断

    private float timer = 0;//计时器，暂时未使用        
    public enum PlayerState
    {

        Idle,
        Running,
        Attacking,
        Shooting,
        KnockBack

    }
    private PlayerState playerState = PlayerState.Idle;

    public void AnimatorSM(PlayerState newState)//用于切换动画
    {
        //退出当前动画

        if (playerState == PlayerState.Attacking)
        {
            animator.SetBool("isAttacking", false);
        }
        else if (playerState == PlayerState.Shooting)
        {
            animator.SetBool("isShooting", false);
        }
        else if (playerState == PlayerState.Running)
        {
            animator.SetBool("isRunning", false);
        }
        // else if (playerState == PlayerState.Idle)
        // {
        // }
        //更新状态
        playerState = newState;
        //进入新动画
        if (playerState == PlayerState.Attacking)
        {
            animator.SetBool("isAttacking", true);
        }
        else if (playerState == PlayerState.Shooting)
        {
            animator.SetBool("isShooting", true);
        }
        else if (playerState == PlayerState.Running)
        {
            animator.SetBool("isRunning", true);
        }
        else if (playerState == PlayerState.KnockBack)
        {
        }

    }

    private void Update()
    {
        if (timer >= 0)
            timer -= Time.deltaTime;

        if (!canBeInterrupted)
            return;
        else MovementSM();
    }
    private void MovementSM()
    {
        // 如果处于KnockBack状态，不处理其他状态转换
        if (playerState == PlayerState.KnockBack)
        {
            HandleKnockBackState();
            return;
        }
        if (Input.GetButtonDown("Slash") && playerCombat.enabled && timer < 0)
        {
            AnimatorSM(PlayerState.Attacking);
        }
        else if (Input.GetButtonDown("Shoot") && playerBow.enabled && timer < 0)
        {
            AnimatorSM(PlayerState.Shooting);
        }
        else if (IsToRunning())
        {
            AnimatorSM(PlayerState.Running);
        }
        else if (!IsToRunning())
        {
            AnimatorSM(PlayerState.Idle);
        }

        switch (playerState)//用于执行逻辑
        {
            case PlayerState.Idle:
                HandleIdleState();
                break;
            case PlayerState.Running:
                HandleRunningState();
                break;
            case PlayerState.Attacking:
                HandleAttackingState();
                break;
            case PlayerState.Shooting:
                HandleShootingState();
                break;
            case PlayerState.KnockBack:
                HandleKnockBackState();
                break;

        }
    }
    public void SetCanBeInterrupted(bool value)
    {
        canBeInterrupted = value;
    }
    public void ResetTimer()
    {
        timer = StatsManager.Instance.coolDown;
    }
    private bool IsToRunning()
    {
        return Mathf.Abs(Input.GetAxis("Horizontal")) > 0 || Mathf.Abs(Input.GetAxis("Vertical")) > 0;
    }

    private void HandleKnockBackState()
    {
        canBeInterrupted = false;
    }

    private void HandleShootingState()
    {
        SetMovement(0, 0);
        canBeInterrupted = false;
        playerBow.HandleAiming();
    }

    private void HandleAttackingState()
    {
        SetMovement(0, 0);
        canBeInterrupted = false;
    }

    private void HandleIdleState()
    {
        SetMovement(0, 0);
    }

    private void HandleRunningState()
    {
        //获取输入值，正负代表方向
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        //判断（仅）水平输入值和当前角色朝向的符号是否一致，否（意味着玩家将要转向）则调用翻转
        if (horizontal * transform.localScale.x < 0)
            Flip();

        //将animator的horizontal参数的值设定为变量的值
        SetMovement(horizontal, vertical);
    }
    private void SetMovement(float horizontal, float vertical)
    {
        rb.velocity = new Vector2(horizontal, vertical) * StatsManager.Instance.speed;
    }

    private void Flip()
    {
        facingDirection *= -1;
        transform.localScale =
            new Vector3(-1 * transform.localScale.x,
            transform.localScale.y,
            transform.localScale.z);
    }
    public int getFacingDirection()
    {
        return this.facingDirection;
    }
    public void KonckBack(Transform enemy, float force, float stunTime)
    {
        playerState = PlayerState.KnockBack;
        Vector2 direction = (transform.position - enemy.position).normalized;

        Vector2 knockBackVelocity = direction * force;
        SetMovement(knockBackVelocity.x, knockBackVelocity.y);

        StartCoroutine(KnockBackCounter(stunTime));
    }
    public PlayerState GetPlayerState()
    {
        return playerState;
    }
    IEnumerator KnockBackCounter(float stunTime)
    {
        yield return new WaitForSeconds(stunTime);
        rb.velocity = Vector2.zero;
        canBeInterrupted = true;
        AnimatorSM(PlayerState.Idle);
    }

}

