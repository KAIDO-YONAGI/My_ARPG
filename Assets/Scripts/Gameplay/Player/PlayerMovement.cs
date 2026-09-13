using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using MyEnums;
using Gameplay.Player.Services;

public class PlayerMovement : MonoBehaviour
{
    private static readonly int IsAttacking = Animator.StringToHash("isAttacking");
    private static readonly int IsShooting = Animator.StringToHash("isShooting");
    private static readonly int IsRunning = Animator.StringToHash("isRunning");
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerBow playerBow;
    [SerializeField] private Joystick joystick;
    [SerializeField] private Transform visualRoot;

    [Header("Input Actions")] [SerializeField]
    private InputActionReference moveAction;

    [SerializeField] private InputActionReference slashAction;
    [SerializeField] private InputActionReference shootAction;

    [Header("Action Finished Events")] [SerializeField]
    private VoidEventSO slashActionFinishedEvent;

    [SerializeField] private VoidEventSO shootActionFinishedEvent;

    private int facingDirection = 1; //默认朝向为右
    private bool canBeInterrupted = true; //是否可以被打断，攻击和射击动画期间不可被打断

    private float timer = 0; //计时器，暂时未使用

    private PlayerState playerState = PlayerState.Idle;

    /// <summary>
    /// 玩家移动组件的静态定位器（唯一玩家）。SceneChanger/EnemyCombat 等经此访问，
    /// 免去 GetComponentInChildren 查找。注意：ForbidInput 禁用本组件不清空此引用
    /// （AllowInput 还要用），仅在 OnDestroy 销毁时清空。
    /// </summary>
    public static PlayerMovement Main { get; private set; }

    private void Awake()
    {
        Main = this;
    }

    private void OnDestroy()
    {
        if (Main == this) Main = null;
    }

    private void OnEnable()
    {
        canBeInterrupted = true;
        timer = 0;

        // 场景切换（ForbidInput/AllowInput）会禁用再启用 PlayerMovement，
        // 必须走 AnimatorSM 清掉残留动画布尔并清空速度，否则进新场景仍保持旧的移动状态
        AnimateMachine(PlayerState.Idle);
        if (rb != null) rb.velocity = Vector2.zero;

        moveAction.action.Enable();
        slashAction.action.Enable();
        shootAction.action.Enable();

        slashActionFinishedEvent.VoidEvent += OnActionFinished;
        shootActionFinishedEvent.VoidEvent += OnActionFinished;
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        slashAction.action.Disable();
        shootAction.action.Disable();

        slashActionFinishedEvent.VoidEvent -= OnActionFinished;
        shootActionFinishedEvent.VoidEvent -= OnActionFinished;
    }

    /// <summary>
    /// 统一的动作结束处理：回到 Idle、清除攻击/射击动画标记、解锁输入、重置冷却。
    /// 由近战结束、射击结束、切换装备三个来源通过 SO 事件触发。
    /// </summary>
    private void OnActionFinished()
    {
        AnimateMachine(PlayerState.Idle);
        animator.SetBool(IsAttacking, false);
        animator.SetBool(IsShooting, false);
        SetCanBeInterrupted(true);
        ResetTimer();
    }

    public void AnimateMachine(PlayerState newState) //用于切换动画
    {
        //退出当前动画

        if (playerState == PlayerState.Attacking)
        {
            animator.SetBool(IsAttacking, false);
        }
        else if (playerState == PlayerState.Shooting)
        {
            animator.SetBool(IsShooting, false);
        }
        else if (playerState == PlayerState.Running)
        {
            animator.SetBool(IsRunning, false);
        }

        // else if (playerState == PlayerState.Idle)
        // {
        // }
        //更新状态
        playerState = newState;
        //进入新动画
        if (playerState == PlayerState.Attacking)
        {
            animator.SetBool(IsAttacking, true);
        }
        else if (playerState == PlayerState.Shooting)
        {
            animator.SetBool(IsShooting, true);
        }
        else if (playerState == PlayerState.Running)
        {
            animator.SetBool(IsRunning, true);
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
        
        MovementSM();
    }

    private void MovementSM()
    {
        // 如果处于KnockBack状态，不处理其他状态转换
        if (playerState == PlayerState.KnockBack)
        {
            HandleKnockBackState();
            return;
        }

        bool attackPressed =
            (slashAction.action.WasPressedThisFrame() && playerCombat.IsActive) ||
            (shootAction.action.WasPressedThisFrame() && playerBow.IsActive);

        if (attackPressed && TryAttack())
        {
        }
        else if (IsToRunning())
        {
            AnimateMachine(PlayerState.Running);
        }
        else if (!IsToRunning())
        {
            AnimateMachine(PlayerState.Idle);
        }

        switch (playerState) //用于执行逻辑
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
        }
    }

