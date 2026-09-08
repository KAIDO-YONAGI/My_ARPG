using UnityEngine;
using UnityEngine.UI;

public class ShopToggles : MonoBehaviour
{
    [SerializeField] private Button itemButton;
    [SerializeField] private Button weaponButton;
    [SerializeField] private Button armorButton;

    [Header("Link To Parent Shop")]
    [Tooltip("拖入父级商店面板上的 ShopManager 组件（接口无法序列化，用 Component 承载）")]
    [SerializeField] private Component shopRef;

    private IShopInteractable shop;

    private void Awake()
    {
        shop = shopRef as IShopInteractable;
        if (shop == null)
            Debug.LogError("ShopToggles: shopRef 未接线或未实现 IShopInteractable。", this);
    }

    private void Start()
    {
        // 确保按钮不为空
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(OpenItemShop);
        }

        if (weaponButton != null)
        {
            weaponButton.onClick.AddListener(OpenWeaponShop);
        }

        if (armorButton != null)
        {
            armorButton.onClick.AddListener(OpenArmorShop);
        }
    }

    public void OpenItemShop()
    {
        if (shop != null)
            shop.OpenItemShop();
        else
            Debug.LogError("ShopToggles: 商店引用缺失！", this);
    }

    public void OpenWeaponShop()
    {
        if (shop != null)
            shop.OpenWeaponShop();
        else
            Debug.LogError("ShopToggles: 商店引用缺失！", this);
    }

    public void OpenArmorShop()
    {
        if (shop != null)
            shop.OpenArmorShop();
        else
            Debug.LogError("ShopToggles: 商店引用缺失！", this);
    }

    //在销毁时移除监听器，防止内存泄漏
    private void OnDestroy()
    {
        if (itemButton != null)
        {
            itemButton.onClick.RemoveListener(OpenItemShop);
        }

        if (weaponButton != null)
        {
            weaponButton.onClick.RemoveListener(OpenWeaponShop);
        }

        if (armorButton != null)
        {
            armorButton.onClick.RemoveListener(OpenArmorShop);
        }
    }
}
