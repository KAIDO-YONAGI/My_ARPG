using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using Unity.VisualScripting;
using System.Collections.Generic;
using System.Linq;
public class DialogManager : MonoBehaviour
{
    public static DialogManager instance;

    [Header("Dialog UI")]
    public CanvasGroup dialogCanvasGroup;
    public Image speakerPortrait;
    public TMP_Text dialogText;
    public TMP_Text speakerNameText;
    public bool isDialogActive;
    public Button[] optionButtons;

    private int currentLineIndex = 0;
    private DialogSO currentDialog;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        setDialogCanvas(false);
        DisableButtons();

    }
    public void setDialogCanvas(bool state)
    {
        dialogCanvasGroup.alpha = state ? 1 : 0;
        dialogCanvasGroup.interactable = state;
        dialogCanvasGroup.blocksRaycasts = state;
        isDialogActive = state;
    }
    public void StartDialog(DialogSO dialog)
    {
        if (dialog.HasChated)
            return;
        if (!MatchConditionsToStartDialog(dialog))
            return;

        setDialogCanvas(true);
        currentDialog = dialog;
        currentLineIndex = 0;
        ShowDialog();
    }

    public void AdvanceDialog()
    {
        if (currentLineIndex < currentDialog.dialogLines.Length)//防止越界
        {
            ShowDialog();
        }
        else if (currentDialog.nextDialogOptions.Length == 0 &&
         currentLineIndex == currentDialog.dialogLines.Length)
        {
            EndDialog();
        }
        if (currentDialog.dialogLines.Length != 0 &&
        currentDialog.dialogLines.Length == currentLineIndex)
        //非零的时候才是有对话打开的
        //另外，这个if是让选项直接和对话一同出现，不用多点一下
        {
            ShowChoices();
        }

    }
    public void EndDialog()
    {
        setDialogCanvas(false);
        if (currentDialog != null)
            ConversationHistoryManager.instance.RecordCharacter(currentDialog.mainCharacter);
        if (currentDialog.canOnlyBeTriggeredOnce)
            currentDialog.SetHasChated(true);
    }
    private bool MatchConditionsToStartDialog(DialogSO dialog)
    {
        {//对话角色限制
            HashSet<CharacterSO> needToTalk = new(dialog.requireCharacters);

            // Debug.Log(needToTalk.Count);

            needToTalk.ExceptWith(ConversationHistoryManager.instance.CharactersHasChated);

            if (needToTalk.Count > 0)
            {

                StartRefuseDialoge(dialog, MyEnums.ChatType.RefuseChatByCharacter);

                return false;
            }
        }

        {//拾取过的物品限制
            Dictionary<ItemSO, int> needToPick = dialog.requireItems
                    .ToDictionary(i => i.itemSO, i => i.quantity);

            foreach (var item in needToPick)
            {
                ItemSO itemSO = item.Key;
                int amount = item.Value;
                if (!ItemHistoryManager.instance.HasPickedOverAmount(itemSO, amount))
                {
                    StartRefuseDialoge(dialog, MyEnums.ChatType.RefuseChatByItem);

                    return false;
                }

            }
        }

        return true;
    }
    private void StartRefuseDialoge(DialogSO dialog, MyEnums.ChatType chatTypeToStart)
    {
        foreach (var characterRefuseDialog in dialog.refuseDialogs)
        {
            if (characterRefuseDialog.chatType == chatTypeToStart)
            {
                StartDialog(characterRefuseDialog);
                break;
            }
        }
    }
    private void ShowChoices()
    {
        if (currentDialog.nextDialogOptions.Length == 0)
        {
            return;
        }
        InitializeButtons();
        for (int i = 0; i < currentDialog.nextDialogOptions.Length; i++)
        {
            if (i >= optionButtons.Length) break; // 边界检查

            int index = i;
            DialogSO nextDialog = currentDialog.nextDialogOptions[index].nextDialogNode;

            optionButtons[index].onClick.AddListener(
                () => OnOptionSelected(nextDialog)
            );
        }
    }
    private void ShowDialog()//显示当前对话行的文本和说话人信息
    {
        DialogLine currentLine = currentDialog.dialogLines[currentLineIndex];
        speakerPortrait.sprite = currentLine.speaker.characterPortrait;
        speakerNameText.text = currentLine.speaker.characterName;

        dialogText.text = currentLine.text;
        currentLineIndex++;
    }
    public void DisableButtons()
    {
        foreach (var button in optionButtons)
        {
            button.gameObject.SetActive(false);
            button.onClick.RemoveAllListeners();//去除监听器，防止重复添加监听器导致的多次调用
        }
    }
    public void InitializeButtons()//初始化按钮，显示选项文本并添加监听器
    {
        int nextDialogOptions = currentDialog.nextDialogOptions.Length;
        int buttonQuantity = optionButtons.Length;

        if (nextDialogOptions > buttonQuantity)
        {
            Debug.Log("Dialog options out of button quantity:" + buttonQuantity);
            return;
        }
        for (int i = 0; i < buttonQuantity; i++)
        {
            // Debug.Log($"按钮 {i}: active={optionButtons[i].gameObject.activeSelf}, interactable={optionButtons[i].interactable}");
            if (i < nextDialogOptions)
            {
                optionButtons[i].interactable = true;
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].GetComponentInChildren<TMP_Text>().text = currentDialog.nextDialogOptions[i].optionText;
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnOptionSelected(DialogSO nextDialog)//按钮事件，根据选择的选项加载下一个对话节点
    {
        if (nextDialog != null)
        {
            StartDialog(nextDialog);
            DisableButtons();
        }
        else
        {
            EndDialog();
        }
    }
}
