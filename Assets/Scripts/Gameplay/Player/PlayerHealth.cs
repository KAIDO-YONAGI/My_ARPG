using UnityEngine;
using Gameplay.Player.Services;

public class PlayerHealth : YSingleton<PlayerHealth>
{
    [SerializeField] private GameObject playerRoot;

    void Start()
    {
        StatsService.Instance.Respawn();
    }
    public void ChangeHealth(int amount)
    {
        StatsService.Instance.UpdateHealth(amount);

        if (StatsService.Instance.Model.CurrentHealth <= 0)
        {
            // GameOver 不配置按键，通过统一 RequestCanvasToggle 请求分支进入焦点栈与阻塞体系。
            UIManager.Instance.RequestCanvasToggle(MyEnums.CanvasToToggle.GameOver);

            if (playerRoot != null)
                playerRoot.SetActive(false);
        }
    }
}
