using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NPCChat : MonoBehaviour
{
    private Rigidbody2D rb;
    // public Animator moveAnimator;
    public Animator chatAnimator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    private void OnEnable()
    {
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        chatAnimator.Play("Chat");
    }
    private void OnDisable()
    {
        rb.isKinematic = false;
        chatAnimator.Play("Idle");
    }
}
