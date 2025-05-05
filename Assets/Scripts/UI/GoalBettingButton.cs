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
            button.targetGraphic.color = Color.yellow;
    }

    // ✅ 특정 색상으로 강조할 수 있는 함수 추가
    public void HighlightWithColor(Color color)
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = color;
    }

    public void Dim()
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = DimColor;
    }

    public void ResetColor()
    {
        if (GetComponent<Button>()?.targetGraphic != null)
        {
            GetComponent<Button>().targetGraphic.color = Color.white; // 기본 색상으로 복원
        }

        // 텍스트도 다시 보이게
        SetTextVisible(true);
    }

    public void SetMultiplierText(float multiplier)
    {
        Text label = GetComponentInChildren<Text>();
        if (label != null)
            label.text = $"{multiplier:F1}X"; // 소수점 1자리로 출력
    }
       
    public void SetTextVisible(bool isVisible)
    {
        Text label = GetComponentInChildren<Text>();
        if (label != null)
            label.enabled = isVisible;
    }

}