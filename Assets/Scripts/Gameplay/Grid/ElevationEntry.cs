using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevationEntry : MonoBehaviour
{
    [SerializeField] private Collider2D[] mountainColliders;
    [SerializeField] private Collider2D[] edgeColliders;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            foreach (Collider2D collider in mountainColliders)
            {
                collider.enabled = false;
            }
            foreach (Collider2D edge in edgeColliders)
            {
                edge.enabled = true;
            }

        }
        else return;
        // 例外保留：触发对方是玩家碰撞体，运行时才知道是谁；补判空防 NRE
        if (collision.gameObject.TryGetComponent<SpriteRenderer>(out var sr))
            sr.sortingOrder = 15;
    }
}
