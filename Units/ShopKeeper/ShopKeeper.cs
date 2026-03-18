using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopKeeper : MonoBehaviour
{
    public CanvasGroup shopCanvasGroup;
    public Animator animator;
    private bool playerInRange;

    private bool shopIsOpen = false;
    void Start()
    {
        shopCanvasGroup.alpha = 0;
        shopCanvasGroup.interactable = false;
        shopCanvasGroup.blocksRaycasts = false;

    }

    void Update()
    {
        if (Input.GetButtonDown("Interact") && playerInRange)
        {

            if (shopIsOpen)
            {
                TimeManager.Instance.ResumeGame();
                shopCanvasGroup.alpha = 0;
                shopCanvasGroup.interactable = false;
                shopCanvasGroup.blocksRaycasts = false;
                shopIsOpen = false;
            }
            else
            {
                TimeManager.Instance.PauseGame();
                shopCanvasGroup.alpha = 1;
                shopCanvasGroup.interactable = true;
                shopCanvasGroup.blocksRaycasts = true;
                shopIsOpen = true;
            }
        }
    }
    public void OpenItemShop()
    {

    }

    public void OpenWeapnShop()
    {

    }
    public void OpenArmourShop()
    {

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
