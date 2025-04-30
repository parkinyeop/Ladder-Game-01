using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// LadderGenerator
/// - LadderManager로부터 사다리 생성 명령을 받아 UI 기반 사다리를 구성
/// - 세로줄과 가로줄을 위치 계산 헬퍼를 기반으로 정확하게 정렬함
/// </summary>
public class LadderGenerator
{
    private LadderManager manager;                         // LadderManager 참조 (설정값, 프리팹, 부모 등 접근용)
    private List<GameObject> verticalLines = new();        // 생성된 세로줄 오브젝트 리스트
    private bool[,] ladderMap;                             // 사다리 가로줄 존재 정보 [y=층, x=세로줄 사이]

    private const float ladderWidth = 800f;                // 사다리 전체 가로폭 (위치 계산 공통 기준)

    public LadderGenerator(LadderManager manager)
    {
        this.manager = manager;
    }

    /// <summary>
    /// 사다리 전체 구조 생성:
    /// 1. 기존 오브젝트 제거
    /// 2. 세로줄 생성
    /// 3. ladderMap 설정 및 가로줄 배치
    /// 4. ladderMap 기반 가로줄 오브젝트 생성
    /// 5. 도착 지점 버튼 생성
    /// </summary>
    public void GenerateLadder(int verticalCount, int stepCount, int horizontalLineCount, bool randomize)
    {
        // 1. 기존 오브젝트 제거
        ClearLadder();

        // 2. 세로줄 생성
        CreateVerticalLines(verticalCount, stepCount);

        // 3. ladderMap 설정
        SetupHorizontalLines(verticalCount, stepCount, horizontalLineCount, randomize);

        // 4. 가로줄 UI 생성
        CreateHorizontalLineObjects(verticalCount, stepCount);

        // 5. 도착 버튼 생성
        CreateDestinationButtons(verticalCount);

        // 6. 출발 버튼 생성
        CreateStartButtons(verticalCount);

        // 7. 출발 버튼 초기화
        manager.ResetAllStartButtonColors();
    }

    /// <summary>
    /// ladderMap 초기화 후, 가로줄을 아래 기준에 따라 설정:
    /// 1. 모든 인접 세로줄 쌍(x)에 대해 최소 1개씩 가로줄 보장
    /// 2. 추가로 horizontalLineCount를 만족할 때까지 랜덤 생성 (겹침 방지 포함)
    /// </summary>
    private void SetupHorizontalLines(int verticalCount, int stepCount, int horizontalLineCount, bool randomize)
    {
        ladderMap = new bool[stepCount, verticalCount - 1];

        Debug.Log("[Ladder] 최소 보장 가로줄 생성 시작");

        // 1. 모든 세로줄 쌍(x)에 대해 1개 이상 보장
        for (int x = 0; x < verticalCount - 1; x++)
        {
            bool placed = false;
            int attempts = 0;
            while (!placed && attempts < stepCount * 2)
            {
                int y = Random.Range(0, stepCount);
                if (CanPlaceHorizontalLine(y, x, verticalCount))
                {
                    ladderMap[y, x] = true;
                    Debug.Log($"[보장 생성] x={x}, y={y}");
                    placed = true;
                }
                attempts++;
            }

            if (!placed)
            {
                Debug.LogWarning($"[실패] x={x}에 보장용 가로줄 배치 실패");
            }
        }

        // 최소 보장한 개수
        int guaranteedLines = verticalCount - 1;

        // 2. 추가 가로줄 생성 (랜덤 or 고정)
        int additionalCount = randomize
            ? Mathf.Max(0, Random.Range(0, verticalCount + 4) - guaranteedLines)
            : Mathf.Max(0, horizontalLineCount - guaranteedLines);

        Debug.Log($"[Ladder] 추가 가로줄 생성 시도: {additionalCount}개");

        int created = 0;
        int maxTries = additionalCount * 10;
        int tries = 0;

        while (created < additionalCount && tries < maxTries)
        {
            int x = Random.Range(0, verticalCount - 1);
            int y = Random.Range(0, stepCount);

            if (CanPlaceHorizontalLine(y, x, verticalCount))
            {
                ladderMap[y, x] = true;
                created++;
                Debug.Log($"[추가 생성] x={x}, y={y}");
            }

            tries++;
        }

        if (created < additionalCount)
        {
            Debug.LogWarning($"[추가 생성 중단] 목표 {additionalCount}개 중 {created}개만 생성됨");
        }
    }


