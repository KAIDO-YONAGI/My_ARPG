using UnityEngine;
using UnityEngine.EventSystems;

public class MobileAttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private RectTransform pressVisual;
    [SerializeField, Range(0.5f, 1f)] private float pressedScale = 0.88f;

    private Vector3 normalScale = Vector3.one;

    private void Awake()
    {
        if (pressVisual != null)
            normalScale = pressVisual.localScale;
    }

    private void OnDisable()
    {
        SetPressed(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetPressed(true);

        if (playerMovement != null)
            playerMovement.TryAttack();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetPressed(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetPressed(false);
    }

    private void SetPressed(bool pressed)
    {
        if (pressVisual != null)
            pressVisual.localScale = normalScale * (pressed ? pressedScale : 1f);
    }
}
