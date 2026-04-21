using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            return;
        }

        setDialogCanvas(false);
        DisableButtons();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void setDialogCanvas(bool state)
    {
        if (dialogCanvasGroup == null)
        {
            isDialogActive = false;
            return;
        }

        dialogCanvasGroup.alpha = state ? 1 : 0;
        dialogCanvasGroup.interactable = state;
        dialogCanvasGroup.blocksRaycasts = state;
        isDialogActive = state;
    }

    public void StartDialog(DialogSO dialog)
    {
        if (dialog == null)
        {
            return;
        }

        if (!MatchConditionsToStartDialog(dialog))
        {
            return;
        }

        setDialogCanvas(true);
        DisableButtons();
        currentDialog = dialog;
        currentLineIndex = 0;
        ShowDialog();
    }

    public void AdvanceDialog()
    {
        if (currentDialog == null)
        {
            return;
        }

        if (currentLineIndex < currentDialog.dialogLines.Length)
        {
            ShowDialog();
        }
        else if (currentDialog.nextDialogOptions.Length == 0 &&
                 currentLineIndex == currentDialog.dialogLines.Length)
        {
            EndDialog();
            return;
        }

        if (currentDialog != null &&
            currentDialog.dialogLines.Length != 0 &&
            currentDialog.dialogLines.Length == currentLineIndex)
        {
            ShowChoices();
        }
    }

    public void EndDialog()
    {
        setDialogCanvas(false);
        DisableButtons();

        if (currentDialog != null)
        {
            if (ConversationHistoryManager.instance != null)
            {
                ConversationHistoryManager.instance.RecordCharacter(currentDialog.mainCharacter);
            }

            if (currentDialog.canOnlyBeTriggeredOnce)
            {
                currentDialog.parentDialog.SetHasChated(true);
            }
        }

        currentDialog = null;
        currentLineIndex = 0;
    }

    public void ForeceEndDialog()
    {
        setDialogCanvas(false);
        DisableButtons();
        currentDialog = null;
        currentLineIndex = 0;
    }

    private bool MatchConditionsToStartDialog(DialogSO dialog)
    {
        HashSet<CharacterSO> needToTalk = new(dialog.requireCharacters);
        HashSet<CharacterSO> charactersHasChated = ConversationHistoryManager.instance != null
            ? ConversationHistoryManager.instance.CharactersHasChated
            : new HashSet<CharacterSO>();

        {//角色对话拒绝策略
            needToTalk.ExceptWith(charactersHasChated);

            if (needToTalk.Count > 0)
            {
                StartRefuseDialog(dialog, MyEnums.ChatType.RefuseChatByCharacter);
                return false;
            }
        }

        {//物品对话拒绝策略
            Dictionary<ItemSO, int> needToPick = dialog.requireItems
                    .ToDictionary(i => i.itemSO, i => i.quantity);

            foreach (var item in needToPick)
            {
                ItemSO itemSO = item.Key;
                int amount = item.Value;

                if (ItemHistoryManager.instance == null ||
                    !ItemHistoryManager.instance.HasPickedOverAmount(itemSO, amount))
                {
                    StartRefuseDialog(dialog, MyEnums.ChatType.RefuseChatByItem);
                    return false;
                }
            }
        }

        {//主分支结束后默认对话
        //TODO HasChated需要改为非SO存储
            Debug.Log(dialog.HasChated);

            if (dialog.HasChated)
            {
                StartRefuseDialog(dialog, MyEnums.ChatType.DefaultChat);
                return false;
            }
        }

        return true;
    }

    private void StartRefuseDialog(DialogSO dialog, MyEnums.ChatType chatTypeToStart)
    {
        foreach (var characterRefuseDialog in dialog.refusingDialogs)
        {
            if (characterRefuseDialog.chatType != chatTypeToStart)
            {
                continue;
            }

            setDialogCanvas(true);
            DisableButtons();
            currentDialog = characterRefuseDialog;
            currentLineIndex = 0;
            ShowDialog();
            break;
        }
    }

    private void ShowChoices()
    {
        if (currentDialog == null || currentDialog.nextDialogOptions.Length == 0)
        {
            return;
        }

        if (optionButtons == null || optionButtons.Length == 0)
        {
            return;
        }

        InitializeButtons();

        for (int i = 0; i < currentDialog.nextDialogOptions.Length; i++)
        {
            if (i >= optionButtons.Length)
            {
                break;
            }

            if (optionButtons[i] == null)
            {
                continue;
            }

            int index = i;
            DialogSO nextDialog = currentDialog.nextDialogOptions[index].nextDialogNode;

            optionButtons[index].onClick.AddListener(() => OnOptionSelected(nextDialog));
        }
    }

    private void ShowDialog()
    {
        if (currentDialog == null || currentDialog.dialogLines.Length == 0)
        {
            EndDialog();
            return;
        }

        if (currentLineIndex < 0 || currentLineIndex >= currentDialog.dialogLines.Length)
        {
            return;
        }

        DialogLine currentLine = currentDialog.dialogLines[currentLineIndex];

        if (speakerPortrait != null)
        {
            speakerPortrait.sprite = currentLine.speaker.characterPortrait;
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = currentLine.speaker.characterName;
        }

        if (dialogText != null)
        {
            dialogText.text = currentLine.text;
        }

        currentLineIndex++;
    }

    public void DisableButtons()
    {
        if (optionButtons == null)
        {
            return;
        }

        foreach (var button in optionButtons)
        {
            if (button == null)
            {
                continue;
            }

            button.gameObject.SetActive(false);
            button.onClick.RemoveAllListeners();
        }
    }

    public void InitializeButtons()
    {
        if (currentDialog == null || optionButtons == null)
        {
            return;
        }

        int nextDialogOptions = currentDialog.nextDialogOptions.Length;
        int buttonQuantity = optionButtons.Length;

        if (nextDialogOptions > buttonQuantity)
        {
            Debug.Log("Dialog options out of button quantity:" + buttonQuantity);
            return;
        }

        for (int i = 0; i < buttonQuantity; i++)
        {
            if (optionButtons[i] == null)
            {
                continue;
            }

            if (i < nextDialogOptions)
            {
                optionButtons[i].interactable = true;
                optionButtons[i].gameObject.SetActive(true);
                TMP_Text optionText = optionButtons[i].GetComponentInChildren<TMP_Text>();

                if (optionText != null)
                {
                    optionText.text = currentDialog.nextDialogOptions[i].optionText;
                }
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnOptionSelected(DialogSO nextDialog)
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
