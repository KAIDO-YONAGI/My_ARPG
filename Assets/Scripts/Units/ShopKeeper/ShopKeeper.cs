using System.Collections.Generic;
using UnityEngine;

public class ShopKeeper : MonoBehaviour
{
    public Animator logoAnimator;
    public Animator shopKeeperAnimator;

    [SerializeField] private List<ShopItems> shopItems;
    [SerializeField] private List<ShopItems> shopWeapon;
    [SerializeField] private List<ShopItems> shopArmor;

    public List<ShopItems> ShopItems => shopItems;
    public List<ShopItems> ShopWeapon => shopWeapon;
    public List<ShopItems> ShopArmor => shopArmor;

    public Transform PortraitTarget => shopKeeperAnimator != null
        ? shopKeeperAnimator.transform
        : transform;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!collider.CompareTag("Player")) return;

        if (ShopManager.instance != null)
            ShopManager.instance.RegisterActiveShopKeeper(this);

        if (logoAnimator != null)
            logoAnimator.SetBool("playerInRange", true);
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (!collider.CompareTag("Player")) return;

        if (ShopManager.instance != null)
        {
            ShopManager.instance.UnregisterActiveShopKeeper();
            if (ShopManager.instance.IsShopOpen)
                ShopManager.instance.CloseShop();
        }

        if (logoAnimator != null)
            logoAnimator.SetBool("playerInRange", false);
    }

    private void OnDisable()
    {
        if (ShopManager.instance != null)
        {
            if (ShopManager.instance.IsShopOpen)
                ShopManager.instance.CloseShop();
            ShopManager.instance.UnregisterActiveShopKeeper();
        }
    }
}
