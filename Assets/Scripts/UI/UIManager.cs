using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else Destroy(gameObject);
        InitiateUICanvasList();
    }
    [Header("Events")]
    public SceneLoadEventSO loadEventSO;
    public List<ToggleCanvasEventSO> toggleCanvasEvents;


    [SerializeField] private GameObject fatherObjOfUICanvasNeedToReset;
    [SerializeField] private GameObject playerUI;
    [SerializeField] private List<CanvasGroup> integretedUICanvas = new();
    [SerializeField] private List<Button> integretedButtons = new();

    private MyEnums.CanvasToToggle CanvasToToggle = MyEnums.CanvasToToggle.Default;
    private List<CanvasGroup> UICanvasList = new();

    private void OnEnable()
    {
        loadEventSO.LoadRequestEvent += OnLoadScene;
    }
    private void OnDisable()
    {
        loadEventSO.LoadRequestEvent -= OnLoadScene;

    }
    private void OnLoadScene(GameSceneSO arg0, Vector3 arg1, bool arg2)
    {
        ResetCanvas();
    }
    private void Update()
    {

        ToggleCanvas();
    }
    // private void Start()
    // {
    //     IsToToggleCanvas();//初始化所有画布组
    // }


    //TODO 需要订阅一个空事件用于超出范围关闭画布组


    private void ToggleCanvas()
    {
        // 暂存本帧输入
        bool esc = Input.GetButtonDown("ESC");
        bool skillTree = Input.GetButtonDown("ToggleSkillTree");
        bool stats = Input.GetButtonDown("ToggleStats");
        bool npcInteract = Input.GetButtonDown("NPCInteract");
        bool questList = Input.GetButtonDown("OpenQuestList");
        bool interact = Input.GetButtonDown("Interact");

        // ESC：在非Default时统一关闭回到Default
        if (esc)
        {
            if (CanvasToToggle == MyEnums.CanvasToToggle.Default)
                CanvasToToggle = MyEnums.CanvasToToggle.ESC;
            else
                CanvasToToggle = MyEnums.CanvasToToggle.Default;

            IsToToggleCanvas(CanvasToToggle);
            return;
        }

        // 只有Default状态才能通过按键打开面板
        if (CanvasToToggle != MyEnums.CanvasToToggle.Default)
            return;

        if (skillTree) CanvasToToggle = MyEnums.CanvasToToggle.Skills;
        else if (stats) CanvasToToggle = MyEnums.CanvasToToggle.Stats;
        else if (npcInteract) CanvasToToggle = MyEnums.CanvasToToggle.Dialog;
        else if (questList) CanvasToToggle = MyEnums.CanvasToToggle.Quest;
        else if (interact) CanvasToToggle = MyEnums.CanvasToToggle.Shop;

        if (CanvasToToggle != MyEnums.CanvasToToggle.Default)
        {
            IsToToggleCanvas(CanvasToToggle);
        }
    }

    private void IsToToggleCanvas(MyEnums.CanvasToToggle target)
    {
        Debug.Log("isToTo");
        Debug.Log(target.ToString());

        foreach (var eventSO in toggleCanvasEvents)
        {
            if (eventSO.canvasToToggle == target)
            {
                eventSO.RaiseToggleCanvasEvent(true);
            }
            else
            {
                eventSO.RaiseToggleCanvasEvent(false);
            }
        }
    }
    private void InitiateUICanvasList()
    {
        // 遍历所有子对象
        foreach (Transform child in fatherObjOfUICanvasNeedToReset.transform)
        {
            CanvasGroup canvasGroup = child.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                UICanvasList.Add(canvasGroup);
            }
        }
    }
    private void ResetCanvas()
    {
        foreach (var canvas in UICanvasList)
        {
            if (canvas.GetComponent<CanvasGroup>() != null)
            {
                canvas.alpha = 0;
                canvas.interactable = false;
                canvas.blocksRaycasts = false;
            }
            else
            {
                Debug.LogWarning($"CanvasGroup component not found on {canvas.name}");
            }
        }
    }
}
