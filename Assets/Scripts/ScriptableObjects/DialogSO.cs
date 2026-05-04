using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogSO", menuName = "Dialog/DialogNode", order = 1)]
public class DialogSO : ScriptableObject
{
    public CharacterSO mainCharacter;
    public DialogLine[] dialogLines;
    public DialogOption[] nextDialogOptions;

    [Header("Refuse Requirements For Main")]

    public List<RefuseDialogSO> refuseDialogs;

    [Header("Conditional Requirements For Sub")]
    public bool onlyTriggeredOnce;//用于在子对话设置该分支只能进入一次
    public DialogSO parentDialog;//在子对话（option）设置，用于标记已完成的对话，会阻止进入主分支

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