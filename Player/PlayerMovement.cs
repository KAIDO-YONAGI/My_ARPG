using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody2D rb;//���������unity���϶���
    public Animator animator;//����״̬������ͬ��
    public PlayerCombat playerCombat;
    public PlayerBow playerBow;

    private int facingDirection = 1;//Ĭ�ϳ���Ϊ��
    private bool canBeInterrupted = true;//�Ƿ���Ա���ϣ���������������ڼ䲻�ɱ����

    private float timer=0;//��ʱ������ʱδʹ��        
    public enum PlayerState
    {

        Idle,
        Running,
        Attacking,
        Shooting,
        KnockBack

    }
    private PlayerState playerState = PlayerState.Idle;

    public void AnimatorSM(PlayerState newState)//�����л�����
    {
        //�˳���ǰ����

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
        //����״̬
        playerState = newState;
        //�����¶���
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
        if(timer >= 0)
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

        if (Input.GetButtonDown("Slash") && playerCombat.enabled&&timer<0)
        {
            AnimatorSM(PlayerState.Attacking);
        }
        else if (Input.GetButtonDown("Shoot") && playerBow.enabled&&timer<0)
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

        switch (playerState)//状态处理机
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
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (horizontal * transform.localScale.x < 0)
            Flip();

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

