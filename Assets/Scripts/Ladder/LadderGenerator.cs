using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// LadderGenerator
/// - LadderManager로부터 사다리 생성 명령을 받아 UI 기반 사다리를 구성
/// - 세로줄과 가로줄을 위치 계산 헬퍼를 기반으로 정확하게 정렬함
/// </summary>
public class LadderGenerator : MonoBehaviour
{
    [Header("세로줄 프리팹 및 부모")]
    public GameObject verticalLinePrefab;
    public Transform ladderParent;

    [Header("가로줄 프리팹")]
    public GameObject horizontalLinePrefab;

    [Header("세팅 값")]
    public float spacingX = 160f; // 세로줄 간격
    public float spacingY = 80f;  // 가로줄 간격
    public float horizontalLineHeight = 10f;

    // 생성된 사다리 정보 저장
    public List<RectTransform> verticalLines = new List<RectTransform>();
    public bool[,] ladderMap; // [y, x] : 가로줄 존재 여부
    private int verticalCount;
    private int stepCount;

    private LadderManager manager;                         // LadderManager 참조 (설정값, 프리팹, 부모 등 접근용)
    //private List<GameObject> verticalLines = new();        // 생성된 세로줄 오브젝트 리스트
    //private bool[,] ladderMap;                             // 사다리 가로줄 존재 정보 [y=층, x=세로줄 사이]

    private const float ladderWidth = 800f;                // 사다리 전체 가로폭 (위치 계산 공통 기준)

    public void Initialize(LadderManager manager)
    {
        this.manager = manager;
    }

    public void GenerateVerticalLines(int verticalCount, int stepCount)
    {
        this.verticalCount = verticalCount;
        this.stepCount = stepCount;

        verticalLines.Clear();

        if (ladderParent == null)
        {
            Debug.LogError("🚨 ladderParent가 연결되지 않았습니다.");
            return;
        }

        foreach (Transform child in ladderParent)
        {
            UnityEngine.Object.Destroy(child.gameObject);
        }

        for (int i = 0; i < verticalCount; i++)
        {
            if (verticalLinePrefab == null)
            {
                Debug.LogError("🚨 verticalLinePrefab이 연결되지 않았습니다.");
                continue;
            }

            GameObject line = Instantiate(verticalLinePrefab, ladderParent);
            RectTransform rt = line.GetComponent<RectTransform>();

            if (rt != null)
            {
                float x = GetXPosition(i);
                Debug.Log($"✅ VerticalLine #{i} X pos: {x}");

                rt.anchoredPosition = new Vector2(x, 0f);
                verticalLines.Add(rt);
            }
            else
            {
                Debug.LogError($"🚨 verticalLinePrefab에 RectTransform이 없습니다! index={i}");
            }
        }

        if (manager == null)
        {
            Debug.LogError("🚨 manager가 null입니다. 생성자 주입 여부 확인 필요");
            return;
        }

        manager.SetVerticalLines(verticalLines);
    }
    // ---------------------------------------------------------------------
    // ✅ 가로줄을 생성하는 함수 (GO 버튼 누를 때 호출)
    public void CreateHorizontalLinesWithGuarantee()
    {
        int min = verticalCount - 1;
        int max = verticalCount + 3;
        int horizontalLineCount = Random.Range(min, max + 1);

        int created = 0;
        int safety = 1000; // 무한 루프 방지용

        if (ladderMap == null)
        {
            Debug.LogWarning("⚠ ladderMap이 null 상태였습니다. 새로 초기화합니다.");
            ladderMap = new bool[stepCount, verticalCount - 1];
        }

        while (created < horizontalLineCount && safety-- > 0)
        {
            int y = Random.Range(0, stepCount);
            int x = Random.Range(0, verticalCount - 1);

            // 양 옆에 이미 가로줄이 있으면 생성하지 않음
            if ((x > 0 && ladderMap[y, x - 1]) || ladderMap[y, x] || (x < verticalCount - 2 && ladderMap[y, x + 1]))
                continue;

            ladderMap[y, x] = true;
            CreateHorizontalLine(y, x);
            created++;
        }
    }

