using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopKeeper : MonoBehaviour
{
    public static ShopKeeper currentShopKeeper;//通过每次打开的商店来更新这个静态变量，确定要刷新的商店是哪一个
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
                OnShopStateChanged?.Invoke(shopManager, false);//为订阅者广播商店事件
                currentShopKeeper = null;
                shopCanvasGroup.alpha = 0;
                shopCanvasGroup.interactable = false;
                shopCanvasGroup.blocksRaycasts = false;
                shopIsOpen = false;
            }
            else//打开商店
            {
                TimeManager.Instance.PauseGame();
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
