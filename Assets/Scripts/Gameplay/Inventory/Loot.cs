using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
public class Loot : MonoBehaviour, ISaveable, IPoolable
{
    public ItemSO item;
    public SpriteRenderer sr;
    public Animator animator;
    public LootEventSO lootEvent;
    public int quantity;
    public bool canBePick = true;//防止丢弃拾取死循环
    public bool hasBeenPicked = false;//在对象池里标记是否被拾取，决定是否加载时刷新

    /// <summary>判定「位置被改动过」的阈值（平方），避免浮点噪声把没挪过的摆放也记成位移</summary>
    private const float MovedEpsilonSqr = 0.0001f;

    private ObjectPool<Loot> sourcePool;
    private DataDefinition dataDef;

    /// <summary>生成态基准位置：场景摆放位置（Awake）或掉落物本次出生位置（Initialize）。
    /// 存档只记录相对它的位移，没挪过就不写「已移动」，因此旧档不会把设计师
    /// 之后在场景里调整过的摆放拖回原位。</summary>
    private Vector3 baselinePosition;

    /// <summary>本次取件是一个全新掉落实体，需要把生成态基准重置到本次出生位置</summary>
    private bool needsNewBaseline;

    /// <summary>是否已挂进 DataManager。注册/注销在 Awake、OnPoolGet、OnPoolCreated、
    /// OnPoolReturn、OnDestroy 里都会走到，用标记保证幂等。</summary>
    private bool registered;

    /// <summary>由 InventoryManager 在取件后注入，拾取/回收时经它归还池中</summary>
    public void SetSourcePool(ObjectPool<Loot> pool) => sourcePool = pool;

    private void Awake()
    {
        dataDef = GetComponent<DataDefinition>();//自身组件一次性缓存
        baselinePosition = transform.position;//场景摆放位置即生成态基准
        // 场景摆放的 loot 靠这里进存档系统（它们没有池的 Get/Return 生命周期）；
        // 池化实例紧随其后由 OnPoolCreated 退回未注册态，不会混进存档。
        RegisterSelf();
        // 生成时对账：注册后立刻按生成时确定的身份套用存档里的「改动量」，
        // 而不是让存档反过来决定这个 loot 生在哪、是不是该出现。
        // 场景加载后的 OnAutoLoad 仍会再走一遍 LoadData，逻辑幂等。
        MatchSavedStateAtSpawn();
    }

    public void OnPoolGet()
    {
        // 池对象是 InventoryManager 的子物体，取件时要脱离父级变成场景根物体：
        // InventoryManager.DropLoot 需要把掉落物 MoveGameObjectToScene 到当前游戏场景
        // （好让它随场景卸载销毁），而该 API 只接受场景根物体，否则抛
        // ArgumentException: Gameobject is not a root in a scene。
        // 与 Arrow.OnPoolGet 的处理一致；Return 时会重新挂回池父级。
        transform.SetParent(null);

        // 取件 = 一个全新掉落实体：身份在这里确定，之后整个生命周期不再变
        AssignNewIdentity();
        needsNewBaseline = true;
        hasBeenPicked = false;//新实体尚未被拾取，避免继承池中上一个实体的状态
        RegisterSelf();
    }

    public void OnPoolReturn()
    {
        UnregisterSelf();//归还 = 移出存档系统，池内对象不参与存读档
    }

    /// <summary>由 ObjectPool 在实例化之后调用：此刻还没被取件，不是实体，退回自动注册。</summary>
    public void OnPoolCreated()
    {
        UnregisterSelf();
    }

    private void OnDestroy()
    {
        UnregisterSelf();
    }

    /// <summary>挂进 DataManager。幂等，且跳过不参与存档的 DataDefinition。</summary>
    private void RegisterSelf()
    {
        if (registered || DataManager.Instance == null) return;

        var dataId = GetDataID();
        if (dataId == null) return;
        if (dataId.persistentType != MyEnums.PersistentType.ReadWrite) return;
        if (string.IsNullOrEmpty(dataId.ID)) dataId.ID = System.Guid.NewGuid().ToString();

        registered = true;
        ISaveable saveable = this;
        saveable.RegisterSaveable();
    }

