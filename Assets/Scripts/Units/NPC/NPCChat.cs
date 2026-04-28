using Unity.VisualScripting;
using UnityEngine;

public class NPCChat : MonoBehaviour
{
    private Rigidbody2D rb;
    public Animator chatAnimator;
    public DialogSO dialogSO;
    public ToggleCanvasEventSO toggleDialogEvent;

    private bool chatState;
    private void OnEnable()
    {
        toggleDialogEvent.toggleCanvasEvent += OnToggleDialogEvent;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }

        if (chatAnimator != null)
        {
            chatAnimator.Play("Chat");
        }
    }

    private void OnDisable()
    {
        toggleDialogEvent.toggleCanvasEvent -= OnToggleDialogEvent;

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        if (chatAnimator != null)
        {
            chatAnimator.Play("Idle");
        }

        if (DialogManager.instance != null)
        {
            DialogManager.instance.ForeceEndDialog();
        }
    }
    private void OnToggleDialogEvent(bool state)
    {
        if (DialogManager.instance == null)
        {
            return;
        }
        chatState = state;
    }
    private void Update()
    {

        if (chatState)
        {
            if (dialogSO != null && !DialogManager.instance.isDialogActive)
            {
                DialogManager.instance.StartDialog(dialogSO);
            }
            else if (Input.GetMouseButtonDown(0) && DialogManager.instance.isDialogActive)
            {
                DialogManager.instance.AdvanceDialog();
            }
            else
            {
                return;
            }
        }
        else if (!chatState)
        {
            DialogManager.instance.ForeceEndDialog();
        }
        chatState = DialogManager.instance.isDialogActive;
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (DialogManager.instance != null)
        {
            DialogManager.instance.DisableButtons();
        }
    }
}
