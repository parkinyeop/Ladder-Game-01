using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StartBettingButton
/// - 출발 지점 버튼
/// - 클릭 시 LadderManager에 선택한 출발 인덱스 전달
/// </summary>
public class StartBettingButton : MonoBehaviour
{
    public int startIndex; // 이 버튼이 담당하는 출발 인덱스

    private Button button;
    private LadderManager ladderManager;

    // 색상 정의
    private static readonly Color HighlightColor = Color.cyan;
    private static readonly Color DimColor = Color.gray;
    private static readonly Color DefaultColor = Color.white;

    private void Start()
    {
        button = GetComponent<Button>();
        ladderManager = FindObjectOfType<LadderManager>();

        if (button != null)
            button.onClick.AddListener(SelectStart);
    }

    private void SelectStart()
    {
        if (ladderManager != null)
        {
            ladderManager.SetSelectedStart(startIndex);   // ⭐ 선택 인덱스 전달
            ladderManager.HighlightSelectedStartButton(this);
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
}