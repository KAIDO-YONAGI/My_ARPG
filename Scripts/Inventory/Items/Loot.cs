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
    private bool hasBeenPicked = false;
    [Header("Send")]
    public VoidEventSO saveDataEvent;
    public VoidEventSO loadDataEvent;

    private void Awake()
    {
        loadDataEvent?.OnEventRaised();
    }
    private void OnEnable()
    {
        ISaveable saveable = this;
        saveable.RegisterSaveable();//注册在需要保存的数据的列表中
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
        saveDataEvent?.OnEventRaised();

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
            hasBeenPicked = true;


            saveDataEvent.OnEventRaised();
            Destroy(gameObject, .5f);
            // LoadData();
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

    public void SaveData(Data data)
    {
        if (data == null || data.lootsStatsDic == null) return;

        var dataId = GetDataID();
        if (dataId == null) return;

        if (data.lootsStatsDic.ContainsKey(dataId.ID))
        {
            data.lootsStatsDic[dataId.ID] = (transform.position, hasBeenPicked);
        }
        else
        {
            data.lootsStatsDic.Add(dataId.ID, (transform.position, hasBeenPicked));
        }
    }

    public void LoadData(Data data)
    {
        if (data == null) return;
        if (data.lootsStatsDic == null) return;

        var dataId = GetDataID();
        if (dataId == null) return;

        if (data.lootsStatsDic.ContainsKey(dataId.ID))
        {
            transform.position = data.lootsStatsDic[dataId.ID].Item1;
            hasBeenPicked = data.lootsStatsDic[dataId.ID].Item2;
        }
        if (hasBeenPicked)
        {
            gameObject.SetActive(false);
        }
    }
}
