using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GoalBettingButton
/// - 골 선택 버튼 UI 클래스
/// - 클릭 시 LadderManager에 선택 전달 및 시각적 하이라이트 처리
/// - 배당률 텍스트만 출력 (번호 없음)
/// </summary>
public class GoalBettingButton : MonoBehaviour
{
    public int destinationIndex;

    [Header("배당률 출력용 텍스트")]
    public Text multiplierText; // 🎯 2X, 3X 등 표시

    private Button button;
    private LadderManager ladderManager;

    private static readonly Color HighlightColor = new Color(0.2f, 0.8f, 0.4f);
    private static readonly Color DimColor = new Color(0.7f, 0.7f, 0.7f);
    private static readonly Color DefaultColor = Color.white;

    private void Start()
    {
        button = GetComponent<Button>();
        ladderManager = FindObjectOfType<LadderManager>();

        if (button != null)
            button.onClick.AddListener(SelectDestination);
    }

    private void SelectDestination()
    {
        if (ladderManager != null)
        {
            ladderManager.SetSelectedDestination(destinationIndex);
            ladderManager.HighlightSelectedGoalButton(this);
        }
    }

    public void Highlight()
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = HighlightColor;
    }

    public void Dim()
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = DimColor;
    }

    public void ResetColor()
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = DefaultColor;
    }

    /// <summary>
    /// 배당률 텍스트 설정 (예: 2X, 3X 등)
    /// </summary>
    public void SetMultiplierText(int multiplier)
    {
        if (multiplierText != null)
            multiplierText.text = $"{multiplier}X";
    }
}