    /// <summary>移出 DataManager。幂等，DataManager 缺失时只复位标记。</summary>
    private void UnregisterSelf()
    {
        if (!registered) return;
        registered = false;

        if (DataManager.Instance == null) return;
        ISaveable saveable = this;
        saveable.UnRegisterSaveable();
    }

    /// <summary>取件时换发身份：旧 ID 的存档条目属于上一个实体，先清掉再换发，
    /// 否则新掉落物会继承上一个实体的拾取态。</summary>
    private void AssignNewIdentity()
    {
        var dataId = GetDataID();
        if (dataId == null) return;

        if (!string.IsNullOrEmpty(dataId.ID) && DataManager.Instance != null)
        {
            DataManager.Instance.RemoveLootRegistration(dataId.ID);
        }

        dataId.ID = System.Guid.NewGuid().ToString();
    }

    /// <summary>生成时对账：拿当前运行态存档数据按本对象身份套用一次。</summary>
    private void MatchSavedStateAtSpawn()
    {
        if (DataManager.Instance == null) return;
        LoadData(DataManager.Instance.GetData);
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

        if (needsNewBaseline)
        {
            // 全新实体的出生点就是本次生成位置，不算「被挪动过」
            baselinePosition = transform.position;
            needsNewBaseline = false;
        }

        // 身份不在这里重发：被捡起后原地重掉的同一个实体必须保留原 ID，
        // 否则它的存档条目会跟着 RemoveLootRegistration 一起消失（重进场景复活）。
        // 全新掉落的身份由 OnPoolGet 换发，见那边注释。
        UpdateAppearence();
        gameObject.SetActive(true);
        RegisterSelf();
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
        if (sourcePool != null)
            sourcePool.Return(this);//归还池中复用（含注销存档注册）
        else
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
        return dataDef != null ? dataDef : (dataDef = GetComponent<DataDefinition>());
    }

    public void SaveData(Data data)
    {
        if (data == null || data.lootsStatsDic == null) return;

        var dataId = GetDataID();
        if (dataId == null || string.IsNullOrEmpty(dataId.ID)) return;

        // 位置只有偏离生成态才算「改动量」；没挪过就只记拾取态，
        // 读档时以场景摆放为准，设计师改过的摆放不会被旧档拖回去。
        bool moved = (transform.position - baselinePosition).sqrMagnitude > MovedEpsilonSqr;
        LootStatus status = new LootStatus(transform.position, hasBeenPicked, moved);

        if (data.lootsStatsDic.ContainsKey(dataId.ID))//有这个ID就改
        {
            data.lootsStatsDic[dataId.ID] = status;
        }
        else//没ID的注册
        {
            data.lootsStatsDic.Add(dataId.ID, status);
        }

    }

    public void LoadData(Data data)
    {
        if (data == null) return;
        if (data.lootsStatsDic == null) return;

        var dataId = GetDataID();
        if (dataId == null || string.IsNullOrEmpty(dataId.ID)) return;

        if (data.lootsStatsDic.TryGetValue(dataId.ID, out LootStatus lootStatus) && lootStatus != null)
        {
            // 只有被挪动过才回写位置；否则保持场景生成态。
            // 注意不回写 baselinePosition：基准始终是「生成态」，
            // 否则下一次存档会把同一段位移记成「没挪过」。
            if (lootStatus.moved && lootStatus.position != null)
            {
                transform.position = lootStatus.position.ToVector3();
            }

            hasBeenPicked = lootStatus.hasBeenPicked;
        }
        else
        {
            // 存档里没有这条 = 从未被改动过，按场景生成态呈现
            hasBeenPicked = false;
        }

        if (hasBeenPicked)//disable里不删除索引，为的是这里能在load的时候设置active
        {
            canBePick = false;
            gameObject.SetActive(false);
        }
        else
        {
            canBePick = true;
            gameObject.SetActive(true);
        }

    }
}
