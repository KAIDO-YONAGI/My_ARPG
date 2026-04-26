using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

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
    [SerializeField] private GameObject fatherObjOfUICanvasNeedToReset;
    [SerializeField] private GameObject playerUI;

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