    public bool TryAttack()
    {
        if (!isActiveAndEnabled || !canBeInterrupted || playerState == PlayerState.KnockBack || timer >= 0)
            return false;

        if (playerCombat.IsActive)
            AnimateMachine(PlayerState.Attacking);
        else if (playerBow.IsActive)
            AnimateMachine(PlayerState.Shooting);
        else
            return false;

        SetMovement(0, 0);
        canBeInterrupted = false;
        return true;
    }

    public void SetCanBeInterrupted(bool value)
    {
        canBeInterrupted = value;
    }

    public void ResetTimer()
    {
        timer = StatsService.Instance.Model.CoolDown;
    }

    private bool IsToRunning()
    {
        if (joystick != null && (Mathf.Abs(joystick.Horizontal) > 0.1f || Mathf.Abs(joystick.Vertical) > 0.1f))
            return true;
        Vector2 v = moveAction.action.ReadValue<Vector2>();
        return Mathf.Abs(v.x) > 0 || Mathf.Abs(v.y) > 0;
    }

    private void HandleKnockBackState()
    {
    }

    private void HandleShootingState()
    {
        SetMovement(0, 0);
        canBeInterrupted = false;
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
        float horizontal, vertical;
        if (joystick != null && (Mathf.Abs(joystick.Horizontal) > 0.1f || Mathf.Abs(joystick.Vertical) > 0.1f))
        {
            horizontal = joystick.Horizontal;
            vertical = joystick.Vertical;
        }
        else
        {
            Vector2 v = moveAction.action.ReadValue<Vector2>();
            horizontal = v.x;
            vertical = v.y;
        }

        //判断（仅）水平输入值和当前角色朝向的符号是否一致，否（意味着玩家将要转向）则调用翻转
        Transform root = GetPlayerRoot();
        if (horizontal * root.localScale.x < 0)
            Flip();

        //将animator的horizontal参数的值设定为变量的值
        SetMovement(horizontal, vertical);
    }

    private void SetMovement(float horizontal, float vertical)
    {
        rb.velocity = new Vector2(horizontal, vertical) * StatsService.Instance.Model.Speed;
    }

    private void Flip()
    {
        facingDirection *= -1;
        Transform root = GetPlayerRoot();
        root.localScale =
            new Vector3(-root.localScale.x,
                root.localScale.y,
                root.localScale.z);
    }

    public int GetFacingDirection()
    {
        return this.facingDirection;
    }

    public void KnockBack(Transform enemy, float force, float stunTime)
    {
        if (!isActiveAndEnabled)
            return;
        playerState = PlayerState.KnockBack;
        Vector2 direction = (GetPlayerRoot().position - enemy.position).normalized;

        rb.velocity = direction * force;

        StartCoroutine(KnockBackCounter(stunTime));
    }

    private Transform GetPlayerRoot()
    {
        if (visualRoot != null)
            return visualRoot;
        if (rb != null)
            return rb.transform;
        return transform;
    }

    public PlayerState GetPlayerState()
    {
        return playerState;
    }

    IEnumerator KnockBackCounter(float stunTime)
    {
        yield return new WaitForSeconds(stunTime);
        canBeInterrupted = true;
        AnimateMachine(PlayerState.Idle);
    }
}