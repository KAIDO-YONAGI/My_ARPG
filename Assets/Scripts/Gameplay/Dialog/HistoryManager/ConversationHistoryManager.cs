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

        private HashSet<CharacterSO> charactersHasChated = new();

        private HashSet<int> dialogsHasChated = new();

        public void RecordCharacter(CharacterSO character)
        {
            charactersHasChated.Add(character);
            CharacterTalkedChanged?.Invoke(character);
        }
        public bool HasChatedWith(CharacterSO character)
        {
            return charactersHasChated.Contains(character);
        }

        public void RecordDialogHasChated(DialogSO dialog)
        {
            dialogsHasChated.Add(dialog.GetInstanceID());
        }
        public bool HasDialogChated(DialogSO dialog)
        {
            return dialogsHasChated.Contains(dialog.GetInstanceID());
        }
    }
}
