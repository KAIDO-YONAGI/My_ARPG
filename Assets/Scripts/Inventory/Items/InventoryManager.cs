using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public class InventoryManager : MonoBehaviour
{
    private static InventoryManager _instance;
    public static InventoryManager Instance => _instance;

    [SerializeField] private Transform hotbarParent;
    [SerializeField] private Transform backpackParent;
    private List<InventorySlot> inventorySlotsList = new();
    [SerializeField] private UseItem useItem;
    [SerializeField] private TMP_Text goldAmountText;
    [SerializeField] private GameObject lootPrefab;
    [SerializeField] private Transform player;

    public int GoldAmount => goldAmount;
    private int goldAmount;


    [Header("Events")]
    [SerializeField] private InventorySlotsStatsSO ShoppingRequest;
    [SerializeField] private InventorySlotsStatsSO QuestRewardRequest;

    [SerializeField] private LootEventSO lootEvent;


    private InventorySlot slotBeenClicked;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Start()
    {

        inventorySlotsList.AddRange(hotbarParent.GetComponentsInChildren<InventorySlot>());
        inventorySlotsList.AddRange(backpackParent.GetComponentsInChildren<InventorySlot>());

        foreach (InventorySlot slot in inventorySlotsList)
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