    /// <summary>
    /// 이전에 생성된 오브젝트를 모두 제거하고 상태 초기화
    /// </summary>
    private void ClearLadder()
    {
        if (manager != null && manager.ladderRoot != null)
        {
            foreach (Transform child in manager.ladderRoot)
                GameObject.Destroy(child.gameObject);
        }
        verticalLines.Clear();
    }

    /// <summary>
    /// 세로줄(Vertical Line)을 골 버튼 위치 대신, 사다리 간격 기준으로 정확하게 배치하는 함수
    /// - 골 버튼 위치 참조 없이 자체 spacing 계산으로 위치 결정
    /// - 항상 수학적으로 중앙 정렬되며, 재호출 시에도 일관된 결과 보장
    /// </summary>
    private void CreateVerticalLines(int verticalCount, int stepCount)
    {
        if (manager == null || manager.verticalLinePrefab == null || manager.ladderRoot == null)
            return;

        // 사다리 전체 높이 계산 (step 수 × 높이)
        float totalHeight = stepCount * manager.stepHeight;

        // 가로 간격 계산 (세로줄 간격)
        float spacingX = LadderLayoutHelper.CalculateSpacingX(ladderWidth, verticalCount);
        
        // 🔽 여기에 삽입
        Debug.Log($"[spacingX 계산] ladderWidth={ladderWidth}, verticalCount={verticalCount}, spacingX={spacingX}");

        // 중앙 기준 offset (예: 3개일 경우 -1, 0, 1 위치)
        float startX = -((verticalCount - 1) * spacingX) / 2f;

        verticalLines = new List<GameObject>();

        for (int i = 0; i < verticalCount; i++)
        {
            GameObject line = GameObject.Instantiate(manager.verticalLinePrefab, manager.ladderRoot);
            RectTransform rect = line.GetComponent<RectTransform>();

            if (rect != null)
            {
                // 위치 계산: 정해진 간격대로 배치
                float posX = startX + i * spacingX;
                rect.anchoredPosition = new Vector2(posX, 0f); // Y는 0으로 중앙 기준 고정

                // 세로줄 길이 설정
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, totalHeight);

                // UI 오브젝트 정렬 기준 통일 (중앙 anchor/pivot)
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localScale = Vector3.one;

                Debug.Log($"✅ [세로줄 생성] index={i}, x={posX}, height={totalHeight}");
            }
            else
            {
                Debug.LogWarning("⚠ 세로줄 프리팹에 RectTransform 없음");
            }

            verticalLines.Add(line);
        }

