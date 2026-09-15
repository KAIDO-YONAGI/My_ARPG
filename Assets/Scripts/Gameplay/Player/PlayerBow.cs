using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using MyEnums;
using Gameplay.Player.Services;
public class PlayerBow : MonoBehaviour
{
    private static readonly int AimX = Animator.StringToHash("aimX");
    private static readonly int AimY = Animator.StringToHash("aimY");

    [SerializeField] private Transform launchPoint;
    [SerializeField] private Arrow arrowPrefab;
    [Header("Arrow Pool")]
    [SerializeField] private int arrowPrewarm = 8;
    [SerializeField] private int arrowPoolMaxSize = 30;
    private ObjectPool<Arrow> arrowPool;
    private Vector2 aimDirection = Vector2.right;
    private Vector2 shootDirection = Vector2.right;
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerMovement playerMovement;

    private void Awake()
    {
        arrowPool = new ObjectPool<Arrow>(arrowPrefab, arrowPrewarm, transform, arrowPoolMaxSize);
    }

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;

    [Header("Action Finished Event")]
    [SerializeField] private VoidEventSO shootActionFinishedEvent;

    /// <summary>
    /// 当前是否为激活的武器模式。
    /// Player 根节点上的 PlayerAnimationEventRelay 会按此状态转发动画事件。
    /// </summary>
    public bool IsActive { get; private set; } = false;

    public void SetActive(bool active)
    {
        IsActive = active;
        // 动画层权重随武器切换：弓激活时弓层(1)全权，否则剑层(0)全权
        if (anim != null)
        {
            anim.SetLayerWeight(0, active ? 0 : 1);
            anim.SetLayerWeight(1, active ? 1 : 0);
        }
    }

    private void Start()
    {
        // 游戏开始时按初始武器状态设置动画层权重（默认 IsActive=false，即剑模式）
        SetActive(IsActive);
    }

    private float shootTimer;//防止多箭发射
    private void Update()
    {
        if (shootTimer >= 0)
        {
            shootTimer -= Time.deltaTime;
        }
    }

    public void HandleAiming()//为动画控制器触发的函数，用来配置动画条件触发，由状态机调用
    {
        if (shootTimer > 0)
        {
            return;
        }
        aimDirection = new Vector2(playerMovement.GetFacingDirection(), 0).normalized;

        // GetAxisRaw 对键盘返回 -1/0/1 离散值，Move action 的 WASD composite 同样是 -1/0/1，语义一致
        Vector2 move = moveAction.action.ReadValue<Vector2>();
        float horizontal = move.x;
        float vertical = move.y;

        if (horizontal != 0 || vertical != 0)
        {
            aimDirection = new Vector2(horizontal, vertical).normalized;
            anim.SetFloat(AimX, aimDirection.x);
            anim.SetFloat(AimY, aimDirection.y);
        }
        else if (horizontal == 0 && vertical == 0)
        {
            anim.SetFloat(AimX, aimDirection.x);
        }
    }

    public void HandleShootingAiming()//用动画脚本单独触发并且记录射击方向，纠正射击和转向的时序问题(放置在第一帧)
                                      //新问题：瞄准要按方向键，否则会按照上一次的射击方向射击，而不是朝向,因此使用了playermovement脚本里的朝向
    {
        HandleAiming();
        playerMovement.AnimateMachine(PlayerState.Shooting);
        Vector2 move = moveAction.action.ReadValue<Vector2>();
        float horizontal = move.x;
        float vertical = move.y;
        if (horizontal != 0 || vertical != 0)
        {
            shootDirection = new Vector2(horizontal, vertical).normalized;
        }
        else
        {
            shootDirection = new Vector2(playerMovement.GetFacingDirection(), 0).normalized;
        }
    }

    public void Shoot()//动画事件触发的函数，生成箭矢并且设置方向
    {
        if (shootTimer <= 0)
        {
            Arrow arrow = arrowPool.Get();
            if (arrow == null)//池满兜底：一次性实例化，仍可归还池中复用
                arrow = Instantiate(arrowPrefab, launchPoint.position, Quaternion.identity);
            else
                arrow.transform.SetPositionAndRotation(launchPoint.position, Quaternion.identity);
            arrow.SetSourcePool(arrowPool);
            arrow.Launch(shootDirection);//先取箭并获取引用，然后发射
            shootTimer = StatsService.Instance.Stats.CoolDown;//重置射击计时器，防止多箭发射

        }

    }
    public void ShootingDone()//动画事件触发的函数，结束射击，重置状态和计时器
    {
        // 通知 PlayerMovement（及其它订阅者）射击动作结束，由其统一重置状态
        shootActionFinishedEvent.OnEventRaised();
        StartCoroutine(ResetShooting(0.1f));
    }
    IEnumerator ResetShooting(float delay)
    {
        yield return new WaitForSeconds(delay);

        anim.SetFloat(AimX, 0);
        anim.SetFloat(AimY, 0);

    }
}

