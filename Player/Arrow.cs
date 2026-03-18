using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    public Rigidbody2D rb;
    public Vector2 direction=Vector2.right;
    public LayerMask enemyLayer;
    public LayerMask obstacleLayer;
    public SpriteRenderer spriteRenderer;
    public Sprite buriedSprite;




    public float lifeSpwan = 1;
    public float speed = 2;
    public int damage = 1;
    public float  knockBackForce=2;
    public float  knockBackTime=.2f;
    public float  stunTime=.2f;

    private void Start()
    {

        rb.velocity = direction * speed;
        RotateArrow();
        Destroy(gameObject,lifeSpwan);//destory方法的第二个参数表示对象生存时间/多久后销毁

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
            collision.gameObject.GetComponent<EnemyHealth>().ChangeHealth(-damage);
            collision.gameObject.GetComponent<EnemyKnockBack>().Knockback(transform, knockBackForce, stunTime, knockBackTime);
            AttachToTarget(collision.gameObject.transform);
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
