using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// 玩家位置定位器：给存档系统（SaveDataManager）提供玩家当前坐标。
    /// 只做定位查询，不承担任何控制职责，不是 MVCS 的 Controller。
    /// </summary>
    public class PlayerLocator : YSingleton<PlayerLocator>
    {
        [SerializeField] private Transform playerTransform;

        public Vector3 GetPosition() => playerTransform.position;
    }
}
