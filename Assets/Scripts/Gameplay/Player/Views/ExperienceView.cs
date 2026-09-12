using TMPro;
using UnityEngine.UI;

/// <summary>
/// 经验条与等级文本的显示层（View）：只认 Slider / TMP_Text，不判定规则、不找数据来源。
/// 由 <see cref="ExperienceController"/> 构造，值由它传进来；字段没接线时静默跳过，不抛异常。
/// </summary>
public class ExperienceView
{
    private readonly Slider expSlider;
    private readonly TMP_Text currentLevelText;

    public ExperienceView(Slider expSlider, TMP_Text currentLevelText)
    {
        this.expSlider = expSlider;
        this.currentLevelText = currentLevelText;
    }

    /// <summary>把一次结算后的经验状态画到界面：进度条上限 = 当前升级阈值，值 = 当前经验。</summary>
    public void SetExp(int currentExp, int expToUpgrade, int level)
    {
        if (expSlider != null)
        {
            expSlider.maxValue = expToUpgrade;
            expSlider.value = currentExp;
        }

        if (currentLevelText != null)
        {
            currentLevelText.text = "Level:" + level;
        }
    }
}