using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogSO", menuName = "Dialog/DialogNode", order = 1)]
public class DialogSO : ScriptableObject
{
    public CharacterSO mainCharacter;
    public DialogLine[] dialogLines;
    public DialogOption[] nextDialogOptions;
}
[System.Serializable]
public class DialogLine
{
    public CharacterSO speaker;
    [TextArea(3, 10)] public string text;
}
[System.Serializable]
public class DialogOption
{
    public string optionText;
    public DialogSO nextDialogNode;
}