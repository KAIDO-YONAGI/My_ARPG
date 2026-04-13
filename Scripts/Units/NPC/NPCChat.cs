using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NPCChat : MonoBehaviour
{
    private Rigidbody2D rb;
    // public Animator moveAnimator;
    public Animator chatAnimator;
    public DialogSO dialogSO;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        DialogManager.instance.DisableButtons();
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

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))//左键点击
        {
            if (dialogSO != null && !DialogManager.instance.isDialogActive)
            {
                DialogManager.instance.StartDialog(dialogSO);
            }
            else
            {
                DialogManager.instance.AdvanceDialog();
            }
        }
    }

}
