using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// LadderGenerator
/// - LadderManager로부터 명령받아 사다리 구조를 생성하는 클래스
/// </summary>
public class LadderGenerator
{
    private LadderManager manager;                 // 부모 매니저 참조
    private List<GameObject> verticalLines = new List<GameObject>(); // 세로줄 리스트
    private bool[,] ladderMap;                     // 가로줄 연결 데이터

    public LadderGenerator(LadderManager manager)
    {
        this.manager = manager;
    }

    /// <summary>
    /// 전체 사다리 재구성
    /// </summary>
    /// <param name="verticalCount">세로줄 개수</param>
    /// <param name="stepCount">사다리 층 수</param>
    /// <param name="horizontalLineCount">생성할 가로줄 목표 개수</param>
    /// <param name="randomize">가로줄 랜덤 생성 여부</param>
    public void GenerateLadder(int verticalCount, int stepCount, int horizontalLineCount, bool randomize)
    {
        ClearLadder(); // 기존 제거

        ladderMap = new bool[stepCount, verticalCount - 1]; // 가로줄 연결 데이터 초기화 (층 수 x 세로줄 사이 개수)

        CreateVerticalLines(verticalCount);

        // 각 인접한 세로줄 쌍 사이에 최소 1개의 가로줄 생성 보장
        for (int x = 0; x < verticalCount - 1; x++)
        {
            // 각 세로줄 쌍에 대해 랜덤한 높이에 가로줄 생성 시도
            int y = Random.Range(0, stepCount);
            if (CanPlaceHorizontalLine(y, x, verticalCount))
            {
                ladderMap[y, x] = true;
            }
            else
            {
                // 겹치면 다른 높이에 다시 시도 (최대 3번)
                for (int i = 0; i < 3; i++)
                {
                    y = Random.Range(0, stepCount);
                    if (CanPlaceHorizontalLine(y, x, verticalCount))
                    {
                        ladderMap[y, x] = true;
                        break;
                    }
                }
            }
        }

        // 남은 가로줄 개수 계산 (최소 1개 보장된 가로줄 수 제외)
        int remainingHorizontalLines = horizontalLineCount - (verticalCount - 1);
        if (randomize)
        {
            // 랜덤 생성 시 남은 가로줄 개수를 0 이상으로 조정하여 랜덤 범위 설정
            remainingHorizontalLines = Mathf.Max(0, Random.Range(0, verticalCount + 4) - (verticalCount - 1));
            CreateAdditionalHorizontalLines(remainingHorizontalLines, verticalCount, stepCount);
        }
        else
        {
            // 고정 개수 생성 시 남은 가로줄 개수를 0 이상으로 조정
            remainingHorizontalLines = Mathf.Max(0, horizontalLineCount - (verticalCount - 1));
            CreateAdditionalHorizontalLines(remainingHorizontalLines, verticalCount, stepCount);
        }

        CreateHorizontalLineObjects(verticalCount, stepCount);
        CreateDestinationButtons(verticalCount);
    }

    /// <summary>
    /// 기존 세로줄, 가로줄 제거
    /// </summary>
    private void ClearLadder()
    {
        if (manager != null && manager.ladderRoot != null)
        {
            foreach (Transform child in manager.ladderRoot)
            {
                GameObject.Destroy(child.gameObject);
            }
        }
        verticalLines.Clear();
    }

