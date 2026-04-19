using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogSO", menuName = "Dialog/DialogNode", order = 1)]
public class DialogSO : ScriptableObject
{
    [SerializeField] public MyEnums.ChatType chatType;
    public bool canOnlyBeTriggeredOnce;
    public CharacterSO mainCharacter;
    public DialogLine[] dialogLines;
    public DialogOption[] nextDialogOptions;

    [Header("Conditional Requirements")]
    public DialogSO parentDialog;
    public List<CharacterSO> requireCharacters;
    public List<Item> requireItems;

    public List<DialogSO> refusingDialogs;

    private bool hasChated=false;
    public bool HasChated=>hasChated;
    public void SetHasChated(bool state){hasChated=state;}
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
[System.Serializable]
public class Item
{
    public ItemSO itemSO;
    public int quantity;
}