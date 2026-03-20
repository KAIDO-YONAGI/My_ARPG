using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Loot : MonoBehaviour
{
    public ItemSO item;
    public SpriteRenderer sr;
    public Animator animator;
    public static event Action<ItemSO,int >OnItemLooted;
    public int quantity = 10;
    private bool canBePick=true;
    private void OnValidate()
    {
        if (item == null) return;
        UpdateAppearence();

    }
    public void Initialize(ItemSO item,int quantity)
    {
        canBePick = false;
        this.item = item;
        this.quantity = quantity;
        UpdateAppearence();
    }

    private void UpdateAppearence()
    {
        sr.sprite = item.icon;
        this.name = item.itemName;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")&&canBePick)
        {
            animator.Play("Pickup");
            OnItemLooted?.Invoke(item, quantity);
            Destroy(gameObject, .5f);
        }

    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canBePick = true;
        }
    }
}
