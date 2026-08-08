using UnityEngine;
using UnityEngine.InputSystem;

public class NPCChat : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField] private Animator chatAnimator;
    [SerializeField] private DialogSO dialogSO;
    [SerializeField] private ToggleCanvasEventSO toggleDialogEvent;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference advanceDialogAction;

    private bool openDialogRequested;

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
        openDialogRequested = false;

        if (toggleDialogEvent != null)
        {
            toggleDialogEvent.toggleCanvasEvent += OnToggleDialogEvent;
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }

        if (chatAnimator != null)
        {
            chatAnimator.Play("Chat");
        }

        if (advanceDialogAction != null) advanceDialogAction.action.Enable();
    }

    private void OnDisable()
    {
        openDialogRequested = false;

        if (toggleDialogEvent != null)
        {
            toggleDialogEvent.toggleCanvasEvent -= OnToggleDialogEvent;
        }

        if (advanceDialogAction != null) advanceDialogAction.action.Disable();

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

        if (!state)
        {
            openDialogRequested = false;
            DialogManager.instance.ForeceEndDialog();
            return;
        }

        openDialogRequested = true;
    }
    private void Update()
    {
        if (DialogManager.instance == null)
        {
            return;
        }

        if (openDialogRequested)
        {
            openDialogRequested = false;

            if (dialogSO != null && !DialogManager.instance.isDialogActive)
            {
                DialogManager.instance.StartDialog(dialogSO);
            }
        }

        if (DialogManager.instance.isDialogActive && advanceDialogAction != null && advanceDialogAction.action.WasPressedThisFrame())
        {
            DialogManager.instance.AdvanceDialog();
        }
    }
}
