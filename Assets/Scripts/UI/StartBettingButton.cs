using System.Collections.Generic;
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
        if (ladderManager == null)
            return;

        // ✅ 골 버튼이 먼저 선택되지 않은 경우 클릭 무시
        if (!ladderManager.IsGoalSelected())
        {
            Debug.LogWarning("⚠ 먼저 골 지점을 선택해주세요.");
            ladderManager.ShowResultMessage("⚠ 도착 지점을 먼저 선택하세요.");
            return;
        }

        ladderManager.SetSelectedStart(startIndex);
        ladderManager.HighlightSelectedStartButton(this);
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

    public static float GetXPosition(int index, float ladderWidth, int verticalCount)
    {
        return -ladderWidth / 2f + (index * ladderWidth / (verticalCount - 1));
    }

    public static float CalculateActualLadderWidth(List<GameObject> verticalLines)
    {
        if (verticalLines == null || verticalLines.Count < 2)
            return 800f; // fallback
        float left = verticalLines[0].GetComponent<RectTransform>().anchoredPosition.x;
        float right = verticalLines[verticalLines.Count - 1].GetComponent<RectTransform>().anchoredPosition.x;
        return Mathf.Abs(right - left);
    }

    /// <summary>
    /// 버튼 자체의 클릭 가능 여부 설정
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.interactable = interactable;
    }
}