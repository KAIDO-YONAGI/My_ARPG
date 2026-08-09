using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConversationHistoryManager : MonoBehaviour
{
    private static ConversationHistoryManager _instance;
    public static ConversationHistoryManager Instance => _instance;
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
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
