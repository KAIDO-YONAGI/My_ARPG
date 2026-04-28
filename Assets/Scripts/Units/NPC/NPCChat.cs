using UnityEngine;

public class NPCChat : MonoBehaviour
{
    private Rigidbody2D rb;
    public Animator chatAnimator;
    public DialogSO dialogSO;
    public ToggleCanvasEventSO toggleDialogEvent;
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

        if (state)
        {
            if (dialogSO != null && !DialogManager.instance.isDialogActive)
            {
                DialogManager.instance.StartDialog(dialogSO);
            }
            else if (DialogManager.instance.isDialogActive)
            {
                DialogManager.instance.AdvanceDialog();
            }
        }
        else if (!state)
        {
            DialogManager.instance.ForeceEndDialog();
            
        }
        else if (Input.GetMouseButtonDown(0) && DialogManager.instance.isDialogActive)
        {
            DialogManager.instance.AdvanceDialog();
        }
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
