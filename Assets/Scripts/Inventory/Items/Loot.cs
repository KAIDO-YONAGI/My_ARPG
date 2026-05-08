using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class Loot : MonoBehaviour, ISaveable
{
    public ItemSO item;
    public SpriteRenderer sr;
    public Animator animator;
    public LootEventSO lootEvent;
    public int quantity = 10;
    public bool canBePick = true;//防止丢弃拾取死循环
    public bool hasBeenPicked = false;//在对象池里标记是否被拾取，决定是否加载时刷新
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

        // 重新生成 GUID，避免与 prefab 或其他实例共享 ID
        var dataDef = GetComponent<DataDefinition>();
        if (dataDef != null)
        {
            dataDef.ID = System.Guid.NewGuid().ToString();
        }

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
            animator.SetBool("isPicked", true);
            lootEvent.OnEventRaised(item, quantity, this);
            hasBeenPicked = true;
            saveDataEvent.OnEventRaised();
        }
    }

    public void MarkAsDestroyed()
    {
        Destroy(gameObject, .5f);
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
