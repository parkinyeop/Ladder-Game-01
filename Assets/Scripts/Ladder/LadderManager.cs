using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// LadderManager
/// - 사다리 UI 생성, 결과 판단, 버튼 인터랙션 등을 총괄하는 컨트롤러 클래스
/// - LadderGenerator, PlayerMover와 상호작용하며 사다리 게임 흐름을 관리
/// </summary>
public class LadderManager : MonoBehaviour
{
    [Header("사다리 설정")]
    public int verticalCount = 3;               // 세로줄 개수
    public int stepCount = 10;                  // 사다리 층 수
    public int horizontalLineCount = 2;         // 생성할 가로줄 수
    public bool randomizeHorizontalLines = true;// 가로줄 랜덤 생성 여부

    [Header("간격 설정")]
    public float verticalSpacing = 400f;        // 사다리 너비 계산에 사용되는 기준 간격 (참고용)
    public float stepHeight = 60f;              // 층 간 간격 (Y축)

    [Header("UI 연결")]
    public Button generateButton;
    public Button resultButton;
    public Text resultText;

    [Header("보드 UI")]
    public GameObject board;               // 보드 패널
    public Text boardText;                // 보드 내 메시지 출력용 텍스트

    [Header("세로/가로줄 수 조절 UI")]
    public Button increaseVerticalButton;
    public Button decreaseVerticalButton;
    public Button increaseHorizontalButton;
    public Button decreaseHorizontalButton;
    public Toggle randomizeToggle;
    public Text verticalCountText;
    public Text horizontalLineCountText;

    [Header("프리팹 및 부모")]
    public Transform ladderRoot;                // 사다리 줄들의 부모 오브젝트
    public GameObject verticalLinePrefab;
    public GameObject horizontalLinePrefab;
    public Transform destinationButtonsParent;  // 골 버튼 부모
    public GameObject destinationButtonPrefab;

    private StartBettingButton selectedStartButton = null;          // 현재 선택된 출발 버튼
    public List<StartBettingButton> startButtons = new List<StartBettingButton>(); // 생성된 출발 버튼 목록
    private int selectedStartIndex = -1;                            // 선택된 출발 세로줄 인덱스

    [Header("플레이어 관련")]
    public GameObject playerPrefab;             // 이동할 플레이어 프리팹
    public Transform playerTransform;           // 생성된 플레이어의 Transform 참조

    private LadderGenerator generator;          // 사다리 생성기
    private PlayerMover playerMover;            // 플레이어 이동기
    private GameObject spawnedPlayer;           // 현재 생성된 플레이어 오브젝트

    private GoalBettingButton selectedGoalButton = null;                // 선택된 골 버튼 참조
    private List<GoalBettingButton> destinationButtons = new();        // 모든 골 버튼 리스트
    private List<GameObject> verticalLines = new();                    // 생성된 세로줄 오브젝트 저장

    [Header("출발 버튼 관련")]
    public GameObject startButtonPrefab;             // 출발 버튼 프리팹
    public Transform startButtonsParent;             // 출발 버튼들을 담을 부모 오브젝트

    [SerializeField] private BetAmountUIManager betUIManager;
    public BetAmountUIManager betAmountUIManager;

    private bool isLadderGenerated = false;  // READY 상태 → GO 상태 전환 여부
    public float ladderWidth = 800f;

    private void Start()
    {
        generator = new LadderGenerator(this);
        playerMover = new PlayerMover(this);

        if (betAmountUIManager != null)
        {
            betAmountUIManager.OnBetConfirmed += OnBetConfirmedHandler;
        }
        else
        {
            Debug.LogError("🚨 BetAmountUIManager가 연결되지 않았습니다.");
        }

        SetupUI();

        generateButton.onClick.AddListener(GenerateLadder);
        resultButton.onClick.AddListener(OnResultButtonPressed); // ✅ 상태 기반 처리

        UpdateVerticalCountText();
        UpdateHorizontalLineCountText();

        // ✅ 결과 버튼 텍스트 초기화
        if (resultButton != null)
        {
            var txt = resultButton.GetComponentInChildren<Text>();
            if (txt != null)
                txt.text = "READY";
            resultButton.interactable = false;
        }
    }

    private void OnBetAmountConfirmed(int amount)
    {
        Debug.Log($"💰 확정된 배팅 금액: {amount}");
        // 내부 게임 로직에서 활용
    }