    /// <summary>
    /// 세로줄 생성 및 LadderManager의 리스트에 추가
    /// </summary>
    /// <param name="verticalCount">생성할 세로줄 개수</param>
    private void CreateVerticalLines(int verticalCount)
    {
        if (manager == null || manager.verticalLinePrefab == null || manager.ladderRoot == null) return;

        float xOffset = -((verticalCount - 1) * manager.verticalSpacing) / 2f;

        for (int i = 0; i < verticalCount; i++)
        {
            Vector3 pos = new Vector3(i * manager.verticalSpacing + xOffset, 0, 0);
            GameObject line = GameObject.Instantiate(manager.verticalLinePrefab, pos, Quaternion.identity, manager.ladderRoot);

            // 세로줄 길이 조정 (LadderManager에서 처리하도록 변경)
            if (line.TryGetComponent<RectTransform>(out RectTransform rectTransform))
            {
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, manager.stepCount * manager.stepHeight);
                rectTransform.anchoredPosition = new Vector2(0f, (manager.stepCount * manager.stepHeight) / 2f);
            }
            else if (line.TryGetComponent<LineRenderer>(out LineRenderer lineRenderer))
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, new Vector3(0f, 0f, 0f));
                lineRenderer.SetPosition(1, new Vector3(0f, manager.stepCount * manager.stepHeight, 0f));
            }
            else
            {
                Debug.LogWarning("세로줄 프리팹에 RectTransform 또는 LineRenderer가 없어 길이 조절이 필요할 수 있습니다.");
            }

            verticalLines.Add(line);
        }

        // 생성된 세로줄 리스트를 LadderManager에 전달
        manager.SetVerticalLines(verticalLines);
    }

    /// <summary>
    /// 가로줄 생성 가능 여부 체크 (양 옆 가로줄 겹침 방지)
    /// </summary>
    /// <param name="y">사다리 층 (행 인덱스)</param>
    /// <param name="x">세로줄 사이 위치 (열 인덱스)</param>
    /// <param name="verticalCount">총 세로줄 개수</param>
    /// <returns>true: 생성 가능, false: 생성 불가능</returns>
    private bool CanPlaceHorizontalLine(int y, int x, int verticalCount)
    {
        bool hasLeft = (x > 0 && ladderMap[y, x - 1]);
        bool hasRight = (x < verticalCount - 2 && ladderMap[y, x + 1]);
        bool hasNow = ladderMap[y, x];

        return !(hasLeft || hasNow || hasRight);
    }

    /// <summary>
    /// 추가적인 가로줄 랜덤 생성
    /// </summary>
    /// <param name="count">생성할 가로줄 개수</param>
    /// <param name="verticalCount">총 세로줄 개수</param>
    /// <param name="stepCount">사다리 층 수</param>
    private void CreateAdditionalHorizontalLines(int count, int verticalCount, int stepCount)
    {
        int created = 0;
        int attempts = 0;
        int maxAttempts = count * 5; // 적절한 시도 횟수 설정 (가로줄 개수의 5배만큼 시도)

        while (created < count && attempts < maxAttempts)
        {
            int y = Random.Range(0, stepCount);
            int x = Random.Range(0, verticalCount - 1);

            if (CanPlaceHorizontalLine(y, x, verticalCount))
            {
                ladderMap[y, x] = true;
                created++;
            }
            attempts++;
        }
    }

    /// <summary>
    /// 가로줄 오브젝트 생성 및 배치
    /// </summary>
    /// <param name="verticalCount">총 세로줄 개수</param>
    /// <param name="stepCount">사다리 층 수</param>
    private void CreateHorizontalLineObjects(int verticalCount, int stepCount)
    {
        if (manager == null || manager.horizontalLinePrefab == null || manager.ladderRoot == null) return;

        float xOffset = -((verticalCount - 1) * manager.verticalSpacing) / 2f;
        float yStart = ((stepCount - 1) * manager.stepHeight) / 2f;

        for (int y = 0; y < stepCount; y++)
        {
            for (int x = 0; x < verticalCount - 1; x++)
            {
                if (!ladderMap[y, x]) continue;

                float posX = (x + 0.5f) * manager.verticalSpacing + xOffset;
                float posY = yStart - y * manager.stepHeight;

                GameObject hLine = GameObject.Instantiate(manager.horizontalLinePrefab, new Vector3(posX, posY, 0), Quaternion.identity, manager.ladderRoot);

                Transform body = hLine.transform.Find("LineBody");
                if (body != null)
                {
                    body.localScale = new Vector3(manager.verticalSpacing, 0.1f, 1f);
                    body.localPosition = Vector3.zero;
                }
                hLine.transform.localScale = Vector3.one;
            }
        }
    }

    /// <summary>
    /// 골 버튼 생성 요청
    /// </summary>
    /// <param name="verticalCount">총 세로줄 개수</param>
    private void CreateDestinationButtons(int verticalCount)
    {
        if (manager == null || manager.destinationButtonPrefab == null || manager.destinationButtonsParent == null) return;

        // LadderManager를 통해 골 버튼 생성 및 관리하도록 변경
        manager.InitializeDestinationButtons(verticalCount);
    }

    private int selectedDestination = -1; // 선택된 도착 인덱스 (-1은 선택 없음)

    /// <summary>
    /// 선택된 도착 인덱스를 저장
    /// </summary>
    /// <param name="index">선택된 도착 지점 인덱스</param>
    public void SetSelectedDestination(int index)
    {
        selectedDestination = index;
    }

    /// <summary>
    /// 현재 선택된 도착 인덱스를 가져오기
    /// </summary>
    /// <returns>선택된 도착 지점 인덱스 (-1: 미선택)</returns>
    public int GetSelectedDestination()
    {
        return selectedDestination;
    }

    /// <summary>
    /// 사다리 데이터(ladderMap) 기준으로 특정 위치에 가로줄 존재 여부 확인
    /// </summary>
    /// <param name="y">사다리 층 (행 인덱스)</param>
    /// <param name="x">세로줄 사이 위치 (열 인덱스)</param>
    /// <returns>true: 가로줄 존재, false: 가로줄 없음</returns>
    public bool HasHorizontalLine(int y, int x)
    {
        if (ladderMap == null || x < 0 || x >= ladderMap.GetLength(1) || y < 0 || y >= ladderMap.GetLength(0))
            return false;
        return ladderMap[y, x];
    }
}