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
    public void RecordCharacter(CharacterSO character)
    {
        if (charactersHasChated.Add(character))
            Debug.Log(character);
    }
    public HashSet<CharacterSO> CharactersHasChated=>charactersHasChated;
}
