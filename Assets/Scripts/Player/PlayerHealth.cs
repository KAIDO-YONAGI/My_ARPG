using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private static PlayerHealth _instance;
    public static PlayerHealth Instance => _instance;
    [SerializeField] private GameObject playerRoot;
    [SerializeField] private ToggleCanvasEventSO toggleGameOverEvent;


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    void Start()
    {
        StatsManager.Instance.Respawn();
    }
    public void ChangeHealth(int amount)
    {
        StatsManager.Instance.UpdateHealth(amount);

        if (StatsManager.Instance.GetCurrentHealth() <= 0)
        {
            toggleGameOverEvent.RaiseToggleCanvasEvent(true);

            if (playerRoot != null)
                playerRoot.SetActive(false);
        }
    }
}
