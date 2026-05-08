using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.IO;
using Newtonsoft.Json;
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
    private void Awake()
    {
        gameObject.SetActive(false);
        ISaveable saveable = this;
        saveable.RegisterSaveable();//注册在需要保存的数据的列表中
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
        }
    }

    public void MarkAsDisable()
    {
        hasBeenPicked = true;
        StartCoroutine(DisableAfterDelay(0.5f));
    }

    private IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
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
        if (!this) return null;
        return GetComponent<DataDefinition>();
    }

    public void SaveData(Data data)
    {
        if (data == null || data.lootsStatsDic == null) return;

        var dataId = GetDataID();
        if (dataId == null) return;

        if (data.lootsStatsDic.ContainsKey(dataId.ID))//有这个ID就改位置
        {
            data.lootsStatsDic[dataId.ID] = (transform.position, hasBeenPicked);
        }
        else//没ID的注册
        {
            data.lootsStatsDic.Add(dataId.ID, (transform.position, hasBeenPicked));
        }
        Dictionary<string, int> points = new Dictionary<string, int>
        {
            { "James", 9001 },
            { "Jo", 3474 },
            { "Jess", 11926 }
        };

        // 序列化并写入
        string json = JsonConvert.SerializeObject(points, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/save.json", json);
        Debug.Log("SaveRoute: " + Application.persistentDataPath + "/save.json");


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

        if (hasBeenPicked)//disable里不删除索引，为的是这里能在load的时候设置active
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
        }
        // 读取并反序列化
        string json = File.ReadAllText(Application.persistentDataPath + "/save.json");
        var loaded = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
        Debug.Log("loaded: " + loaded.ToString());

    }
}
