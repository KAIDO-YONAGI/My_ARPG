using UnityEngine;

public class PlayerHealth : YSingleton<PlayerHealth>
{
    [SerializeField] private GameObject playerRoot;


    protected override void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this); // 只销毁重复组件，保留玩家根节点
            return;
        }
        _instance = this;
    }

    void Start()
    {
        StatsService.Instance.Respawn();
    }
    public void ChangeHealth(int amount)
    {
        StatsService.Instance.UpdateHealth(amount);

        if (StatsService.Instance.GetCurrentHealth() <= 0)
        {
            // GameOver 不配置按键，通过统一 RequestCanvasToggle 请求分支进入焦点栈与阻塞体系。
            UIManager.Instance.RequestCanvasToggle(MyEnums.CanvasToToggle.GameOver);

            if (playerRoot != null)
                playerRoot.SetActive(false);
        }
    }
}
