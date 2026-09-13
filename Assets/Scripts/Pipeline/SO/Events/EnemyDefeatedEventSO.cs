using System;
using UnityEngine;

/// <summary>
/// 敌人死亡广播：经验奖励与死亡敌人的 Transform。
/// 死亡侧（EnemyHealth）只调 OnEnemyDefeated，不知道谁在听；
/// 经验系统订阅它加经验，任务域的击杀目标将来也从这条通道取死者身份。
/// </summary>
[CreateAssetMenu(fileName = "EnemyDefeatedEvent", menuName = "Events/EnemyDefeatedEvent", order = 0)]
public class EnemyDefeatedEventSO : ScriptableObject
{
    public event Action<int, Transform> EnemyDefeated;

    public void OnEnemyDefeated(int exp, Transform enemy)
    {
        EnemyDefeated?.Invoke(exp, enemy);
    }
}
