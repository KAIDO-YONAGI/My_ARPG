using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.UIElements;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody2D rb;//刚体对象，在unity中拖动绑定
    public Animator animator;//动画状态机对象，同上
    public PlayerCombat playerCombat;
    public PlayerBow playerBow;

    private int facingDirection = 1;//默认朝向为右


    public enum PlayerState
    {

        Idle,
        Running,
        Attacking,
        Shooting,
        KnockBack

    }
    private PlayerState playerState = PlayerState.Idle;
    public PlayerState GetPlayerState()
    {
        return playerState;
    }
    public void ChangeState(PlayerState newState)
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
        }
        else if (playerState == PlayerState.KnockBack)
        {
        }
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
        }
        else if (playerState == PlayerState.KnockBack)
        {
        }

    }

    private void Update()
    {
        if (Input.GetButtonDown("Slash")&&playerCombat.enabled)
        {
            ChangeState(PlayerState.Attacking);
        }
        if (Input.GetButtonDown("Shoot")&&playerBow.enabled)
        {
            ChangeState(PlayerState.Shooting);
        }
        if (IsToRunning())
        {
            ChangeState(PlayerState.Running);
        }


        switch (playerState)
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
    private bool IsToRunning()
    {
        return Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
    }

    private void HandleKnockBackState()
    {

    }

    private void HandleShootingState()
    {
        rb.velocity = Vector2.zero;
        playerBow.HandleAiming();
    }

    private void HandleAttackingState()
    {
        rb.velocity = Vector2.zero;
        playerCombat.Attack();
    }

    private void HandleIdleState()
    {
        animator.Play("Idle");
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
        animator.SetFloat("horizontal", Mathf.Abs(horizontal));
        animator.SetFloat("vertical", Mathf.Abs(vertical));

        rb.velocity = new Vector2(horizontal, vertical) * StatsManager.Instance.speed;
    }


    void Flip()
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
        rb.velocity = direction * force;
        StartCoroutine(KnockBackCounter(stunTime));
    }

    IEnumerator KnockBackCounter(float stunTime)
    {
        yield return new WaitForSeconds(stunTime);
        rb.velocity = Vector2.zero;
    }

}

