using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GoalBettingButton
/// - 골 선택 버튼 UI 클래스
/// - 클릭 시 LadderManager에 선택 전달 및 시각적 하이라이트 처리
/// </summary>
public class GoalBettingButton : MonoBehaviour
{
    [Tooltip("이 버튼이 담당하는 도착 지점 인덱스")]
    public int destinationIndex;

    private Button button;
    private LadderManager ladderManager;

    // 색상 정의
    private static readonly Color HighlightColor = new Color(0.2f, 0.8f, 0.4f); // 초록빛
    private static readonly Color DimColor = new Color(0.7f, 0.7f, 0.7f);        // 연회색
    private static readonly Color DefaultColor = Color.white;                   // 기본 흰색

    private void Start()
    {
        button = GetComponent<Button>();
        ladderManager = FindObjectOfType<LadderManager>();

        if (button != null)
            button.onClick.AddListener(SelectDestination);
    }

    /// <summary>
    /// 버튼 클릭 시 호출됨
    /// - LadderManager에 도착 인덱스를 전달하고 하이라이트 요청
    /// </summary>
    private void SelectDestination()
    {
        if (ladderManager != null)
        {
            ladderManager.SetSelectedDestination(destinationIndex);
            ladderManager.HighlightSelectedGoalButton(this);
        }
    }

    /// <summary>
    /// 선택된 버튼을 강조 색상으로 설정
    /// </summary>
    public void Highlight()
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = HighlightColor;
    }

    /// <summary>
    /// 비선택 상태로 Dim 처리
    /// </summary>
    public void Dim()
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = DimColor;
    }

    /// <summary>
    /// 초기 상태로 색상 리셋
    /// </summary>
    public void ResetColor()
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = DefaultColor;
    }
}
