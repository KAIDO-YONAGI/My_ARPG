using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopKeeper : MonoBehaviour
{
    public static ShopKeeper currentShopKeeper;
    public CanvasGroup shopCanvasGroup;
    public Animator animator;
    public ShopManager shopManager;
    public static event Action<ShopManager, bool> OnShopStateChanged;
    [SerializeField] private List<ShopItems> shopItems;
    [SerializeField] private List<ShopItems> shopWeapon;
    [SerializeField] private List<ShopItems> shopArmour;
    private bool playerInRange;

    private bool shopIsOpen = false;

    void Start()
    {
        shopIsOpen = false;

        if (shopCanvasGroup != null)
        {
            shopCanvasGroup.alpha = 0;
            shopCanvasGroup.interactable = false;
            shopCanvasGroup.blocksRaycasts = false;
        }
    }

    void Update()
    {
        if (Input.GetButtonDown("Interact") && playerInRange)
        {
            if (shopIsOpen)
            {
                TimeManager.instance.ResumeGame();
                OnShopStateChanged?.Invoke(shopManager, false);
                currentShopKeeper = null;
                shopCanvasGroup.alpha = 0;
                shopCanvasGroup.interactable = false;
                shopCanvasGroup.blocksRaycasts = false;
                shopIsOpen = false;
            }
            else
            {
                TimeManager.instance.PauseGame();
                OnShopStateChanged?.Invoke(shopManager, true);
                currentShopKeeper = this;

                shopCanvasGroup.alpha = 1;
                shopCanvasGroup.interactable = true;
                shopCanvasGroup.blocksRaycasts = true;
                shopIsOpen = true;
                OpenItemShop();
            }
        }
    }

    public void OpenItemShop()
    {
        shopManager.PopulateShopItems(shopItems);
    }

    public void OpenWeaponShop()
    {
        shopManager.PopulateShopItems(shopWeapon);
    }

    public void OpenArmourShop()
    {
        shopManager.PopulateShopItems(shopArmour);
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
