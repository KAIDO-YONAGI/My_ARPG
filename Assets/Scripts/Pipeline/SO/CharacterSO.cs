using Contracts;
using UnityEngine;

namespace Pipeline.SO
{
    /// <summary>对话角色配置：角色名与立绘，并继承稳定 GUID 用于跨版本引用。</summary>
    [CreateAssetMenu(fileName = "CharacterSO", menuName = "Dialog/Character", order = 1)]
    public class CharacterSO : GuidSO
    {
        public string characterName;
        public Sprite characterPortrait;


    }
}
