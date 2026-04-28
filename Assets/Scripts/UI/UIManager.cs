using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Events")]
    public SceneLoadEventSO loadEventSO;
    public List<ToggleCanvasEventSO> toggleCanvasEvents;

    [SerializeField] private GameObject fatherOfCanvasToManage;
    [SerializeField] private GameObject playerUI;
    [SerializeField] private List<CanvasGroup> integretedUICanvas = new();
    [SerializeField] private List<Button> integretedButtons = new();

    private MyEnums.CanvasToToggle currentCanvasState = MyEnums.CanvasToToggle.Default;
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

        InitiateUICanvasList();
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

    private void Update()
    {
        ToggleCanvas();
    }

    private void ToggleCanvas()
    {

        bool esc=Input.GetButtonDown("ESC");
        bool skillTree = Input.GetButtonDown("ToggleSkillTree");
        bool stats = Input.GetButtonDown("ToggleStats");
        bool dialog = Input.GetButtonDown("NPCInteract");
        bool quest = Input.GetButtonDown("OpenQuestList");
        bool shop = Input.GetButtonDown("Interact");

        
        isAnyCanvasOpen = IsAnyManagedCanvasOpen();

        if (!isAnyCanvasOpen)
            currentCanvasState = MyEnums.CanvasToToggle.Default;

        if (esc)
        {
            currentCanvasState = isAnyCanvasOpen
                ? MyEnums.CanvasToToggle.Default
                : MyEnums.CanvasToToggle.ESC;

            IsToToggleCanvas(currentCanvasState);
            return;
        }

        if (isAnyCanvasOpen)//如果正打开，那就不执行切换
            return;


        if (skillTree)
            currentCanvasState = MyEnums.CanvasToToggle.Skills;
        else if (stats)
            currentCanvasState = MyEnums.CanvasToToggle.Stats;
        else if (dialog)
            currentCanvasState = MyEnums.CanvasToToggle.Dialog;
        else if (quest)
            currentCanvasState = MyEnums.CanvasToToggle.Quest;
        else if (shop)
            currentCanvasState = MyEnums.CanvasToToggle.Shop;
        else
            currentCanvasState = MyEnums.CanvasToToggle.Default;


        if (currentCanvasState != MyEnums.CanvasToToggle.Default)
        {
            IsToToggleCanvas(currentCanvasState);
        }
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

    private void InitiateUICanvasList()
    {
        foreach (Transform child in fatherOfCanvasToManage.transform)
        {
            CanvasGroup canvasGroup = child.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                uiCanvasList.Add(canvasGroup);
            }
        }
    }

    private bool IsAnyManagedCanvasOpen()
    {
        foreach (var canvas in uiCanvasList)
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
