using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevationExit : MonoBehaviour
{
    [SerializeField] private Collider2D[] mountainColliders;
    [SerializeField] private Collider2D[] edgeColliders;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")//判断仅为玩家时才执行操作，防止因为怪物触发
        {
            foreach (Collider2D collider in mountainColliders)
            {
                collider.enabled = true;//恢复碰撞体积
            }
            foreach (Collider2D edge in edgeColliders)
            {
                edge.enabled = false;
            }

        }
        else return;
        // 例外保留：触发对方是玩家碰撞体，运行时才知道是谁；补判空防 NRE
        if (collision.gameObject.TryGetComponent<SpriteRenderer>(out var sr))
            sr.sortingOrder = 5;
    }
}