    /// <summary>
    /// 버튼 및 토글과 이벤트 연결 초기화
    /// </summary>
    private void SetupUI()
    {
        increaseVerticalButton?.onClick.AddListener(IncreaseVerticalCount);
        decreaseVerticalButton?.onClick.AddListener(DecreaseVerticalCount);
        increaseHorizontalButton?.onClick.AddListener(IncreaseHorizontalLineCount);
        decreaseHorizontalButton?.onClick.AddListener(DecreaseHorizontalLineCount);
        randomizeToggle?.onValueChanged.AddListener(OnRandomizeToggleChanged);
    }

    /// <summary>
    /// 사다리 생성 버튼 클릭 시 처리
    /// </summary>
    public void GenerateLadder()
    {
        // 가로줄 수 무작위 생성
        int min = Mathf.Max(1, verticalCount - 1);
        int max = verticalCount + 3;
        horizontalLineCount = Random.Range(min, max + 1);

        generator.GenerateLadder(verticalCount, stepCount, horizontalLineCount, randomizeHorizontalLines);
        ResetAllGoalButtonColors();

        // ✅ 보드 활성화 및 메시지 출력
        if (board != null) board.SetActive(true);
        if (boardText != null) boardText.text = "도착 지점을 선택하세요!";

        // ✅ 배팅 금액이 0이면 결과 버튼 비활성화
        int currentBet = betAmountUIManager != null ? betAmountUIManager.GetBetAmount() : 0;
        resultButton.interactable = (currentBet > 0);


        // ✅ 결과 버튼 텍스트를 "READY"로 변경
        resultButton.GetComponentInChildren<Text>().text = "READY";

        // ✅ GO 실행 잠금: 골 선택 전까지 비활성화
        resultButton.interactable = false;

        SetStartButtonsInteractable(false);
    }

    /// <summary>
    /// 결과 버튼 클릭 시 처리
    /// - 골 선택 여부 확인, 플레이어 생성 및 이동 실행
    /// </summary>
    public void OnResultButtonClicked()
    {
        // 이미 이동 중이면 무시
        if (playerMover.IsMoving())
            return;

        if (selectedGoalButton == null)
        {
            // ✅ 보드에 안내 메시지 출력
            if (boardText != null) boardText.text = "도착 지점을 선택하세요!";
            return;
        }

        // ✅ 보드 비활성화
        if (board != null) board.SetActive(false);

        // 스타트 버튼이 선택된 경우 해당 인덱스, 아니면 무작위 인덱스 사용
        int startIndex = selectedStartIndex >= 0 ? selectedStartIndex : Random.Range(0, verticalCount);

        // ⭐ 랜덤일 경우에도 하이라이트 적용
        if (selectedStartIndex < 0 && startIndex >= 0 && startIndex < startButtons.Count)
        {
            HighlightSelectedStartButton(startButtons[startIndex]);
        }

        // 이전 플레이어가 존재하면 제거
        if (playerTransform != null)
        {
            playerMover.StopMove(this);
            Destroy(playerTransform.gameObject);
            playerTransform = null;
        }

        // 프리팹이 연결되지 않은 경우 에러
        if (playerPrefab == null)
        {
            //Debug.logError("[LadderManager] Player 프리팹이 연결되지 않았습니다.");
            return;
        }

        // 새로운 플레이어 생성
        GameObject playerGO = Instantiate(playerPrefab, ladderRoot);
        playerTransform = playerGO.transform;

        // 플레이어 위치 계산 (UI 기준 anchoredPosition)
        float x = LadderLayoutHelper.GetXPosition(startIndex, ladderWidth, verticalCount);
        float y = LadderLayoutHelper.GetStartY(stepCount, stepHeight);

        // 위치 지정 (RectTransform 기준)
        RectTransform rect = playerTransform.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(x, y);
        }
        else
        {
            //Debug.logError("[LadderManager] Player에 RectTransform이 없습니다.");
        }

        // 이동 세팅 및 실행
        playerMover.Setup(playerTransform, startIndex, 500f);      // 위치, 속도 설정
        playerMover.SetFinishCallback(CheckResult);                // 도착 후 결과 체크 콜백
        playerMover.StartMove(this);                               // 코루틴으로 이동 시작

