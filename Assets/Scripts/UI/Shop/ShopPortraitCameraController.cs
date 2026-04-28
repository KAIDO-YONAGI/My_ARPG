using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ShopPortraitCameraController : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private ShopLoadEventSO shopLoadEvent;
    [SerializeField] private ToggleCanvasEventSO toggleShopCanvasEvent;

    [SerializeField] private Vector3 followOffset = new(0f, 0f, -2f);
    [SerializeField] private bool hideWhenNoShopKeeper = true;

    private Camera cachedCamera;
    private Transform currentTarget;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();
        SetCameraState(false);
    }

    private void OnEnable()
    {
        if (shopLoadEvent != null)
        {
            shopLoadEvent.ShopLoadEvent += OnShopLoaded;
        }

        if (toggleShopCanvasEvent != null)
        {
            toggleShopCanvasEvent.toggleCanvasEvent += OnShopToggle;
        }
    }

    private void OnDisable()
    {
        if (shopLoadEvent != null)
        {
            shopLoadEvent.ShopLoadEvent -= OnShopLoaded;
        }

        if (toggleShopCanvasEvent != null)
        {
            toggleShopCanvasEvent.toggleCanvasEvent -= OnShopToggle;
        }

        currentTarget = null;
        SetCameraState(false);
    }

    private void LateUpdate()
    {
        if (currentTarget == null)
        {
            return;
        }

        transform.position = currentTarget.position + followOffset;
    }

    private void OnShopLoaded(
        List<ShopItems> shopItems,
        List<ShopItems> shopWeapon,
        List<ShopItems> shopArmor,
        Transform portraitTarget)
    {
        currentTarget = portraitTarget;
        SetCameraState(currentTarget != null);
    }

    private void OnShopToggle(bool state)
    {
        if (state)
        {
            return;
        }

        currentTarget = null;
        SetCameraState(false);
    }

    private void SetCameraState(bool state)
    {
        if (cachedCamera == null || !hideWhenNoShopKeeper)
        {
            return;
        }

        cachedCamera.enabled = state;
    }
}
