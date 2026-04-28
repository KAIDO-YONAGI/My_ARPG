using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    private void OnEnable()
    {
        toggleShopCanvasEvent.toggleCanvasEvent += OnToggleShopCanvas;
    }

    private void OnDisable()
    {
        toggleShopCanvasEvent.toggleCanvasEvent -= OnToggleShopCanvas;
    }
    
    private void OnToggleShopCanvas(bool state)
    {
        if (playerInRange&&state)
        {
            shopLoadEvent.RaiseShopLoadRequest(shopItems,shopWeapon,shopArmor);
        }
    }




    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Player"))
        {
            playerInRange = true;
            logoAnimator.SetBool("playerInRange", true);
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.CompareTag("Player"))
        {
            playerInRange = false;
            logoAnimator.SetBool("playerInRange", false);
        }
    }
}
