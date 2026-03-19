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
        // 场景刷新时重置状态
        if (!GameManager.transitionData.hasPendingTransition)
        {
            shopIsOpen = false;
        }

        // 检查是否需要恢复商店状态（场景编号匹配）
        if (GameManager.transitionData.pendingShop.shopKeeper != null &&
            GameManager.transitionData.pendingShop.shopKeeper == this &&
            GameManager.transitionData.pendingShop.sceneIndex == gameObject.scene.buildIndex)
        {
            // 清空待恢复数据


            shopIsOpen = true;
            shopCanvasGroup.alpha = 1;
            shopCanvasGroup.interactable = true;
            shopCanvasGroup.blocksRaycasts = true;
            currentShopKeeper = this;
            OpenItemShop();
        }
        else
        {
            if (shopCanvasGroup != null)
            {
                shopCanvasGroup.alpha = 0;
                shopCanvasGroup.interactable = false;
                shopCanvasGroup.blocksRaycasts = false;
            }
        }
    }

    void Update()
    {
        if (Input.GetButtonDown("Interact") && playerInRange)
        {
            if (shopIsOpen)
            {
                TimeManager.Instance.ResumeGame();
                OnShopStateChanged?.Invoke(shopManager, false);
                currentShopKeeper = null;
                shopCanvasGroup.alpha = 0;
                shopCanvasGroup.interactable = false;
                shopCanvasGroup.blocksRaycasts = false;
                shopIsOpen = false;
            }
            else
            {
                TimeManager.Instance.PauseGame();
                OnShopStateChanged?.Invoke(shopManager, true);
                currentShopKeeper = this;

                // 保存待恢复的商店数据
                GameManager.transitionData.pendingShop = new PendingShopData
                {
                    sceneIndex = gameObject.scene.buildIndex,
                    shopKeeper = this
                };

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
