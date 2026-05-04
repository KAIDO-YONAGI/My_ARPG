using System;
using System.Collections;
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
    public InventorySlotsStatsSO ShoppingRequest;
    public InventorySlotsStatsSO QuestRewardRequest;

    public LootEventSO lootEvent;


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
        lootEvent.LootEvent += OnItemLootedHandler;
        ShoppingRequest.InventoryUpdateRequestEvent += HandleShopping;
        QuestRewardRequest.InventoryUpdateRequestEvent += HandleQuestReward;

    }


    private void OnDisable()
    {
        lootEvent.LootEvent -= OnItemLootedHandler;
        ShoppingRequest.InventoryUpdateRequestEvent -= HandleShopping;
        QuestRewardRequest.InventoryUpdateRequestEvent -= HandleQuestReward;

    }

    private void HandleQuestReward(ItemSO item, int price, int amount)
    {
        UpdateInvetorySlots(item, amount);
    }

    private void OnItemLootedHandler(ItemSO item, int quantity, Loot lootObj)
    {
        UpdateInvetorySlots(item, quantity, lootObj);
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


    private void UpdateInvetorySlots(ItemSO item, int quantity, Loot lootObj = null)
    {
        //金币
        if (item.isGold)
        {
            goldAmount += quantity;
            ItemHistoryManager.instance.RecordItem(item, quantity);

            amountText.text = goldAmount.ToString();
            lootObj?.MarkAsDestroyed();
            return;
        }
        if (item.isEXP)
        {
            ExpManager.instance.GainExp(quantity);
            return;
        }
        //普通物品
        if (quantity < 0)//物品出售
        {
            if (slotBeenClicked == null)
            {
                Debug.Log("No slot been Marked");
            }
            else if (slotBeenClicked.quantity > 0)
            {
                slotBeenClicked.quantity += quantity;
                ItemHistoryManager.instance.RecordItem(item, quantity);

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
                    ItemHistoryManager.instance.RecordItem(item, amount);

                    slot.UpdateUI();

                    if (quantity <= 0)
                    {
                        lootObj?.MarkAsDestroyed();
                        return;
                    }

                }
            }

            foreach (InventorySlot slot in itemSlots)//寻找可堆叠的格子
            {
                if (slot.itemSO == null)
                {
                    int amount = Mathf.Min(item.stackableSize, quantity);

                    slot.itemSO = item;
                    slot.quantity = amount;
                    ItemHistoryManager.instance.RecordItem(item, amount);

                    slot.UpdateUI();
                    lootObj?.MarkAsDestroyed();
                    return;
                }
            }

            DropLoot(item, quantity, lootObj);//减剩下的quantity丢掉
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
    private void DropLoot(ItemSO item, int quantity, Loot existingLoot = null)
    {
        if (existingLoot != null)
        {
            // 直接位移现有loot对象到玩家脚下
            existingLoot.transform.position = player.position;
            existingLoot.Initialize(item, quantity);
            existingLoot.sr.enabled = true;
            existingLoot.gameObject.SetActive(true);
            StartCoroutine(ResetLootState(existingLoot));
        }
        else
        {
            var sceneChanger = FindObjectOfType<SceneChanger>();
            Scene currentScene = sceneChanger != null ? sceneChanger.GetCurrentScene() : SceneManager.GetActiveScene();
            GameObject lootObj = Instantiate(lootPrefab, player.position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(lootObj, currentScene);
            Loot loot = lootObj.GetComponent<Loot>();
            loot.Initialize(item, quantity);
        }
    }
    private IEnumerator ResetLootState(Loot loot)
    {
        yield return new WaitForFixedUpdate();
        AnimatorStateInfo stateInfo = loot.animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(stateInfo.length * 0.3f);
        loot.canBePick = true;
        loot.hasBeenPicked = false;
        loot.animator.SetBool("isPicked", false);
    }
    public void SetSlotBeenClicked(InventorySlot slot)
    {
        slotBeenClicked = slot;
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
            ItemHistoryManager.instance.RecordItem(slot.itemSO, -1);

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
