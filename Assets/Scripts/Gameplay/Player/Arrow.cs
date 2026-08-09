using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite buriedSprite;

    [SerializeField] private float lifeSpan = 1;
    [SerializeField] private float speed = 2;
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockBackForce = 2;
    [SerializeField] private float knockBackTime = .2f;
    [SerializeField] private float stunTime = .2f;

    private Vector2 direction = Vector2.right;

    /// <summary>
    /// 发射箭矢：设置飞行方向、初速度、旋转角度。由外部（PlayerBow）在实例化后调用一次。
    /// </summary>
    public void Launch(Vector2 direction)
    {
        this.direction = direction;
        rb.velocity = direction * speed;
        RotateArrow();
    }

    private void Start()
    {
        Destroy(gameObject, lifeSpan);//destory方法的第二个参数表示对象生存时间/多久后销毁
        damage = StatsManager.Instance.GetDamage();
    }
    private void RotateArrow()
    {
        float angle = Mathf.Atan2(direction.y, direction.x) *Mathf.Rad2Deg;//*Rad2Deg表示转换弧度制为角度制
        transform.rotation=Quaternion.Euler(0,0,angle);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if ((enemyLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            var damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, transform);
                AttachToTarget(collision.gameObject.transform);
            }
        }
        else if((obstacleLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            AttachToTarget(collision.gameObject.transform);
        }

    }
    private void AttachToTarget(Transform target)
    {
        spriteRenderer.sprite=buriedSprite;//更改贴图
        rb.velocity = Vector3.zero;//让箭矢停止运动
        rb.isKinematic = true;//设置物体不在受到物理引擎的作用
        transform.SetParent(target);
    }
}
