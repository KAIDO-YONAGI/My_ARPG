using UnityEngine;

public class NPCChat : MonoBehaviour
{
    private Rigidbody2D rb;
    public Animator chatAnimator;
    public DialogSO dialogSO;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (DialogManager.instance != null)
        {
            DialogManager.instance.DisableButtons();
        }
    }

    private void OnEnable()
    {
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

    private void Update()
    {
        DialogManager dialogManager = DialogManager.instance;

        if (dialogManager == null)
        {
            return;
        }

        if (Input.GetButtonDown("NPCInteract"))
        {
            if (dialogSO != null && !dialogManager.isDialogActive)
            {
                dialogManager.StartDialog(dialogSO);
            }
            else if (dialogManager.isDialogActive)
            {
                dialogManager.AdvanceDialog();
            }
        }
        else if (Input.GetMouseButtonDown(0) && dialogManager.isDialogActive)
        {
            dialogManager.AdvanceDialog();
        }
    }
}
