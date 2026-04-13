using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using Unity.VisualScripting;
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
    }
    private int currentLineIndex = 0;

    public void StartDialog(DialogSO dialog)
    {
        dialogCanvasGroup.alpha = 1;
        currentDialog = dialog;
        currentLineIndex = 0;
        isDialogActive = true;
        ShowDialog();
    }
    public void EndDialog()
    {
        isDialogActive = false;
        dialogCanvasGroup.alpha = 0;
        DisableButtons();
    }
    public void AdvanceDialog()
    {
        if (currentLineIndex < currentDialog.dialogLines.Length)//防止越界
        {
            ShowDialog();
        }
        else
        {
            ShowChoices();
        }
    }
    private void ShowChoices()
    {
        if (currentDialog.nextDialogOptions.Length == 0)
            EndDialog();
        else
            InitializeButtons();
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
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < currentDialog.nextDialogOptions.Length)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].GetComponentInChildren<TMP_Text>().text = currentDialog.nextDialogOptions[i].optionText;
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(i));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnOptionSelected(int i)//按钮事件，根据选择的选项加载下一个对话节点
    {
        DialogSO nextDialog = currentDialog.nextDialogOptions[i].nextDialogNode;
        if (nextDialog != null)
        {
            StartDialog(nextDialog);
        }
        else
        {
            EndDialog();
        }

    }
}
