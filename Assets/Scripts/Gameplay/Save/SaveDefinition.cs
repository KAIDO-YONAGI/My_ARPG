using Contracts.Enums;
using UnityEngine;

namespace Gameplay.Save
{
    public class SaveDefinition : MonoBehaviour
    {
        public PersistentType persistentType;
        //一个标记字段，用来决定这个 GameObject 是否参与存档
        public string ID;
        private void OnValidate()
        {
            if (persistentType == PersistentType.ReadWrite)
            {
                if (string.IsNullOrEmpty(ID))
                {
                    ID = System.Guid.NewGuid().ToString();
                }
            }
            else
            {
                ID = string.Empty;
            }
        }
    }
}
