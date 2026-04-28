using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopToggles : MonoBehaviour
{
    [SerializeField] private Button itemButton;
    [SerializeField] private Button weaponButton;
    [SerializeField] private Button armorButton;
    
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
        if (ShopManager.instance != null)
        {
            ShopManager.instance.OpenItemShop();
        }
        else
        {
            Debug.LogError("ShopManager instance is null!");
        }
    }
    
    public void OpenWeaponShop()
    {
        if (ShopManager.instance != null)
        {
            ShopManager.instance.OpenWeaponShop();
        }
        else
        {
            Debug.LogError("ShopManager instance is null!");
        }
    }
    
    public void OpenArmorShop()
    {
        if (ShopManager.instance != null)
        {
            ShopManager.instance.OpenArmorShop();
        }
        else
        {
            Debug.LogError("ShopManager instance is null!");
        }
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