using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Shop
{
    /// <summary>
    /// 商店分类按钮组：把物品、武器、护甲三个按钮绑定到
    /// <see cref="ShopManager"/> 的分类切换方法。
    /// </summary>
    /// <remarks>
    /// 按钮引用全部在 ShopCanvas 预制体上拖好；本组件只负责注册和移除监听，
    /// 不保存商品数据，也不直接操作商品槽位。
    /// </remarks>
    public class ShopCategoryToggles : MonoBehaviour
    {
        [Header("分类按钮")]
        [SerializeField] private Button itemButton;
        [SerializeField] private Button weaponButton;
        [SerializeField] private Button armorButton;

        private void Start()
        {
            if (itemButton != null)
                itemButton.onClick.AddListener(OpenItemShop);

            if (weaponButton != null)
                weaponButton.onClick.AddListener(OpenWeaponShop);

            if (armorButton != null)
                armorButton.onClick.AddListener(OpenArmorShop);
        }

        /// <summary>切换到物品分类。</summary>
        public void OpenItemShop()
        {
            ShopManager.Instance?.OpenItemShop();
        }

        /// <summary>切换到武器分类。</summary>
        public void OpenWeaponShop()
        {
            ShopManager.Instance?.OpenWeaponShop();
        }

        /// <summary>切换到护甲分类。</summary>
        public void OpenArmorShop()
        {
            ShopManager.Instance?.OpenArmorShop();
        }

        /// <summary>销毁时移除监听，防止按钮持有已销毁组件的委托。</summary>
        private void OnDestroy()
        {
            if (itemButton != null)
                itemButton.onClick.RemoveListener(OpenItemShop);

            if (weaponButton != null)
                weaponButton.onClick.RemoveListener(OpenWeaponShop);

            if (armorButton != null)
                armorButton.onClick.RemoveListener(OpenArmorShop);
        }
    }
}
