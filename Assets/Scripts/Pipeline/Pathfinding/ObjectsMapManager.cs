using System.Collections.Generic;
using Contracts.Base;
using UnityEngine;

namespace Pipeline.Pathfinding
{
    struct PositionInfo
    {
        private static string guid = System.Guid.NewGuid().ToString();
        public Vector3 pos;
    }
//同步方登记自己的位置
//使用方传入一个值来查找 查找时算法？查找时发送事件让同步放更新一次然后再查，有时序问题？
//空间规划算法？
    public class ObjectsMapManager : YSingleton<ObjectsMapManager>
    {
        /// <summary>
        /// 键为 Tag，值为该物体登记的位置信息
        /// </summary>
        Dictionary<string, PositionInfo> objects = new();

        // 第一帧执行前调用
        void Start()
        {
            PositionInfo positionInfo = new();
            positionInfo.pos = Vector3.zero;
        }

        // 每帧调用
        void Update()
        {
        }

        void RegistToMap(Vector3 worldPos)
        {
        }
    }
}