        manager.SetVerticalLines(verticalLines);
    }



    /// <summary>
    /// 가로줄을 해당 위치에 놓을 수 있는지 검사 (양 옆에 이미 가로줄이 있으면 false)
    /// </summary>
    private bool CanPlaceHorizontalLine(int y, int x, int verticalCount)
    {
        bool hasLeft = (x > 0 && ladderMap[y, x - 1]);
        bool hasRight = (x < verticalCount - 2 && ladderMap[y, x + 1]);
        bool hasNow = ladderMap[y, x];
        return !(hasLeft || hasNow || hasRight);
    }

    /// <summary>
    /// 최소 보장 외에 추가로 랜덤 가로줄 생성
    /// </summary>
    private void CreateAdditionalHorizontalLines(int count, int verticalCount, int stepCount)
    {
        int created = 0, attempts = 0, maxAttempts = count * 5;
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
    /// ladderMap 데이터를 기반으로 가로줄 UI 오브젝트를 생성하고 배치합니다.
    /// 각 가로줄은 연결된 두 세로줄의 중간 위치에 배치되고, 그 거리만큼 길이를 설정합니다.
    /// </summary>
    private void CreateHorizontalLineObjects(int verticalCount, int stepCount)
    {
        // 기본 유효성 체크
        if (manager == null || manager.horizontalLinePrefab == null || manager.ladderRoot == null)
        {
            Debug.LogError("🚨 매니저 또는 프리팹이 null입니다. 가로줄 생성 중단.");
            return;
        }

        if (verticalLines == null || verticalLines.Count < 2)
        {
            Debug.LogError("🚨 verticalLines 정보가 유효하지 않음 (null이거나 2개 미만)");
            return;
        }

        // Y축 시작 위치 계산 (가장 위 층부터 시작, 중앙 정렬 기준)
        float yStart = ((stepCount - 1) * manager.stepHeight) / 2f;

        // 사다리의 모든 층을 순회
        for (int y = 0; y < stepCount; y++)
        {
            // 각 세로줄 쌍에 대해
            for (int x = 0; x < verticalCount - 1; x++)
            {
                // 해당 위치에 가로줄이 없다면 생성 생략
                if (!ladderMap[y, x])
                {
                    Debug.Log($"[스킵] ladderMap[{y},{x}] = false");
                    continue;
                }

                // 연결될 왼쪽/오른쪽 세로줄의 RectTransform 가져오기
                RectTransform left = verticalLines[x].GetComponent<RectTransform>();
                RectTransform right = verticalLines[x + 1].GetComponent<RectTransform>();

                if (left == null || right == null)
                {
                    Debug.LogWarning($"⚠ 세로줄 {x} 또는 {x + 1}의 RectTransform이 null입니다.");
                    continue;
                }

                // 세로줄 좌표 기반 가로줄 위치 및 길이 계산
                float startX = left.anchoredPosition.x;     // 왼쪽 세로줄 X 위치
                float endX = right.anchoredPosition.x;      // 오른쪽 세로줄 X 위치
                float centerX = (startX + endX) / 2f;        // 가운데 위치
                float width = Mathf.Abs(endX - startX);      // 거리 = 길이
                float posY = yStart - y * manager.stepHeight; // 층에 따른 Y 위치

                // 가로줄 프리팹 인스턴스화
                GameObject hLine = GameObject.Instantiate(manager.horizontalLinePrefab, manager.ladderRoot);
                RectTransform rect = hLine.GetComponent<RectTransform>();

                if (rect != null)
                {
                    // 중앙 정렬을 위한 Anchor, Pivot 설정
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f); // ✅ 중앙 기준
                    rect.localScale = Vector3.one;

                    // 위치와 크기 적용
                    rect.anchoredPosition = new Vector2(centerX, posY);
                    rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);

                    // 🔧 자식 Image의 width도 조정 (실제 보이는 가로줄)
                    Image innerImage = rect.GetComponentInChildren<Image>();
                    if (innerImage != null)
                    {
                        RectTransform imageRect = innerImage.GetComponent<RectTransform>();
                        if (imageRect != null)
                        {
                            imageRect.sizeDelta = new Vector2(width, imageRect.sizeDelta.y);
                            Debug.Log($"📏 [Image 길이 조정] width={width}");
                        }
                    }

                    Debug.Log($"✅ [생성] step={y}, x={x}, pos=({centerX}, {posY}), width={width}");
                }
                else
                {
                    Debug.LogWarning("⚠ 생성된 가로줄 프리팹에 RectTransform이 없습니다.");
                }
            }
        }
    }

    /// <summary>
    /// 골 버튼 생성을 LadderManager에 요청
    /// </summary>
    private void CreateDestinationButtons(int verticalCount)
    {
        if (manager.destinationButtonPrefab == null)
            Debug.LogError("🚨 destinationButtonPrefab이 연결되지 않았습니다.");

        manager.InitializeDestinationButtons(verticalCount);
    }

    /// <summary>
    /// 특정 위치에 가로줄이 존재하는지 확인
    /// </summary>
    public bool HasHorizontalLine(int y, int x)
    {
        return ladderMap != null && y >= 0 && y < ladderMap.GetLength(0) && x >= 0 && x < ladderMap.GetLength(1) && ladderMap[y, x];
    }

    /// <summary>
    /// 현재 선택된 도착 인덱스를 저장/조회
    /// </summary>
    public void SetSelectedDestination(int index) => selectedDestination = index;
    public int GetSelectedDestination() => selectedDestination;
    private int selectedDestination = -1; // -1: 미선택 상태

    private void CreateStartButtons(int verticalCount)
    {
        if (manager == null || manager.startButtonPrefab == null || manager.startButtonsParent == null)
        {
            Debug.LogError("🚨 Start 버튼 프리팹 또는 부모가 연결되지 않았습니다.");
            return;
        }

        // 기존 버튼 제거
        foreach (Transform child in manager.startButtonsParent)
            GameObject.Destroy(child.gameObject);
        manager.startButtons.Clear();

        float spacingX = 400f;
        float startX = -((verticalCount - 1) * spacingX) / 2f;
        float buttonY = 300f;

        for (int i = 0; i < verticalCount; i++)
        {
            GameObject buttonGO = GameObject.Instantiate(manager.startButtonPrefab, manager.startButtonsParent);
            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(startX + i * spacingX, buttonY);

            StartBettingButton btn = buttonGO.GetComponent<StartBettingButton>();
            btn.startIndex = i;
            manager.startButtons.Add(btn);

            Text label = buttonGO.GetComponentInChildren<Text>();
            if (label != null)
                label.text = (i + 1).ToString();
        }
    }
}