        // 버튼 비활성화 → 도착 후 다시 활성화
        resultButton.interactable = false;
    }

    /// <summary>
    /// 플레이어 도착 후 성공 여부 확인
    /// </summary>
    private void CheckResult(int arrivedIndex)
    {
        int selectedIndex = generator.GetSelectedDestination();
        resultText.text = (arrivedIndex == selectedIndex) ? "🎉 성공!" : "❌ 실패!";
        Debug.Log($"[결과] 도착 인덱스: {arrivedIndex}, 선택 인덱스: {selectedIndex}");

        // ✅ 로그도 성공/실패에 따라 명확히 출력
        if (arrivedIndex == selectedIndex)
            Debug.Log("✅ 성공! 도착 지점이 선택한 골과 일치합니다.");
        else
            Debug.Log("❌ 실패! 도착 지점이 선택한 골과 다릅니다.");

        resultButton.interactable = true;
        resultButton.GetComponentInChildren<Text>().text = "READY"; // 버튼 상태 초기화
    }

    /// <summary>
    /// 모든 골 버튼을 활성화 또는 비활성화
    /// </summary>
    public void SetGoalButtonsInteractable(bool isOn)
    {
        foreach (var btn in destinationButtons)
        {
            Button uiButton = btn.GetComponent<Button>();
            if (uiButton != null)
                uiButton.interactable = isOn;
        }
    }

    /// <summary>
    /// 선택된 도착 인덱스 저장
    /// </summary>
    public void SetSelectedDestination(int index)
    {
        generator.SetSelectedDestination(index);
    }

    public void HighlightSelectedGoalButton(GoalBettingButton clickedButton)
    {
        // 이미 선택된 버튼을 다시 클릭한 경우 → 선택 해제
        // 이미 선택된 버튼 다시 클릭 → 처리 생략 (또는 해제 로직 추가 가능)
        if (selectedGoalButton == clickedButton)
            return;

        selectedGoalButton?.ResetColor();
        clickedButton.Highlight();
        DimOtherGoalButtons(clickedButton);
        selectedGoalButton = clickedButton;

        SetStartButtonsInteractable(true); // ⭐ 골 선택 시 스타트 버튼 활성화

        // ✅ 결과 버튼을 GO 상태로 활성화
        var txt = resultButton.GetComponentInChildren<Text>();
        if (txt != null)
            txt.text = "GO";

        resultButton.interactable = true; // ⭐ 골 선택 시 GO 버튼 활성화



        // 이전 선택된 버튼 색상 복원
        selectedGoalButton?.ResetColor();

        // 새로 선택된 버튼 강조
        clickedButton.Highlight();

        // 나머지 버튼 dim 처리
        DimOtherGoalButtons(clickedButton);

        selectedGoalButton = clickedButton;

        // ✅ 골 버튼 선택되었으므로 스타트 버튼들 활성화
        SetStartButtonsInteractable(true);
    }

    public void OnResultButtonPressed()
    {
        //string label = resultButton.GetComponentInChildren<Text>().text;

        //if (label == "READY")
        //{
        //    GenerateLadder(); // ✅ 사다리 생성 모드
        //}
        //else if (label == "GO")
        //{
        //    OnResultButtonClicked(); // ✅ 결과 실행
        //}

        string label = resultButton.GetComponentInChildren<Text>().text;

        if (label == "READY")
        {
            GenerateLadder(); // 사다리 및 UI 생성
            resultButton.GetComponentInChildren<Text>().text = "GO"; // 상태 전환
            isLadderGenerated = true;
        }
        else if (label == "GO")
        {
            if (selectedGoalButton == null)
            {
                if (boardText != null) boardText.text = "도착 지점을 선택하세요!";
                return;
            }

            // 결과 실행
            OnResultButtonClicked();

            // 완료 후 버튼을 다시 READY로 되돌리기
            resultButton.GetComponentInChildren<Text>().text = "READY";
            isLadderGenerated = false;

            // 초기화
            //ResetAllGoalButtonColors();
            //ResetAllStartButtonColors();
        }
    }

    private void DimOtherGoalButtons(GoalBettingButton selectedButton)
    {
        foreach (var button in destinationButtons)
        {
            if (button != null && button != selectedButton)
                button.Dim();
        }
    }

    public void ResetAllGoalButtonColors()
    {
        foreach (var button in destinationButtons)
        {
            button?.ResetColor();
        }
        selectedGoalButton = null;

    }

    /// <summary>
    /// 골 버튼 생성 및 위치 설정
    /// - 각 버튼에 배당률 텍스트를 설정
    /// - destinationIndex를 정확히 할당해야 선택 결과가 올바르게 작동
    /// </summary>
    public void InitializeDestinationButtons(int verticalCount)
    {
        // 기존 골 버튼 오브젝트 제거
        foreach (Transform child in destinationButtonsParent)
            Destroy(child.gameObject);
        destinationButtons.Clear();

        // 세로줄 기준으로 버튼 위치 계산 및 생성
        for (int i = 0; i < verticalCount; i++)
        {
            // 골 버튼 프리팹 생성 및 위치 설정
            GameObject buttonGO = Instantiate(destinationButtonPrefab, destinationButtonsParent);
            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            float x = LadderLayoutHelper.GetXPosition(i, ladderWidth, verticalCount);
            rect.anchoredPosition = new Vector2(x, -300f); // Y값 고정된 아래 위치

            // GoalBettingButton 설정
            GoalBettingButton btn = buttonGO.GetComponent<GoalBettingButton>();
            btn.destinationIndex = i;                          // ✅ 꼭 설정해야 결과 판단 가능
            btn.SetMultiplierText(verticalCount);              // ✅ 배당률 텍스트 설정

            destinationButtons.Add(btn);
        }
    }

    /// <summary>
    /// 가로줄 존재 여부 확인 (PlayerMover에서 사용)
    /// </summary>
    public bool HasHorizontalLine(int y, int x)
    {
        return generator.HasHorizontalLine(y, x);
    }

    /// <summary>
    /// 생성된 세로줄 오브젝트 리스트를 저장
    /// </summary>
    public void SetVerticalLines(List<GameObject> lines)
    {
        verticalLines = lines;
    }

    public List<GameObject> GetVerticalLines() => verticalLines;

    private void IncreaseVerticalCount()
    {
        if (verticalCount < 5)
        {
            verticalCount++;
            CorrectHorizontalLineCount();
            UpdateVerticalCountText();
            UpdateHorizontalLineCountText();
        }
    }

    private void DecreaseVerticalCount()
    {
        if (verticalCount > 2)
        {
            verticalCount--;
            CorrectHorizontalLineCount();
            UpdateVerticalCountText();
            UpdateHorizontalLineCountText();
        }
    }

    private void IncreaseHorizontalLineCount()
    {
        int max = verticalCount + 3;
        if (horizontalLineCount < max)
        {
            horizontalLineCount++;
            UpdateHorizontalLineCountText();
        }
    }

    private void DecreaseHorizontalLineCount()
    {
        int min = verticalCount - 1;
        if (horizontalLineCount > min)
        {
            horizontalLineCount--;
            UpdateHorizontalLineCountText();
        }
    }

    private void CorrectHorizontalLineCount()
    {
        int min = verticalCount - 1;
        int max = verticalCount + 3;
        horizontalLineCount = Mathf.Clamp(horizontalLineCount, min, max);
    }

    private void UpdateVerticalCountText()
    {
        if (verticalCountText != null)
            verticalCountText.text = $"세로줄 개수: {verticalCount}";
    }

    private void UpdateHorizontalLineCountText()
    {
        if (horizontalLineCountText != null)
            horizontalLineCountText.text = $"가로줄 개수: {horizontalLineCount}";
    }

    private void OnRandomizeToggleChanged(bool isOn)
    {
        randomizeHorizontalLines = isOn;
    }

    public RectTransform GetVerticalLineAt(int index)
    {
        if (index >= 0 && index < verticalLines.Count)
            return verticalLines[index].GetComponent<RectTransform>();
        return null;
    }

    public void SetSelectedStart(int index)
    {
        selectedStartIndex = index;
    }

    public void HighlightSelectedStartButton(StartBettingButton selectedButton)
    {
        if (selectedStartButton != null)
            selectedStartButton.ResetColor();

        if (selectedButton != null)
            selectedButton.Highlight();

        DimOtherStartButtons(selectedButton);
        selectedStartButton = selectedButton;
    }

    private void DimOtherStartButtons(StartBettingButton selectedButton)
    {
        foreach (var btn in startButtons)
        {
            if (btn != null && btn != selectedButton)
                btn.Dim();
        }
    }


    /// <summary>
    /// 출발(Start) 버튼 생성 및 위치 배치
    /// - 세로줄의 실제 위치를 기준으로 정확히 정렬됨
    /// - GetXPosition() 사용 시, 실측 ladderWidth를 반영해야 위치 불일치 해결 가능
    /// </summary>
    public void InitializeStartButtons(int verticalCount)
    {
        //Debug.log($"✅ InitializeStartButtons 실행됨 verticalCount={verticalCount}");

        // 1. 유효성 검사: 부모 객체와 프리팹이 유효한지 확인
        if (startButtonsParent == null || startButtonPrefab == null)
        {
            //Debug.logError("🚨 StartButtonsParent 또는 StartButtonPrefab이 설정되지 않았습니다.");
            return;
        }

        // 2. 기존 버튼 제거
        foreach (Transform child in startButtonsParent)
            Destroy(child.gameObject);
        startButtons.Clear();

        // 3. 스타트 버튼 배치 기준 Y 값 (상단 고정)
        float buttonY = 300f;

        // 4. 세로줄 기준으로 실제 너비 계산 (가변 ladderWidth 반영)
        float actualLadderWidth = LadderLayoutHelper.CalculateActualLadderWidth(GetVerticalLines());

        // 5. 스타트 버튼 생성 및 배치
        for (int i = 0; i < verticalCount; i++)
        {
            // 정확한 X 위치 계산 (실측 ladderWidth 사용)
            float x = LadderLayoutHelper.GetXPosition(i, actualLadderWidth, verticalCount);
            //Debug.log($"🟢 스타트 버튼 index={i}, x={x}");

            GameObject startButtonGO = Instantiate(startButtonPrefab, startButtonsParent);
            RectTransform rect = startButtonGO.GetComponent<RectTransform>();

            // anchor 및 pivot 설정 (중앙 기준)
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            rect.anchoredPosition = new Vector2(x, buttonY);

            // 버튼 스크립트 설정
            StartBettingButton btn = startButtonGO.GetComponent<StartBettingButton>();
            btn.startIndex = i;
            startButtons.Add(btn);

            // 텍스트 라벨 설정
            Text label = startButtonGO.GetComponentInChildren<Text>();
            if (label != null)
                label.text = $"S{i + 1}";
        }
    }

    /// <summary>
    /// 모든 Start 버튼 색상을 초기화 (선택 해제)
    /// </summary>
    public void ResetAllStartButtonColors()
    {
        foreach (var button in startButtons)
        {
            if (button != null)
                button.ResetColor(); // StartBettingButton 클래스 내부 함수
        }
        selectedStartButton = null;
        selectedStartIndex = -1;
    }

    /// <summary>
    /// 골 버튼이 선택되었는지 여부 반환
    /// </summary>
    public bool IsGoalSelected()
    {
        return selectedGoalButton != null;
    }

    /// <summary>
    /// 결과 메시지 텍스트에 표시
    /// </summary>
    public void ShowResultMessage(string message)
    {
        if (resultText != null)
            resultText.text = message;
    }

    /// <summary>
    /// 모든 스타트 버튼을 활성화 또는 비활성화
    /// </summary>
    private void SetStartButtonsInteractable(bool isOn)
    {
        foreach (var btn in startButtons)
        {
            Button uiButton = btn.GetComponent<Button>();
            if (uiButton != null)
                uiButton.interactable = isOn;
        }
    }

    // ✅ BetAmountUIManager에서 배팅 확정되었을 때 호출되는 핸들러
    private void OnBetConfirmedHandler(int betAmount)
    {
        Debug.Log($"💰 배팅 금액 확정됨: {betAmount}원");

        // 금액이 0이면 결과 버튼 비활성화
        if (betAmount <= 0)
        {
            resultButton.interactable = false;
            if (boardText != null) boardText.text = "배팅 금액을 설정하세요!";
            return;
        }

        // 금액이 1 이상이면 준비 상태로 전환
        resultButton.interactable = true;
        resultButton.GetComponentInChildren<Text>().text = "READY";

        // 예시: 확인 버튼 활성화, 로그 표시 등 필요한 로직 여기에 작성
        // resultButton.interactable = true;
    }

    public bool IsInReadyState()
    {
        var text = resultButton?.GetComponentInChildren<Text>();
        return text != null && text.text == "READY";
    }
}
