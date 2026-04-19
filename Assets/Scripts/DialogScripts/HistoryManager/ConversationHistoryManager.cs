using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConversationHistoryManager : MonoBehaviour
{
    public static ConversationHistoryManager instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else Destroy(gameObject);
    }
    private HashSet<CharacterSO> charactersHasChated = new();
    public HashSet<CharacterSO> CharactersHasChated => charactersHasChated;

    public void RecordCharacter(CharacterSO character)
    {
        charactersHasChated.Add(character);
    }
}