    // ---------------------------------------------------------------------
    // ✅ 실제 가로줄 UI를 생성하는 함수
    // ✅ 실제 가로줄 UI를 생성하는 함수
    /// <summary>
    /// 지정된 좌표에 정확한 길이의 가로줄을 생성하는 함수
    /// - 두 세로줄 사이 간격을 기준으로 가로줄 길이를 정확히 일치시킴
    /// - anchoredPosition 기준으로 중앙에 배치됨
    /// </summary>
    private void CreateHorizontalLine(int y, int x)
    {
        // 예외 방지: 세로줄 인덱스 유효성 체크
        if (verticalLines == null || verticalLines.Count <= x + 1)
        {
            Debug.LogError($"🚨 가로줄 생성 실패: verticalLines가 부족하거나 잘못된 인덱스 접근. x={x}");
            return;
        }

        RectTransform left = verticalLines[x];
        RectTransform right = verticalLines[x + 1];

        // 세로줄 좌표 확인
        float startX = left.anchoredPosition.x;
        float endX = right.anchoredPosition.x;

        // 실제 거리 계산 (절대값)
        float width = Mathf.Abs(endX - startX);

        // ⚠ 간혹 위치가 0으로 초기화된 경우가 있음 → 로그 확인
        if (width < 1f)
        {
            Debug.LogWarning($"⚠ 가로줄 길이가 매우 짧음! startX={startX}, endX={endX}, width={width} → spacingX 사용 권장");
        }

        // 가로줄 프리팹 생성
        GameObject line = Instantiate(horizontalLinePrefab, ladderParent);
        RectTransform rt = line.GetComponent<RectTransform>();

        if (rt != null)
        {
            // 위치 및 크기 설정
            float yPos = LadderLayoutHelper.GetYPosition(y, stepCount, manager.stepHeight);
            float centerX = (startX + endX) / 2f;

            rt.anchoredPosition = new Vector2(centerX, yPos);
            rt.sizeDelta = new Vector2(width, horizontalLineHeight);

            // UI 기준 정렬 보정
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;

            // 자식 이미지 너비 보정
            Image img = rt.GetComponentInChildren<Image>();
            if (img != null)
            {
                RectTransform imgRect = img.GetComponent<RectTransform>();
                if (imgRect != null)
                    imgRect.sizeDelta = new Vector2(width, imgRect.sizeDelta.y);
            }

            Debug.Log($"✅ [HorizontalLine] y={y}, from x={startX} to x={endX}, width={width}");
        }
        else
        {
            Debug.LogError("🚨 가로줄 프리팹에 RectTransform이 없음");
        }
    }

    // ---------------------------------------------------------------------
    // ✅ X 위치 계산 함수
    public float GetXPosition(int index)
    {
        if (verticalCount <= 1) return 0f;

        float spacing = ladderWidth / (verticalCount - 1); // 가로 간격
        float startX = -((verticalCount - 1) * spacing) / 2f; // 중앙 기준 시작 위치

        return startX + index * spacing; // 최종 X 좌표
    }

