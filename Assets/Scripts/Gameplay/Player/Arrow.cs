using System.Collections;
using UnityEngine;

public class Arrow : MonoBehaviour, IPoolable
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite buriedSprite;

    [SerializeField] private float lifeSpan = 1;
    [SerializeField] private float speed = 2;
    [SerializeField] private int damage = 1;

    private Vector2 direction = Vector2.right;
    private Sprite originalSprite;
    private ObjectPool<Arrow> sourcePool;
    private Coroutine lifeCoroutine;

    /// <summary>由 PlayerBow 在建池后注入，箭矢到期/回收时经它归还池中</summary>
    public void SetSourcePool(ObjectPool<Arrow> pool) => sourcePool = pool;

    /// <summary>
    /// 发射箭矢：设置飞行方向、初速度、旋转角度。由外部（PlayerBow）在取件后调用一次。
    /// </summary>
    public void Launch(Vector2 direction)
    {
        this.direction = direction;
        rb.velocity = direction * speed;
        RotateArrow();
    }

    public void OnPoolGet()
    {
        // 每次取件都按当前玩家攻击力刷新伤害
        damage = StatsManager.Instance.GetDamage();
        if (originalSprite != null) spriteRenderer.sprite = originalSprite;
        transform.rotation = Quaternion.identity;
        transform.SetParent(null);

        if (lifeCoroutine != null) StopCoroutine(lifeCoroutine);
        lifeCoroutine = StartCoroutine(LifeTimer());
    }

    public void OnPoolReturn()
    {
        if (lifeCoroutine != null)
        {
            StopCoroutine(lifeCoroutine);
            lifeCoroutine = null;
        }
        rb.velocity = Vector2.zero;
        rb.isKinematic = false;
    }

    private void Awake()
    {
        originalSprite = spriteRenderer.sprite;
    }

    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(lifeSpan);
        lifeCoroutine = null;
        sourcePool?.Return(this);
    }

    private void RotateArrow()
    {
        float angle = Mathf.Atan2(direction.y, direction.x) *Mathf.Rad2Deg;//*Rad2Deg表示转换弧度制为角度制
        transform.rotation=Quaternion.Euler(0,0,angle);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 例外保留：碰撞对象运行时才知道是谁，无法预引用（见重构清单 GetComponent 治理一节）
        if ((enemyLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            if (collision.gameObject.TryGetComponent<IDamageable>(out var damageable))
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
