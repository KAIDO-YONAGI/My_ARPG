using System;
using System.Collections.Generic;
using Contracts.Base;
using Pipeline.SO;

namespace Gameplay.Dialog.HistoryManager
{
    public class ConversationHistoryManager : YSingleton<ConversationHistoryManager>
    {
        /// <summary>角色对话记录变化时触发，携带本次记录的角色。</summary>
        public event Action<CharacterSO> CharacterTalkedChanged;

        // 已对话过的角色集合。
        private HashSet<CharacterSO> charactersHasChated = new();

        // 已完成的对话实例 ID 集合，按场景实例 ID 记录。
        private HashSet<int> dialogsHasChated = new();

        /// <summary>记录角色已对话并通知监听方。</summary>
        /// <param name="character">角色配置。</param>
        public void RecordCharacter(CharacterSO character)
        {
            // 重复记录不改变集合内容，但通知仍按本次记录发出，订阅方重算需要幂等。
            charactersHasChated.Add(character);
            CharacterTalkedChanged?.Invoke(character);
        }
        /// <summary>判断角色是否已经对话过。</summary>
        /// <param name="character">角色配置。</param>
        public bool HasChatedWith(CharacterSO character)
        {
            return charactersHasChated.Contains(character);
        }

        /// <summary>记录对话已完成。</summary>
        /// <param name="dialog">对话配置。</param>
        public void RecordDialogHasChated(DialogSO dialog)
        {
            dialogsHasChated.Add(dialog.GetInstanceID());
        }
        /// <summary>判断对话是否已经完成。</summary>
        /// <param name="dialog">对话配置。</param>
        public bool HasDialogChated(DialogSO dialog)
        {
            return dialogsHasChated.Contains(dialog.GetInstanceID());
        }
    }
}