    // ✅ Y 위치 계산 함수
    public static float GetYPosition(int index, int stepCount, float stepHeight)
    {
        float offset = (stepCount - 1) * stepHeight * 0.5f;
        return -index * stepHeight + offset;
    }



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
    /// 사다리 가로줄 생성 함수 (보장 + 추가 랜덤 포함)
    /// - verticalCount: 세로줄 개수
    /// - stepCount: 층 수 (Y 방향 개수)
    /// - horizontalLineCount: 총 가로줄 목표 수 (randomize=false일 때 사용됨)
    /// - randomize: true면 랜덤 개수 생성, false면 고정 개수 생성
    /// </summary>
    private void SetupHorizontalLines(int verticalCount, int stepCount, int horizontalLineCount, bool randomize)
    {
        // 1. ladderMap 배열 초기화
        //    [층 수, 세로줄 사이 개수]로 구성
        ladderMap = new bool[stepCount, verticalCount - 1];

        // 2. 동일한 Y 좌표에 중복 배치되지 않도록 추적할 Set
        HashSet<int> usedYPositions = new HashSet<int>();

        // 3. 각 세로줄 쌍(x)에 대해 최소 하나의 가로줄을 보장
        for (int x = 0; x < verticalCount - 1; x++)
        {
            bool placed = false;
            int attempt = 0;

            // 보장 배치 시도 최대 횟수 제한
            while (!placed && attempt < stepCount * 2)
            {
                int y = UnityEngine.Random.Range(0, stepCount);

                // 양옆/자기 위치에 이미 존재하거나 y값이 중복되면 배치 불가
                if (CanPlaceHorizontalLine(y, x, verticalCount) && !usedYPositions.Contains(y))
                {
                    ladderMap[y, x] = true;               // 맵에 등록
                    usedYPositions.Add(y);                // y값 중복 방지
                    CreateHorizontalLine(y, x);           // 실제 UI 생성
                    placed = true;                        // 배치 성공
                }

                attempt++;
            }

            // 만약 너무 많은 시도에도 실패했다면 강제 배치 (예외 상황 대비)
            if (!placed)
            {
                int fallbackY = UnityEngine.Random.Range(0, stepCount);
                ladderMap[fallbackY, x] = true;
                usedYPositions.Add(fallbackY);
                CreateHorizontalLine(fallbackY, x);
                Debug.LogWarning($"⚠️ [강제 배치] x={x}, y={fallbackY}");
            }
        }

        // 4. 최소 보장된 개수 계산
        int guaranteedCount = verticalCount - 1;

        // 5. 총 목표 가로줄 수 결정
        int targetCount = randomize
            ? UnityEngine.Random.Range(guaranteedCount, guaranteedCount + 4) // [세로-1 ~ 세로+3]
            : Mathf.Clamp(horizontalLineCount, guaranteedCount, stepCount * (verticalCount - 1));

        // 6. 추가 생성할 개수
        int extraToCreate = targetCount - guaranteedCount;
        int created = 0;
        int safety = 1000; // 무한 루프 방지

        // 7. 추가 가로줄 생성 루프
        while (created < extraToCreate && safety-- > 0)
        {
            int y = UnityEngine.Random.Range(0, stepCount);
            int x = UnityEngine.Random.Range(0, verticalCount - 1);

            // 중복, 인접 금지, y 충돌 방지
            if (ladderMap[y, x]) continue;
            if (!CanPlaceHorizontalLine(y, x, verticalCount)) continue;
            if (usedYPositions.Contains(y)) continue;

            ladderMap[y, x] = true;
            usedYPositions.Add(y);
            CreateHorizontalLine(y, x);
            created++;
        }

        // 8. 디버그 로그로 결과 확인
        Debug.Log($"✅ SetupHorizontalLines 완료: 총 {guaranteedCount + created}개 생성됨 (보장 {guaranteedCount}, 추가 {created})");
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
    /// 세로줄(Vertical Line)을 사다리 중앙 기준으로 균등하게 배치하는 함수
    /// - 프리팹을 이용해 verticalCount 개수만큼 세로줄 UI를 생성
    /// - ladderWidth를 기준으로 좌우 간격을 계산하여 X 위치 설정
    /// - 생성된 RectTransform 리스트를 LadderManager에 전달
    /// - 세로줄 생성 후 도착/출발 버튼도 배치 (좌표 참조 위함)
    /// </summary>
    private void CreateVerticalLines(int verticalCount, int stepCount)
    {
        // 매니저나 필수 프리팹이 지정되지 않은 경우 조기 종료
        if (manager == null || manager.verticalLinePrefab == null || manager.ladderRoot == null)
            return;

        // 전체 사다리 높이 계산 (step 수 × 층 높이)
        float totalHeight = stepCount * manager.stepHeight;

        // 세로줄 간 간격 계산 (전체 너비를 verticalCount 기준으로 나눔)
        float spacingX = LadderLayoutHelper.CalculateSpacingX(manager.ladderWidth, verticalCount);

        // 시작 X 위치: 중앙 정렬을 위해 왼쪽에서 시작하는 오프셋 계산
        float startX = -((verticalCount - 1) * spacingX) / 2f;

        // 기존 세로줄 리스트 초기화 (RectTransform 기준)
        verticalLines = new List<RectTransform>();

        // verticalCount만큼 세로줄 생성
        for (int i = 0; i < verticalCount; i++)
        {
            // 1. 프리팹으로 세로줄 UI 오브젝트 생성 (ladderRoot의 자식으로)
            GameObject line = UnityEngine.Object.Instantiate(manager.verticalLinePrefab, manager.ladderRoot);

            // 2. RectTransform 컴포넌트 가져오기 (UI 전용 위치 설정 위함)
            RectTransform rect = line.GetComponent<RectTransform>();

            if (rect != null)
            {
                // 3. 정확한 X 좌표 계산 (간격 * 인덱스 + 시작 위치)
                float posX = startX + i * spacingX;
                Debug.Log($"[VerticalLine #{i}] posX={posX}");

                // 4. 세로줄의 위치 및 크기 설정 (Y는 중앙 기준)
                rect.anchoredPosition = new Vector2(posX, 0f); // 중앙 기준 Y 위치
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, totalHeight);

                // 5. UI 정렬 기준 통일 (anchor, pivot 중앙)
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localScale = Vector3.one;

                // 6. 리스트에 추가
                verticalLines.Add(rect);

            }
            else
            {
                Debug.LogWarning("⚠ 세로줄 프리팹에 RectTransform 컴포넌트가 없습니다.");
            }
        }

        // 7. 생성된 세로줄 리스트를 LadderManager에 전달
        manager.SetVerticalLines(verticalLines);

        // 8. 세로줄 위치 기준으로 도착/출발 버튼도 함께 배치
        manager.InitializeDestinationButtons(verticalCount);
        manager.InitializeStartButtons(verticalCount);


    }


