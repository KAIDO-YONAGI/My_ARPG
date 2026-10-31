using System.Collections.Generic;
using Contracts;
using Pipeline.SO;

namespace Gameplay.Dialog.HistoryManager
{
    public class ConversationHistoryManager : YSingleton<ConversationHistoryManager>
    {
        private HashSet<CharacterSO> charactersHasChated = new();

        private HashSet<int> dialogsHasChated = new();

        public void RecordCharacter(CharacterSO character)
        {
            charactersHasChated.Add(character);
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
