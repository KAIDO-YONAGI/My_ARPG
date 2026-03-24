using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public class InventoryManager : MonoBehaviour
{
    public InventorySlot[] itemSlots;
    public UseItem useItem;
    public TMP_Text amountText;
    public GameObject lootPrefab;
    public Transform player;

    public int goldAmount;


    [Header("Events")]
    public InventorySlotsStatsSO InventoryUpdateRequest;


    private InventorySlot slotBeenClicked;

    private void Start()
    {
        foreach (InventorySlot slot in itemSlots)
        {
            slot.UpdateUI();
        }
    }
    private void OnEnable()
    {
        Loot.OnItemLooted += UpdateInvetorySlots;
        InventoryUpdateRequest.InventoryUpdateRequestEvent += HandleShopping;

    }
    private void OnDisable()
    {
        Loot.OnItemLooted -= UpdateInvetorySlots;
        InventoryUpdateRequest.InventoryUpdateRequestEvent -= HandleShopping;
    }
    private void HandleShopping(ItemSO item, int price, int amount)
    {
        if (item == null || goldAmount < price) return;
        else if (amount > 0)//购买
        {
            if (HasSpaceForItem(item))
            {
                UpdateGold(price);
                UpdateInvetorySlots(item, amount);
            }
        }
        else if (amount < 0)//出售
        {
            UpdateGold(price);
            UpdateInvetorySlots(item, amount);
        }
    }


    private void UpdateInvetorySlots(ItemSO item, int quantity)
    {
        if (item.isGold)
        {
            if (!amountText) return;
            goldAmount += quantity;
            amountText.text = goldAmount.ToString();
            return;
        }
        if (quantity < 0)//物品出售
        {
            if (slotBeenClicked == null)
            {
                Debug.Log("No slot been Marked");
            }
            else if (slotBeenClicked.quantity>0)
            {
                slotBeenClicked.quantity += quantity;
                slotBeenClicked.UpdateUI();
                return;
            }
        }
        else if (quantity > 0)//物品拾取以及购买
        {

            foreach (InventorySlot slot in itemSlots)//物品堆叠逻辑
            {
                if (slot.itemSO == item && slot.quantity < item.stackableSize)
                {
                    int availableSize = item.stackableSize - slot.quantity;
                    int amount = Mathf.Min(availableSize, quantity);

                    slot.quantity += amount;
                    quantity -= amount;

                    slot.UpdateUI();

                    if (quantity <= 0) return;

                }
            }

            foreach (InventorySlot slot in itemSlots)//寻找可堆叠的格子
            {
                if (slot.itemSO == null)
                {
                    int amount = Mathf.Min(item.stackableSize, quantity);

                    slot.itemSO = item;
                    slot.quantity = amount;
                    slot.UpdateUI();
                    return;
                }
            }

            DropLoot(item, quantity);
        }

    }
    private bool HasSpaceForItem(ItemSO item)
    {
        foreach (var slot in itemSlots)
        {
            if ((slot.itemSO == item && slot.quantity < item.stackableSize)
                || slot.itemSO == null) return true;
        }
        return false;
    }
    private void DropLoot(ItemSO item, int quantity)
    {
        var sceneChanger = FindObjectOfType<SceneChanger>();
        Scene currentScene = sceneChanger != null ? sceneChanger.GetCurrentScene() : SceneManager.GetActiveScene();
        GameObject lootObj = Instantiate(lootPrefab, player.position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(lootObj, currentScene);
        Loot loot = lootObj.GetComponent<Loot>();
        loot.Initialize(item, quantity);
    }
    public void SetSlotBeenClicked(InventorySlot slot)
    {
        slotBeenClicked=slot;
    }
    public void DropByClick(InventorySlot slot)
    {
        DropLoot(slot.itemSO, 1);
        slot.quantity -= 1;
        if (slot.quantity <= 0)
        {
            slot.itemSO = null;
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

}
