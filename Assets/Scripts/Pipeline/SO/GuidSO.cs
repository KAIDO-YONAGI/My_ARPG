using UnityEngine;

namespace Pipeline.SO
{
    /// <summary>
    /// 带稳定 GUID 的 ScriptableObject 基类。首次在 Inspector 校验时生成 GUID，
    /// 之后保持不变，供需要跨版本引用配置资产的系统使用。
    /// </summary>
    public class GuidSO : ScriptableObject
    {
        [SerializeField] private string guid;

        public string Guid => guid;

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(guid))
            {
                guid = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
