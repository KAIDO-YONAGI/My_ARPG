using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private static PlayerHealth instance;
    [SerializeField] private GameObject playerRoot;
    [SerializeField] private ToggleCanvasEventSO toggleGameOverEvent;


    private void Awake()
    {
        if (instance == null) instance = this;
        else
        {
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void Start()
    {
        StatsManager.instance.Respawn();
    }
    public void ChangeHealth(int amount)
    {
        StatsManager.instance.UpdateHealth(amount);

        if (StatsManager.instance.GetCurrentHealth() <= 0)
        {
            toggleGameOverEvent.RaiseToggleCanvasEvent(true);

            if (playerRoot != null)
                playerRoot.SetActive(false);
        }
    }
}
