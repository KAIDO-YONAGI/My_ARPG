using System.Collections.Generic;
using UnityEngine;

public class ShopKeeper : MonoBehaviour
{
    public Animator logoAnimator;
    public Animator shopKeeperAnimator;

    [Header("Events")]
    public ShopLoadEventSO shopLoadEvent;
    public ToggleCanvasEventSO toggleShopCanvasEvent;

    [SerializeField] private List<ShopItems> shopItems;
    [SerializeField] private List<ShopItems> shopWeapon;
    [SerializeField] private List<ShopItems> shopArmor;

    private bool playerInRange;

    public Transform PortraitTarget => shopKeeperAnimator != null
        ? shopKeeperAnimator.transform
        : transform;

    private void OnEnable()
    {
        if (toggleShopCanvasEvent != null)
        {
            toggleShopCanvasEvent.toggleCanvasEvent += OnToggleShopCanvas;
        }
    }

    private void OnDisable()
    {
        if (toggleShopCanvasEvent != null)
        {
            toggleShopCanvasEvent.toggleCanvasEvent -= OnToggleShopCanvas;
        }

        CloseShopIfOpen();
    }

    private void OnToggleShopCanvas(bool state)
    {
        if (!playerInRange || !state || shopLoadEvent == null)
        {
            return;
        }

        shopLoadEvent.RaiseShopLoadRequest(
            shopItems,
            shopWeapon,
            shopArmor,
            PortraitTarget);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!collider.CompareTag("Player"))
        {
            return;
        }

        playerInRange = true;

        if (logoAnimator != null)
        {
            logoAnimator.SetBool("playerInRange", true);
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (!collider.CompareTag("Player"))
        {
            return;
        }

        playerInRange = false;

        if (logoAnimator != null)
        {
            logoAnimator.SetBool("playerInRange", false);
        }

        CloseShopIfOpen();
    }

    private void CloseShopIfOpen()
    {
        if (toggleShopCanvasEvent == null ||
            ShopManager.instance == null ||
            !ShopManager.instance.IsShopOpen)
        {
            return;
        }

        toggleShopCanvasEvent.RaiseToggleCanvasEvent(false);
    }
}
