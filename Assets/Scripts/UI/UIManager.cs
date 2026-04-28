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
        if (CanvasToToggle == MyEnums.CanvasToToggle.Default && Input.GetButtonDown("ESC"))//默认界面ESC弹出ESC面板
        {
            CanvasToToggle = MyEnums.CanvasToToggle.ESC;
            IsToToggleCanvas();
        }
        else if (CanvasToToggle != MyEnums.CanvasToToggle.Default && Input.GetButtonDown("ESC"))//非默认界面则设为Default并且退出界面
        {
            CanvasToToggle = MyEnums.CanvasToToggle.Default;
            IsToToggleCanvas();
        }
        else if (CanvasToToggle == MyEnums.CanvasToToggle.Default)//默认状态才能按面板按钮
        {
            if (Input.GetButtonDown("ToggleSkillTree"))//打开技能树
            {
                CanvasToToggle = MyEnums.CanvasToToggle.Skills;
                IsToToggleCanvas();
            }
            else if (Input.GetButtonDown("ToggleStats"))//打开状态面板
            {
                CanvasToToggle = MyEnums.CanvasToToggle.Stats;
                IsToToggleCanvas();
            }
            else if (Input.GetButtonDown("NPCInteract"))//和NPC对话
            {
                CanvasToToggle = MyEnums.CanvasToToggle.Dialog;
                IsToToggleCanvas();
            }
            else if (Input.GetButtonDown("Interact"))//打开商店
            {
                CanvasToToggle = MyEnums.CanvasToToggle.Shop;
                IsToToggleCanvas();
            }
        }
    }

    private void IsToToggleCanvas()
    {

        foreach (var eventSO in toggleCanvasEvents)
        {
            if (eventSO.canvasToToggle == CanvasToToggle)
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
