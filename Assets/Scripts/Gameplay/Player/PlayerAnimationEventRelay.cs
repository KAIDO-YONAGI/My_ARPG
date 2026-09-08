using UnityEngine;

public class PlayerAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerBow bow;

    private void Awake()
    {
        // 引用必须在 Inspector 里接好；不再做 GetComponentInChildren 兜底
        if (combat == null)
            Debug.LogError("PlayerAnimationEventRelay: combat 引用未在 Inspector 接线。", this);
        if (bow == null)
            Debug.LogError("PlayerAnimationEventRelay: bow 引用未在 Inspector 接线。", this);
    }

    public void DealDamage()
    {
        if (combat != null && combat.IsActive)
            combat.DealDamage();
    }

    public void FinishCombat()
    {
        if (combat != null && combat.IsActive)
            combat.FinishCombat();
    }

    public void HandleShootingAiming()
    {
        if (bow != null && bow.IsActive)
            bow.HandleShootingAiming();
    }

    public void Shoot()
    {
        if (bow != null && bow.IsActive)
            bow.Shoot();
    }

    public void ShootingDone()
    {
        if (bow != null && bow.IsActive)
            bow.ShootingDone();
    }
}
