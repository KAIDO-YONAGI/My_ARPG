using UnityEngine;

public class NPCChat : MonoBehaviour
{
    private Rigidbody2D rb;
    // public Animator moveAnimator;
    public Animator chatAnimator;
    public DialogSO dialogSO;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        DialogManager.instance.DisableButtons();
    }
    private void OnEnable()
    {
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        chatAnimator.Play("Chat");

    }
    private void OnDisable()
    {
        rb.isKinematic = false;
        chatAnimator.Play("Idle");
        DialogManager.instance.EndDialog();
    }

    private void Update()
    {
        if (Input.GetButtonDown("NPCInteract"))
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
        else if (Input.GetMouseButtonDown(0) && DialogManager.instance.isDialogActive)
        {
            DialogManager.instance.AdvanceDialog();
        }

    }
}
