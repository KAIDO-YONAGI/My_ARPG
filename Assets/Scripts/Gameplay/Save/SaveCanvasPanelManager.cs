using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveCanvasPanelManager : MonoBehaviour, ICanvasManager
{
    [Header("Events To Receive")]
    [SerializeField] private ToggleCanvasEventSO toggleSaveLoadCanvasEvent;
    [SerializeField] private VoidEventSO sceneLoadedEvent;
    public ToggleCanvasEventSO ToggleCanvasEvent => toggleSaveLoadCanvasEvent;
    public VoidEventSO SceneLoadedEvent => sceneLoadedEvent;

    [Header("UI")]
    [SerializeField] private CanvasGroup saveCanvasGroup;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject buttonsContent;


    private bool isPanelOpen = false;
    private MyEnums.SaveType saveType = MyEnums.SaveType.PlayerSave;
    [SerializeField] private List<SaveLoadButtonGroup> saveLoadButtonGroups = new();

    /// <summary>存档按钮组引用（Inspector 接线，替代旧的 GetChild+GetComponent 自动发现）。</summary>
    [System.Serializable]
    public class SaveLoadButtonGroup
    {
        public SaveInfo saveInfo;
        public TMP_Text saveInfoText;
        public Button saveButton;
        public TMP_Text saveButtonText;
        public Button loadButton;
    }
    public class SaveInfo

    {
        public string savePath;
        public MyEnums.SaveType saveType;
        public SaveInfo() { }
        public SaveInfo(string savePath, MyEnums.SaveType saveType)
        {
            this.savePath = savePath;
            this.saveType = saveType;
        }

    }
    private void Start()
    {
        // 一次性注册按钮监听（引用已在 Inspector 序列化好）
        foreach (var group in saveLoadButtonGroups)
        {
            if (group == null || group.saveButton == null || group.loadButton == null) continue;
            var g = group;
            g.saveButton.onClick.AddListener(() => OnClickSave(g));
            g.loadButton.onClick.AddListener(() => OnClickLoad(g));
        }
    }

    private void OnEnable()
    {
        toggleSaveLoadCanvasEvent.toggleCanvasEvent += OnToggleCanvas;
        toggleSaveLoadCanvasEvent.focusEvent += OnFocus;
        if (sceneLoadedEvent != null)
            sceneLoadedEvent.VoidEvent += OnSceneLoaded;
        LoadInfoToSaveList();
        RefreshSaveButtonState();

    }

    private void OnDisable()
    {
        toggleSaveLoadCanvasEvent.toggleCanvasEvent -= OnToggleCanvas;
        toggleSaveLoadCanvasEvent.focusEvent -= OnFocus;
        if (sceneLoadedEvent != null)
            sceneLoadedEvent.VoidEvent -= OnSceneLoaded;
    }

    private void OnSceneLoaded()
    {
        isPanelOpen = false;
        ((ICanvasManager)this).SetCanvaInactive(saveCanvasGroup, MyEnums.CanvasToToggle.SaveLoad);
    }


    private void OnToggleCanvas(bool state)
    {
        if (state)
        {
            LoadInfoToSaveList();
            RefreshSaveButtonState();

            if (!isPanelOpen)
            {
                OpenPanel();
            }
        }
        else
        {
            ClosePanel();
        }
    }

    private void OnFocus()
    {
        if (!isPanelOpen) return;
        ((ICanvasManager)this).RefreshCanvaOrder(canvas, MyEnums.CanvasToToggle.SaveLoad, true);
    }
    string[] files;
    private void LoadInfoToSaveList()
    {
        if (SaveSystem.Instance == null) return; // 启动期 OnEnable 可能早于 SaveSystem.Awake

        files = SaveSystem.Instance.GetSavesPath(saveType);

        int i = 0;
        foreach (var group in saveLoadButtonGroups)
        {
            if (i >= files.Length)
            {
                group.saveInfo = new(null,saveType);
                group.saveInfoText.text = "";
                continue;
            }
            else if (files != null)
            {
                group.saveInfo = new(files[i], saveType);
                group.saveInfoText.text = "Save Info\n" + group.saveInfo.savePath;
            }

            i++;
        }
        //按顺序加载到List里
    }

    public void OnClickSave(SaveLoadButtonGroup group)
    //按钮订阅的时候持有自己所在对象的引用，这样可以拿到文本引用
    {
        TMP_Text infoText = group.saveInfoText;
        if (IsMenuScene())
        {
            if (SaveSystem.Instance.DeleteSave(group.saveInfo?.savePath))
            {
                group.saveInfo = new(null, saveType);
                infoText.text = "";
                LoadInfoToSaveList();
                RefreshSaveButtonState();
            }
            return;
        }
        if (group.saveInfo == null)
        {
            group.saveInfo = new(null, saveType);
        }
        if (DataManager.Instance == null || !DataManager.Instance.PrepareManualSaveData())
        {
            Debug.LogWarning("Manual save data is not ready.");
            return;
        }
        string path = SaveSystem.Instance.WriteSave(group.saveInfo.saveType);
        group.saveInfo.savePath=path;
        infoText.text =  "Save Info\n" + path;

    }
    public void OnClickLoad(SaveLoadButtonGroup group)
    {
        SaveSystem.Instance.LoadSave(group.saveInfo.saveType, group.saveInfo.savePath);

    }
    private void RefreshSaveButtonState()
    {
        bool isMenuScene = IsMenuScene();
        foreach (var group in saveLoadButtonGroups)
        {
            if (group.saveButtonText != null)
            {
                group.saveButtonText.text = isMenuScene ? "Delete" : "Save";
            }
            if (group.saveButton != null)
            {
                group.saveButton.interactable = !isMenuScene || !string.IsNullOrEmpty(group.saveInfo?.savePath);
            }
        }
    }
    private bool IsMenuScene()
    {
        GameSceneSO currentScene = SceneChanger.Instance != null ? SceneChanger.Instance.GetCurrentGameScene() : null;
        return currentScene != null && currentScene.sceneType == MyEnums.SceneType.Menu;
    }
    public void OpenPanel()
    {
        isPanelOpen = true;
        ((ICanvasManager)this).ToggleCanvas(saveCanvasGroup, canvas, MyEnums.CanvasToToggle.SaveLoad, true);
        UIManager.Instance.HandleFocus(MyEnums.CanvasToToggle.SaveLoad);//非键盘按键唤起的画布，直接手动focus
    }

    public void ClosePanel()
    {
        isPanelOpen = false;
        ((ICanvasManager)this).ToggleCanvas(saveCanvasGroup, canvas, MyEnums.CanvasToToggle.SaveLoad, false);
    }
}
