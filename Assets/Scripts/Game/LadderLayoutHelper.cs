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
        return (verticalCount > 1) ? ladderWidth / (verticalCount - 1) : 0f;
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
        return ((stepCount - 1) * stepHeight) / 2f;
    }

    /// <summary>
    /// 세로줄 인덱스를 기반으로 X 위치 계산
    /// </summary>
    public static float GetXPosition(int index, float ladderWidth, int verticalCount)
    {
        float spacingX = CalculateSpacingX(ladderWidth, verticalCount);
        float startX = GetStartX(ladderWidth);
        return startX + spacingX * index;
    }

    /// <summary>
    /// 층 인덱스를 기반으로 Y 위치 계산 (위에서 아래로 내려감)
    /// </summary>
    public static float GetYPosition(int yIndex, int stepCount, float stepHeight)
    {
        float yStart = GetStartY(stepCount, stepHeight);
        return yStart - yIndex * stepHeight;
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
}
