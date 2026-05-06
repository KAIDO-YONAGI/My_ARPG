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
    public List<ToggleCanvasEventSO> toggleCanvasEvents;

    [Header("Input Bindings")]
    public List<CanvasInputBinding> inputBindings;

    private MyEnums.CanvasToToggle canvasToToggle
        = MyEnums.CanvasToToggle.Default;
    private bool isAnyCanvasOpen;
    private Dictionary<MyEnums.CanvasToToggle, bool> inputState;
    //利用枚举字典来匹配行为，取消代码配置
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

        inputState = new Dictionary<MyEnums.CanvasToToggle, bool>();
        foreach (MyEnums.CanvasToToggle canvas in Enum.GetValues(typeof(MyEnums.CanvasToToggle)))
        {
            inputState[canvas] = false;//初始化枚举字典
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
    private void Update()
    {
        ToggleCanvas();
    }

    public void SetCanvasToggle(MyEnums.CanvasToToggle canvas, bool state)
    //留给那些会自己打开并且关掉画布组的画布来报告已关闭，如果需要跨场景可以换成一个依赖枚举的事件彻底解耦
    {

        if (!state)
        {
            isAnyCanvasOpen = false;
            canvasToToggle = MyEnums.CanvasToToggle.Default;
            //先设置为Default，状态接着会在Update里被刷新
        }

        inputState[canvas] = state;
    }

    private void ToggleCanvas()
    {
        foreach (var binding in inputBindings)
        //获取注册在inputBindings的画布组的按钮绑定的激活状态，如果不注册，则仅使用SetCanvasToggle调度
        {
            bool pressed = Input.GetButtonDown(binding.buttonName);
            inputState[binding.canvas] = inputState[binding.canvas] || pressed;
            //外部输入和点按钮均可
        }

        if (inputState[MyEnums.CanvasToToggle.ESC])
        {
            canvasToToggle = isAnyCanvasOpen
                ? MyEnums.CanvasToToggle.Default
                : MyEnums.CanvasToToggle.ESC;

            IsToToggleCanvas(canvasToToggle);
            return;
        }

        canvasToToggle = MyEnums.CanvasToToggle.Default;
        foreach (var binding in inputBindings)
        {
            if (inputState[binding.canvas])
            {
                canvasToToggle = binding.canvas;
                break;//多个输入只处理第一个
            }
        }

        if (canvasToToggle != MyEnums.CanvasToToggle.Default)
        {
            IsToToggleCanvas(canvasToToggle);
        }
    }

    private void IsToToggleCanvas(MyEnums.CanvasToToggle target)
    {
        isAnyCanvasOpen = target != MyEnums.CanvasToToggle.Default;

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
        ResetInputState();//多个输入只处理第一个

    }

    private void ResetInputState()
    {
        var keys = new List<MyEnums.CanvasToToggle>(inputState.Keys);
        foreach (var key in keys)
        {
            inputState[key] = false;
        }
    }

    private void ResetCanvas()
    {
        canvasToToggle = MyEnums.CanvasToToggle.Default;
        isAnyCanvasOpen = false;

        foreach (var eventSO in toggleCanvasEvents)
        {
            eventSO.RaiseToggleCanvasEvent(false);
        }
    }
}

[Serializable]
public class CanvasInputBinding
{
    public MyEnums.CanvasToToggle canvas;
    public string buttonName;
}
