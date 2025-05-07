using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LadderLayoutHelper
/// - 사다리 UI의 세로줄, 가로줄, 버튼, 플레이어의 위치 정렬을 일관되게 계산하는 헬퍼 클래스
/// - ladderWidth를 기준으로 중앙 정렬 계산을 수행함
/// </summary>
public static class LadderLayoutHelper
{
    /// <summary>
    /// 세로줄 간 간격 계산 (spacingX)
    /// </summary>
    public static float CalculateSpacingX(float ladderWidth, int verticalCount)
    {
        if (verticalCount <= 1) return 0;
        return ladderWidth / (verticalCount - 1); // ✅ 반드시 분모는 (count - 1)
    }

    /// <summary>
    /// X축 시작 위치 계산 (가장 왼쪽 세로줄의 X 좌표)
    /// </summary>
    public static float GetStartX(float ladderWidth)
    {
        return -ladderWidth / 2f;
    }

    /// <summary>
    /// Y축 시작 위치 계산 (가장 위층의 Y 좌표)
    /// </summary>
    public static float GetStartY(int stepCount, float stepHeight)
    {
        return (stepCount / 2f) * stepHeight;
    }

    /// <summary>
    /// 세로줄 인덱스를 기반으로 X 위치 계산
    /// </summary>
    public static float GetXPosition(int index, float ladderWidth, int verticalCount)
    {
        if (verticalCount <= 1) return 0f;

        float spacing = ladderWidth / (verticalCount - 1); // 간격 계산
        float startX = -((verticalCount - 1) * spacing) / 2f; // 중앙 정렬 기준 시작점

        return startX + (index * spacing); // 최종 X 좌표
    }

    /// <summary>
    /// 해당 층 인덱스에 대한 Y 좌표 계산 (UI 기준: 위가 +, 아래가 -)
    /// </summary>
    public static float GetYPosition(int yIndex, int stepCount, float stepHeight)
    {
        float offset = (stepCount - 1) * stepHeight * 0.5f; // 중앙 정렬 기준 오프셋
        return -yIndex * stepHeight + offset;
    }

    /// <summary>
    /// 두 세로줄 사이의 중앙 X 위치 계산 (가로줄용)
    /// </summary>
    public static float GetMiddleX(int x, float ladderWidth, int verticalCount)
    {
        float spacingX = CalculateSpacingX(ladderWidth, verticalCount);
        float startX = GetStartX(ladderWidth);
        return startX + spacingX * (x + 0.5f);
    }

    /// <summary>
    /// 전체 높이 계산
    /// </summary>
    public static float GetTotalHeight(int stepCount, float stepHeight)
    {
        return stepCount * stepHeight;
    }

    public static float GetVisualStartY(RectTransform verticalLine)
    {
        return verticalLine.anchoredPosition.y + verticalLine.sizeDelta.y / 2f;
    }

    /// <summary>
    /// 실제 생성된 세로줄의 RectTransform 좌표로부터 사다리 폭 계산
    /// </summary>
    public static float CalculateActualLadderWidth(List<RectTransform> verticalLines)
    {
        if (verticalLines == null || verticalLines.Count < 2)
            return 0f; // fallback 기본값

        float firstX = verticalLines[0].anchoredPosition.x;
        float lastX = verticalLines[verticalLines.Count - 1].anchoredPosition.x;

        return Mathf.Abs(lastX - firstX);
    }
}