    /// <summary>
    /// 해당 (y, x) 위치에 가로줄을 놓을 수 있는지 검사
    /// - 왼쪽(x-1), 현재(x), 오른쪽(x+1)에 이미 줄이 있으면 안됨
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
                    //Debug.Log($"[스킵] ladderMap[{y},{x}] = false");
                    continue;
                }

                // 연결될 왼쪽/오른쪽 세로줄의 RectTransform 가져오기
                RectTransform left = verticalLines[x].GetComponent<RectTransform>();
                RectTransform right = verticalLines[x + 1].GetComponent<RectTransform>();

                if (left == null || right == null)
                {
                    //Debug.LogWarning($"⚠ 세로줄 {x} 또는 {x + 1}의 RectTransform이 null입니다.");
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
                            //Debug.Log($"📏 [Image 길이 조정] width={width}");
                        }
                    }

                    //Debug.Log($"✅ [생성] step={y}, x={x}, pos=({centerX}, {posY}), width={width}");
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

        float buttonY = 300f;

        for (int i = 0; i < verticalCount; i++)
        {
            // ⭐ LadderLayoutHelper.GetXPosition 사용
            float x = LadderLayoutHelper.GetXPosition(i, manager.ladderWidth, verticalCount);

            GameObject buttonGO = GameObject.Instantiate(manager.startButtonPrefab, manager.startButtonsParent);
            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, buttonY);

            StartBettingButton btn = buttonGO.GetComponent<StartBettingButton>();
            btn.startIndex = i;
            manager.startButtons.Add(btn);

            Text label = buttonGO.GetComponentInChildren<Text>();
            if (label != null)
                label.text = (i + 1).ToString();
        }
    }
}
