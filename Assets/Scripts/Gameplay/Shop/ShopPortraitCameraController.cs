using UnityEngine;

[DisallowMultipleComponent]
public class ShopPortraitCameraController : MonoBehaviour
{
    [SerializeField] private Vector3 followOffset = new(0f, 0f, -2f);
    [SerializeField] private bool hideWhenNoShopKeeper = true;
    [SerializeField] private ShopKeeperEventSO shopKeeperEvent;

    private Camera cachedCamera;
    private Transform currentTarget;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();
        SetCameraState(false);
    }

    private void OnEnable()
    {
        if (shopKeeperEvent != null)
        {
            shopKeeperEvent.ShopKeeperEntered += OnKeeperEntered;
            shopKeeperEvent.ShopKeeperExited += OnKeeperExited;
        }
        SetCameraState(false);
    }

    private void OnDisable()
    {
        if (shopKeeperEvent != null)
        {
            shopKeeperEvent.ShopKeeperEntered -= OnKeeperEntered;
            shopKeeperEvent.ShopKeeperExited -= OnKeeperExited;
        }
        currentTarget = null;
        SetCameraState(false);
    }

    private void OnKeeperEntered(ShopKeeper keeper)
    {
        currentTarget = keeper != null ? keeper.PortraitTarget : null;
        SetCameraState(currentTarget != null);
    }

    private void OnKeeperExited(ShopKeeper keeper)
    {
        currentTarget = null;
        SetCameraState(false);
    }

    private void LateUpdate()
    {
        if (currentTarget != null)
        {
            transform.position = currentTarget.position + followOffset;
        }
    }

    private void SetCameraState(bool state)
    {
        if (cachedCamera == null || !hideWhenNoShopKeeper) return;

        cachedCamera.enabled = state;
    }
}
