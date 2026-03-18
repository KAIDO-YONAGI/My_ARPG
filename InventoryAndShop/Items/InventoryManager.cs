using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
public class InventoryManager : MonoBehaviour
{
    public InventorySlot[] itemSlots;
    public UseItem useItem;
    public TMP_Text amountText;
    public GameObject lootPrefab;
    public Transform player;

    public int goldAmount;

    private void Start()
    {
        foreach (InventorySlot slot in itemSlots)
        {
            slot.UpdateUI();

        }
    }
    private void OnEnable()
    {
        Loot.OnItemLooted += AddItem;

    }
    private void OnDisable()
    {
        Loot.OnItemLooted -= AddItem;
    }
    public void AddItem(ItemSO item, int quantity)
    {
        if (item.isGold)
        {
            if (!amountText) return;
            goldAmount += quantity;
            amountText.text = goldAmount.ToString();
            return;
        }
        foreach (InventorySlot slot in itemSlots)//物品堆叠逻辑
        {
            if (slot.itemSO == item && slot.quantity < item.stackableSize)
            {
                int availableSize = item.stackableSize - slot.quantity;
                int amountToAdd = Mathf.Min(availableSize, quantity);

                slot.quantity += amountToAdd;
                quantity -= amountToAdd;

                slot.UpdateUI();

                if (quantity <= 0) return;

            }
        }

        foreach (InventorySlot slot in itemSlots)//寻找可堆叠的格子
        {
            if (slot.itemSO == null)
            {
                int amountToAdd = Mathf.Min(item.stackableSize, quantity);

                slot.itemSO = item;
                slot.quantity = amountToAdd;
                slot.UpdateUI();
                return;
            }
        }
        if (quantity > 0)
        {
            DropLoot(item, quantity);
        }

    }
    public void DropByClick(InventorySlot slot)
    {
        DropLoot(slot.itemSO, 1);
        slot.quantity -= 1;
        if(slot.quantity <= 0)
        {
            slot.itemSO=null;
        }
        slot.UpdateUI();
    }


    public void UseItem(InventorySlot slot)
    {
        if (slot.itemSO != null && slot.quantity >= 0)
        {
            useItem.ApplyItemEffects(slot.itemSO);//使用效果
            slot.quantity--;
            if (slot.quantity <= 0)
            {
                slot.itemSO = null;
            }
            slot.UpdateUI();
        }
    } 
    public void UpdateGold(int price)
    {
        goldAmount -= price;
        amountText.text = goldAmount.ToString();
    }
    private void DropLoot(ItemSO item, int quantity)
    {
        Loot loot = Instantiate(lootPrefab, player.position, Quaternion.identity).GetComponent<Loot>();
        loot.Initialize(item, quantity);

    }
}
