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



    [SerializeField] private List<ShopItems> shopItems;
    [SerializeField] private List<ShopItems> shopWeapon;
    [SerializeField] private List<ShopItems> shopArmour;
    private bool playerInRange;
    private void OnEnable()
    {
        shopLoadEvent.ShopLoadEvent += OnShopLoad;
    }

    private void OnDisable()
    {
        shopLoadEvent.ShopLoadEvent -= OnShopLoad;

    }
    
    private void OnShopLoad(List<ShopItems> arg0, List<ShopItems> arg1, List<ShopItems> arg2)
    {
        throw new NotImplementedException();
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
