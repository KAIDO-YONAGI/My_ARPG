using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    public static int FocusOrder = 90;
    public static int DefaultOrder = 5;

    [Header("Events")]
    public SceneLoadEventSO loadEventSO;
    public List<ToggleCanvasEventSO> toggleCanvasEvents;

    [Header("Input Bindings")]
    public List<CanvasInputBinding> inputBindings;

    private MyEnums.CanvasToToggle canvasToToggle
        = MyEnums.CanvasToToggle.Default;
    private MyEnums.CanvasToToggle currentOpenCanvas
        = MyEnums.CanvasToToggle.Default;
    private bool isAnyCanvasOpen;
    private Dictionary<MyEnums.CanvasToToggle, bool> inputState;
    private LinkedList<MyEnums.CanvasToToggle> canvasOpenOrder;
    private MyEnums.CanvasToToggle currentFocusCanvas
        = MyEnums.CanvasToToggle.Default;
    // Uses an enum-keyed dictionary to drive behavior instead of hardcoded routing.
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
            inputState[canvas] = false;
        }

        canvasOpenOrder = new LinkedList<MyEnums.CanvasToToggle>();
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
    public void HandleFocus(MyEnums.CanvasToToggle canvas)
    {
        if (canvas == MyEnums.CanvasToToggle.Default)
        {
            return;
        }

        if (canvas == currentFocusCanvas && ContainsCanvas(canvas))
        {
            return;
        }

        ApplyFocusChange(canvas);
    }

    // Used for externally toggling the requested canvas/default state.
    public void RequestCanvasToggle(MyEnums.CanvasToToggle canvas)
    {
        if (!inputState.ContainsKey(canvas))
        {
            return;
        }

        inputState[canvas] = true;
    }

    public void RequestCanvasClose(MyEnums.CanvasToToggle canvas)
    {
        if (canvas == MyEnums.CanvasToToggle.Default || !ContainsCanvas(canvas))
        {
            return;
        }

        RaiseCanvasEvent(canvas, false);
        RefreshFocusAfterClose(canvas);
    }
    // Reports the canvas's real open/close state.
    public void ReportCanvasState(MyEnums.CanvasToToggle canvas, bool state)
    {
        if (canvas == MyEnums.CanvasToToggle.Default)
        {
            return;
        }

        UpdateCanvasOpenOrder(canvas, state);

        currentOpenCanvas = canvasOpenOrder.Last != null
            ? canvasOpenOrder.Last.Value
            : MyEnums.CanvasToToggle.Default;
        canvasToToggle = currentOpenCanvas;
        isAnyCanvasOpen = canvasOpenOrder.Count > 0;
    }

    public bool IsCanvasFocused(MyEnums.CanvasToToggle canvas)
    {
        return currentFocusCanvas == canvas;
    }

    private void ToggleCanvas()
    {
        // Reads registered input bindings; unregistered canvases can still use RequestCanvasToggle.
        foreach (var binding in inputBindings)
        {
            bool pressed = Input.GetButtonDown(binding.buttonName);
            inputState[binding.canvas] = inputState[binding.canvas] || pressed;
            // Both external requests and button presses can trigger a toggle.
        }

        if (inputState[MyEnums.CanvasToToggle.ESC])
        {
            HandleESCInput();
            ResetInputState();
            return;
        }

        if (currentOpenCanvas == MyEnums.CanvasToToggle.ESC)
        {
            ResetInputState();
            return;
        }

        canvasToToggle = MyEnums.CanvasToToggle.Default;
        foreach (var binding in inputBindings)
        {
            if (binding.canvas == MyEnums.CanvasToToggle.ESC)
            {
                continue;
            }

            if (inputState[binding.canvas])
            {
                canvasToToggle = binding.canvas;

                break;// Only handle the first input in this frame.
            }
        }

        if (canvasToToggle != MyEnums.CanvasToToggle.Default)
        {
            ApplyFocusChange(canvasToToggle);
        }

        ResetInputState();
    }

    private void HandleESCInput()
    {
        if (!isAnyCanvasOpen)
        {
            RaiseCanvasEvent(MyEnums.CanvasToToggle.ESC, true);
            return;
        }

        CloseLastCanvas();
    }

    private void CloseLastCanvas()
    {
        if (canvasOpenOrder.Last == null)
        {
            return;
        }

        MyEnums.CanvasToToggle canvasToClose = canvasOpenOrder.Last.Value;
        RaiseCanvasEvent(canvasToClose, false);
        RefreshFocusAfterClose(canvasToClose);
    }

    private void RaiseCanvasEvent(MyEnums.CanvasToToggle target, bool state)
    {
        foreach (var eventSO in toggleCanvasEvents)
        {
            if (eventSO.canvasToToggle != target)
            {
                continue;
            }

            eventSO.RaiseToggleCanvasEvent(state);
            return;
        }
    }

    private void ApplyFocusChange(MyEnums.CanvasToToggle target)
    {
        bool wasTargetOpen = ContainsCanvas(target);
        MyEnums.CanvasToToggle previousFocus = currentFocusCanvas;
        currentFocusCanvas = target;

        if (previousFocus != MyEnums.CanvasToToggle.Default &&
            previousFocus != target &&
            ContainsCanvas(previousFocus))
        {
            RaiseCanvasEvent(previousFocus, true);
        }

        if (!wasTargetOpen || previousFocus != target)
        {
            RaiseCanvasEvent(target, true);
        }
    }

    private void RefreshFocusAfterClose(MyEnums.CanvasToToggle closedCanvas)
    {
        if (currentFocusCanvas != closedCanvas)
        {
            return;
        }

        currentFocusCanvas = currentOpenCanvas;

        if (currentFocusCanvas != MyEnums.CanvasToToggle.Default)
        {
            RaiseCanvasEvent(currentFocusCanvas, true);
        }
    }

    private void UpdateCanvasOpenOrder(MyEnums.CanvasToToggle canvas, bool state)
    {
        RemoveCanvasNode(canvas);

        if (state)
        {
            canvasOpenOrder.AddLast(canvas);
        }
    }

    private bool ContainsCanvas(MyEnums.CanvasToToggle canvas)
    {
        LinkedListNode<MyEnums.CanvasToToggle> currentNode = canvasOpenOrder.First;

        while (currentNode != null)
        {
            if (currentNode.Value == canvas)
            {
                return true;
            }

            currentNode = currentNode.Next;
        }

        return false;
    }

    private void RemoveCanvasNode(MyEnums.CanvasToToggle canvas)
    {
        LinkedListNode<MyEnums.CanvasToToggle> currentNode = canvasOpenOrder.First;

        while (currentNode != null)
        {
            LinkedListNode<MyEnums.CanvasToToggle> nextNode = currentNode.Next;

            if (currentNode.Value == canvas)
            {
                canvasOpenOrder.Remove(currentNode);

                break;
            }

            currentNode = nextNode;
        }
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
        currentOpenCanvas = MyEnums.CanvasToToggle.Default;
        currentFocusCanvas = MyEnums.CanvasToToggle.Default;
        isAnyCanvasOpen = false;
        canvasOpenOrder.Clear();

        foreach (var eventSO in toggleCanvasEvents)
        {
            eventSO.RaiseToggleCanvasEvent(false);
        }

        ResetInputState();
    }
}

[Serializable]
public class CanvasInputBinding
{
    public MyEnums.CanvasToToggle canvas;
    public string buttonName;
}
