using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public class InventoryManager : YSingleton<InventoryManager>
{

    [SerializeField] private Transform hotbarParent;
    [SerializeField] private Transform backpackParent;
    private List<InventorySlot> inventorySlotsList = new();
    [SerializeField] private UseItem useItem;
    [SerializeField] private TMP_Text goldAmountText;
    [SerializeField] private Loot lootPrefab;
    [SerializeField] private Transform player;
    [Header("Loot Pool")]
    [SerializeField] private int lootPrewarm = 4;
    [SerializeField] private int lootPoolMaxSize = 20;
    private ObjectPool<Loot> lootPool;

    public int GoldAmount => goldAmount;
    private int goldAmount;


    [Header("Events")]
    [SerializeField] private InventorySlotsStatsSO ShoppingRequest;
    [SerializeField] private InventorySlotsStatsSO QuestRewardRequest;

    [SerializeField] private LootEventSO lootEvent;


    private InventorySlot slotBeenClicked;


    private void Start()
    {

        inventorySlotsList.AddRange(hotbarParent.GetComponentsInChildren<InventorySlot>());
        inventorySlotsList.AddRange(backpackParent.GetComponentsInChildren<InventorySlot>());

        foreach (InventorySlot slot in inventorySlotsList)
        {
            slot.UpdateUI();
        }

        lootPool = new ObjectPool<Loot>(lootPrefab, lootPrewarm, transform, lootPoolMaxSize);
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
        UpdateInventorySlots(item, amount);
    }

    private void OnItemLootedHandler(ItemSO item, int quantity, Loot lootObj)
    {
        UpdateInventorySlots(item, quantity, lootObj);
    }
    private void HandleShopping(ItemSO item, int price, int amount)
    {
        if (item == null || goldAmount < price) return;
        else if (amount > 0)//购买
        {
            if (HasSpaceForItem(item))
            {
                UpdateGold(price);
                UpdateInventorySlots(item, amount);
            }
        }
        else if (amount < 0)//出售
        {
            UpdateGold(price);
            UpdateInventorySlots(item, amount);
        }
    }


    private void UpdateInventorySlots(ItemSO item, int quantity, Loot lootObj = null)
    {
        //金币
        if (item.isGold)
        {
            goldAmount += quantity;
            ItemHistoryManager.Instance.RecordItem(item, quantity);

            goldAmountText.text = goldAmount.ToString();
            lootObj?.MarkAsDisable();
            return;
        }
        if (item.isEXP)
        {
            ExpManager.Instance.GainExp(quantity);
            return;
        }
        //普通物品
        if (quantity < 0)//物品出售
        {
            if (slotBeenClicked == null)
            {
                Debug.Log("No slot been Marked");
            }
            else if (slotBeenClicked.Quantity > 0)
            {
                int removed = slotBeenClicked.RemoveItem(-quantity);
                ItemHistoryManager.Instance.RecordItem(item, -removed);
                return;
            }
        }
        else if (quantity > 0)//物品拾取以及购买
        {
            foreach (InventorySlot slot in inventorySlotsList)
            {
                if (slot.IsEmpty || slot.ItemSO == item)//空格子 或 可堆叠格子
                {
                    int placed = slot.AddItem(item, quantity);
                    if (placed > 0)
                    {
                        ItemHistoryManager.Instance.RecordItem(item, placed);
                        quantity -= placed;
                    }

                    if (quantity <= 0)
                    {
                        lootObj?.MarkAsDisable();
                        return;
                    }
                }
            }

            if (quantity > 0)
                DropLoot(item, quantity, lootObj);//减剩下的quantity丢掉
        }

    }
    private bool HasSpaceForItem(ItemSO item)
    {
        if (item == null) return false;
        foreach (var slot in inventorySlotsList)
        {
            if (slot.SpaceRemaining(item) > 0) return true;
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
            Scene currentScene = SceneChanger.Instance != null
                ? SceneChanger.Instance.GetCurrentScene()
                : SceneManager.GetActiveScene();
            Loot loot = lootPool.Get();
            if (loot == null)//池满兜底：一次性实例化，仍可归还池中复用
            {
                loot = Instantiate(lootPrefab, player.position, Quaternion.identity);
                // 兜底实例不经过池的 Get，补一次取件回调：Loot 在此确定自己的身份
                // 并挂进存档系统。否则它会沿用 prefab 的共享 ID，多个兜底实例
                // 会写进同一条存档记录互相覆盖。
                loot.OnPoolGet();
            }
            else
            {
                loot.transform.SetPositionAndRotation(player.position, Quaternion.identity);
            }
            loot.SetSourcePool(lootPool);
            // 掉落物随当前场景卸载销毁；池 Get 会跳过已销毁对象并在池空时走兜底
            SceneManager.MoveGameObjectToScene(loot.gameObject, currentScene);
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
        if (slot.ItemSO == null) return;
        DropLoot(slot.ItemSO, 1);
        slot.RemoveItem(1);
    }


    public void UseItem(InventorySlot slot)
    {
        if (slot.ItemSO != null && slot.Quantity > 0)
        {
            useItem.ApplyItemEffects(slot.ItemSO);//使用效果
            ItemSO used = slot.ItemSO;
            slot.RemoveItem(1);
            ItemHistoryManager.Instance.RecordItem(used, -1);
        }
    }
    public void UpdateGold(int price)
    {
        goldAmount -= price;
        goldAmountText.text = goldAmount.ToString();
    }

}
