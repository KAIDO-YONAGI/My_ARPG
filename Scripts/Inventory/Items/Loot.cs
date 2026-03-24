using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class Loot : MonoBehaviour, ISaveable
{
    public ItemSO item;
    public SpriteRenderer sr;
    public Animator animator;
    public static event Action<ItemSO, int> OnItemLooted;
    public int quantity = 10;
    private bool canBePick = true;
    private void Awake()
    {
        var pos = GetComponent<Transform>().position;
        /*
        
        foreach(var position:DestoryLootsPosition)
        {
            if(pos==position){
                return;
            }
        }     
        
        
        */

    }
    private void OnEnable()
    {
        ISaveable saveable = this;
        saveable.RegisterSaveable();
    }

    private void OnDisable()
    {
        ISaveable saveable = this;
        saveable.UnRegisterSaveable();
    }

    private void OnValidate()
    {
        if (item == null) return;
        UpdateAppearence();

    }
    public void Initialize(ItemSO item, int quantity)
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
        if (collision.CompareTag("Player") && canBePick)
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

    public DataDefinition GetDataID()
    {
        return GetComponent<DataDefinition>();
    }

    public void GetSaveData(Data data)
    {
        throw new NotImplementedException();
    }

    public void LoadSaveData(Data data)
    {
        throw new NotImplementedException();
    }
}
