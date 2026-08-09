using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")] [SerializeField]
    private RectTransform backgroundRect;

    [SerializeField] private RectTransform handleRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject joystickRoot;

    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }

    private Vector2 inputVector;
    private float handleRange = 1f;

    private void Awake()
    {
        // 不依赖编辑器里保存的激活状态，运行时按平台强制调整显隐。
        // 全程使用显式引用控制，不做隐式 GetComponent、不直接操作自身 gameObject。
#if UNITY_EDITOR
        SetJoystickVisible(false);
#elif UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
        SetJoystickVisible(true);
#else
        SetJoystickVisible(false);
#endif
    }

    /// <summary>
    /// 按平台强制设置摇杆显隐：
    /// 触屏平台强制激活摇杆根物体 + CanvasGroup；桌面平台隐藏摇杆根物体。
    /// </summary>
    private void SetJoystickVisible(bool visible)
    {
        // 摇杆根物体：触屏端强制激活（即使场景里被存成 inactive），桌面端隐藏
        if (joystickRoot != null)
            joystickRoot.SetActive(visible);

        // CanvasGroup：控制摇杆子树的透明度与输入，画布保持激活不干扰其他 UI
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position = RectTransformUtility.WorldToScreenPoint(null, backgroundRect.position);
        Vector2 radius = new Vector2(backgroundRect.rect.width / 2, backgroundRect.rect.height / 2);
        inputVector = (eventData.position - position) / radius;

        if (inputVector.magnitude > 1f)
            inputVector = inputVector.normalized;

        handleRect.anchoredPosition = inputVector * radius * handleRange;

        Horizontal = inputVector.x;
        Vertical = inputVector.y;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        handleRect.anchoredPosition = Vector2.zero;
        Horizontal = 0f;
        Vertical = 0f;
    }
}