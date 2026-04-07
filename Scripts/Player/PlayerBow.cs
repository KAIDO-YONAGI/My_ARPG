using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;
using MyEnums;
public class PlayerBow : MonoBehaviour
{
    public Transform launchPoint;
    public GameObject arrowPrefab;
    private Vector2 aimDirection = Vector2.right;
    private Vector2 shootDirection = Vector2.right;
    public Animator anim;
    public PlayerMovement playerMovement;
    //TODO:删除animator调用

    private float shootTimer;//防止多箭发射
    private void OnEnable()
    {
        anim.SetLayerWeight(0, 0);//意为把序号零的层设为优先级零
        anim.SetLayerWeight(1, 1);
    }
    private void OnDisable()
    {
        anim.SetLayerWeight(0, 1);
        anim.SetLayerWeight(1, 0);
    }
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
        aimDirection = new Vector2(playerMovement.getFacingDirection(), 0).normalized;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal != 0 || vertical != 0)
        {
            aimDirection = new Vector2(horizontal, vertical).normalized;
            anim.SetFloat("aimX", aimDirection.x);
            anim.SetFloat("aimY", aimDirection.y);
        }
        else if (horizontal == 0 && vertical == 0)
        {
            anim.SetFloat("aimX", aimDirection.x);
        }
    }

    public void HandleShootingAiming()//用动画脚本单独触发并且记录射击方向，纠正射击和转向的时序问题(放置在第一帧)
                                      //新问题：瞄准要按方向键，否则会按照上一次的射击方向射击，而不是朝向,因此使用了playermovement脚本里的朝向
    {
        HandleAiming();
        playerMovement.AnimatorSM(PlayerState.Shooting);
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        if (horizontal != 0 || vertical != 0)
        {
            shootDirection = new Vector2(horizontal, vertical).normalized;
        }
        else
        {
            shootDirection = new Vector2(playerMovement.getFacingDirection(), 0).normalized;
        }
    }

    public void Shoot()//动画事件触发的函数，生成箭矢并且设置方向
    {
        if (shootTimer <= 0)
        {
            Arrow arrow = Instantiate(arrowPrefab, launchPoint.position, Quaternion.identity).GetComponent<Arrow>();//实例化箭矢，第二个参数为生成位置,第三个为单位向量（表示禁用旋转)
            arrow.direction = shootDirection;//先创建箭矢并且获取引用，然后修改方向
            shootTimer = StatsManager.instance.coolDown;

        }

    }
    public void ShootingDone()//动画事件触发的函数，结束射击，重置状态和计时器
    {
        playerMovement.AnimatorSM(PlayerState.Idle);
        playerMovement.animator.SetBool("isShooting", false);
        playerMovement.SetCanBeInterrupted(true);
        playerMovement.ResetTimer();
        StartCoroutine(ResetShooting(0.1f));
    }
    IEnumerator ResetShooting(float delay)
    {
        yield return new WaitForSeconds(delay);

        anim.SetFloat("aimX", 0);
        anim.SetFloat("aimY", 0);

    }
}

