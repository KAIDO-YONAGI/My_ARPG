using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Events")]
    public SceneLoadEventSO loadEventSO;
    public List<ToggleCanvasEventSO> toggleCanvasEvents;//画布组对应的manager也会各自绑定他们的事件

    [SerializeField] private List<CanvasGroup> canvasToManage;
    [SerializeField] private GameObject playerUI;


    private MyEnums.CanvasToToggle currentCanvasState
        = MyEnums.CanvasToToggle.Default;
    private readonly List<CanvasGroup> uiCanvasList = new();
    private bool isAnyCanvasOpen;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

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
    private class UIInputState
    {
        public bool escClick;
        public bool skillTreeClick;
        public bool statsClick;
        public bool dialogClick;
        public bool questClick;
        public bool shopClick;
        public void Reset()
        {
            escClick = false;
            skillTreeClick = false;
            statsClick = false;
            dialogClick = false;
            questClick = false;
            shopClick = false;
        }
    }
    private UIInputState inputState = new();
    public void SetInput(MyEnums.CanvasToToggle canvas, bool state)
    //配合枚举类、封装类、状态机来提供外部操作的进入点
    {
        switch (canvas)
        {
            case MyEnums.CanvasToToggle.ESC:
                inputState.escClick = state;
                break;
            case MyEnums.CanvasToToggle.Skills:
                inputState.skillTreeClick = state;
                break;
            case MyEnums.CanvasToToggle.Stats:
                inputState.statsClick = state;
                break;
            case MyEnums.CanvasToToggle.Dialog:
                inputState.dialogClick = state;
                break;
            case MyEnums.CanvasToToggle.Quest:
                inputState.questClick = state;
                break;
            case MyEnums.CanvasToToggle.Shop:
                inputState.shopClick = state;
                break;
            
        }
    }
    public List<ToggleCanvasEventSO> GetToggleCanvasEventsList()
    {
        return toggleCanvasEvents;
    }
    private void Update()
    {
        ToggleCanvas();
    }


    private void ToggleCanvas()
    {
        inputState.escClick = inputState.escClick || Input.GetButtonDown("ESC");
        inputState.skillTreeClick = inputState.skillTreeClick || Input.GetButtonDown("ToggleSkillTree");
        inputState.statsClick = inputState.statsClick || Input.GetButtonDown("ToggleStats");
        inputState.dialogClick = inputState.dialogClick || Input.GetButtonDown("NPCInteract");
        inputState.questClick = inputState.questClick || Input.GetButtonDown("OpenQuestList");
        inputState.shopClick = inputState.shopClick || Input.GetButtonDown("Interact");

        isAnyCanvasOpen = IsAnyManagedCanvasOpen();
        //根据透明度判断是不是打开,可以利用这个来同步其它画布在其它地方打开的情况的状态

        if (!isAnyCanvasOpen && currentCanvasState != MyEnums.CanvasToToggle.Default)
        //这个状态组合说明面板被其它地方关了，因为此时不是default状态
        {
            currentCanvasState = MyEnums.CanvasToToggle.Default;
        }
        else if (!isAnyCanvasOpen)
        {
            currentCanvasState = MyEnums.CanvasToToggle.Default;
        }

        if (inputState.escClick)
        {
            currentCanvasState = isAnyCanvasOpen
                ? MyEnums.CanvasToToggle.Default
                : MyEnums.CanvasToToggle.ESC;

            IsToToggleCanvas(currentCanvasState);
            inputState.Reset();
            return;
        }

        if (inputState.skillTreeClick)
            currentCanvasState = MyEnums.CanvasToToggle.Skills;
        else if (inputState.statsClick)
            currentCanvasState = MyEnums.CanvasToToggle.Stats;
        else if (inputState.dialogClick)
            currentCanvasState = MyEnums.CanvasToToggle.Dialog;
        else if (inputState.questClick)
            currentCanvasState = MyEnums.CanvasToToggle.Quest;
        else if (inputState.shopClick)
            currentCanvasState = MyEnums.CanvasToToggle.Shop;
        else
            currentCanvasState = MyEnums.CanvasToToggle.Default;

        if (currentCanvasState != MyEnums.CanvasToToggle.Default)
        {
            IsToToggleCanvas(currentCanvasState);
        }

        inputState.Reset();
    }

    private void IsToToggleCanvas(MyEnums.CanvasToToggle target)
    {
        // Debug.Log(target.ToString());

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



    private bool IsAnyManagedCanvasOpen()
    {
        foreach (var canvas in canvasToManage)
        {
            if (canvas == null || !canvas.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (canvas.alpha > 0.01f || canvas.interactable || canvas.blocksRaycasts)
            {
                return true;
            }
        }

        return false;
    }

    private void ResetCanvas()
    {
        currentCanvasState = MyEnums.CanvasToToggle.Default;

        foreach (var canvas in uiCanvasList)
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
