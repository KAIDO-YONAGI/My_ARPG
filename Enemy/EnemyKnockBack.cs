using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyKnockBack : MonoBehaviour
{
    private Rigidbody2D rb;
    private EnemyMovement enemyMovement;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyMovement = GetComponent<EnemyMovement>();
    }
    public void Knockback(Transform forceTransform, float knockBackForce, float stunTime, float knockBackTime)
    {
        enemyMovement.AnimatorSM(EnemyState.KnockBack);
        StartCoroutine(StunTimer(stunTime, knockBackTime));//用于启动协程
        Vector2 direction = (transform.position - forceTransform.position).normalized;
        rb.velocity = direction * knockBackForce;
    }
    IEnumerator StunTimer(float stunTime, float knockBackTime)
    {
        yield return new WaitForSeconds(knockBackTime);//表示让协程等待的时间
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(stunTime);
        enemyMovement.AnimatorSM(EnemyState.Idle);
    }
}
