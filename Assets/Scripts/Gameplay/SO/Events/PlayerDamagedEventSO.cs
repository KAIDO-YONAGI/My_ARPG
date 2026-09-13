using System;
using UnityEngine;

/// <summary>
/// 玩家受击广播：伤害值、攻击者（击退方向用）、击退力与眩晕时长。
/// 伤害源（敌人/陷阱/投射物）只负责 OnPlayerDamaged，不知道玩家存在；
/// PlayerDamageController 订阅后统一处理扣血、击退与死亡编排，受击反馈的扩展都收口在那一个挂点。
/// </summary>
[CreateAssetMenu(fileName = "PlayerDamagedEvent", menuName = "Events/PlayerDamagedEvent", order = 0)]
public class PlayerDamagedEventSO : ScriptableObject
{
    public event Action<int, Transform, float, float> PlayerDamaged;

    public void OnPlayerDamaged(int damage, Transform attacker, float knockBackForce, float stunTime)
    {
        PlayerDamaged?.Invoke(damage, attacker, knockBackForce, stunTime);
    }
}
