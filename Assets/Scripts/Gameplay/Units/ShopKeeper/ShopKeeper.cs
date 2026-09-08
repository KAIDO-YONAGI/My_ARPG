using System.Collections.Generic;
using UnityEngine;

public class ShopKeeper : MonoBehaviour
{
    [SerializeField] private Animator logoAnimator;
    [SerializeField] private Animator shopKeeperAnimator;
    [SerializeField] private ShopKeeperEventSO shopKeeperEvent;

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

        if (shopKeeperEvent != null)
            shopKeeperEvent.RaiseShopKeeperEntered(this);

        if (logoAnimator != null)
            logoAnimator.SetBool("playerInRange", true);
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (!collider.CompareTag("Player")) return;

        if (shopKeeperEvent != null)
            shopKeeperEvent.RaiseShopKeeperExited(this);

        if (logoAnimator != null)
            logoAnimator.SetBool("playerInRange", false);
    }

    private void OnDisable()
    {
        if (shopKeeperEvent != null)
            shopKeeperEvent.RaiseShopKeeperExited(this);
    }
}
