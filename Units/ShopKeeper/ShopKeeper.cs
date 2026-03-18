using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopKeeper : MonoBehaviour
{
    public CanvasGroup shopCanvasGroup;
    public Animator animator;
    private bool playerInRange;

    void Start()
    {
        shopCanvasGroup.alpha = 0;
        shopCanvasGroup.interactable = false;
        shopCanvasGroup.blocksRaycasts = false;

    }

    void Update()
    {
        if(Input.GetButtonDown("Interact") && playerInRange)
        {
            if (shopCanvasGroup.alpha == 0)
            {
                Time.timeScale = 0; // ‘›Õ£”Œœ∑
                shopCanvasGroup.alpha = 1;
                shopCanvasGroup.interactable = true;
                shopCanvasGroup.blocksRaycasts = true;
            }
            else
            {
                Time.timeScale = 1; // ª÷∏¥”Œœ∑
                shopCanvasGroup.alpha = 0;
                shopCanvasGroup.interactable = false;
                shopCanvasGroup.blocksRaycasts = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Player"))
        {
            playerInRange = true;
            animator.SetBool("playerInRange", true);
        }
    }
    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.CompareTag("Player"))
        {
            playerInRange = false;
            animator.SetBool("playerInRange", false);
        }
    }
}
