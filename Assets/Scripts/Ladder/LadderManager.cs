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
    [Header("배당 설정")]
    [Tooltip("골/스타트 배율 결정에 곱해지는 계수 (예: 0.5이면 2줄 × 0.5 = 1X)")]
    public float goalMultiplierFactor = 0.5f;
    public float startMultiplierFactor = 1.0f; // 필요시 따로 분리

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
    public Text rewardText;

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

        if (rewardText != null)
            rewardText.gameObject.SetActive(false);

        // GenerateLadder() 끝 부분에 추가
        if (betAmountUIManager != null)
            betAmountUIManager.SetInteractable(false); // 🔒 사다리 생성 후 배팅 비활성화
    }

    /// <summary>
    /// 결과 버튼(GO) 클릭 시 실행
    /// - 플레이어 생성 및 이동 시작
    /// </summary>
    public void OnResultButtonClicked()
    {
        if (playerMover.IsMoving())
            return;

        if (selectedGoalButton == null)
        {
            if (boardText != null) boardText.text = "도착 지점을 선택하세요!";
            return;
        }

        if (board != null) board.SetActive(false);

        // ⭐ startIndex를 여기에 정의해야 함!
        int startIndex = selectedStartIndex >= 0
            ? selectedStartIndex
            : Random.Range(0, verticalCount);

        // ⭐ 스타트 버튼 텍스트 초기화
        foreach (var btn in startButtons)
        {
            Text label = btn.GetComponentInChildren<Text>();
            if (label != null) label.text = "";
        }

        // ✅ 모든 버튼 색상 초기화 (이후 강조할 버튼은 따로 처리)
        ResetAllStartButtonColors();

        // ⭐ 랜덤 선택인 경우 강조 적용
        if (selectedStartIndex < 0 && startIndex >= 0 && startIndex < startButtons.Count)
        {
            selectedStartButton = startButtons[startIndex];
            selectedStartButton.HighlightWithColor(Color.yellow); // ✅ 노란색으로 강조
        }

        // 기존 플레이어 제거
        if (playerTransform != null)
        {
            playerMover.StopMove(this);
            Destroy(playerTransform.gameObject);
            playerTransform = null;
        }

        // 플레이어 프리팹 생성
        if (playerPrefab == null) return;

        GameObject playerGO = Instantiate(playerPrefab, ladderRoot);
        playerTransform = playerGO.transform;

        float x = LadderLayoutHelper.GetXPosition(startIndex, ladderWidth, verticalCount);
        float y = LadderLayoutHelper.GetStartY(stepCount, stepHeight);
        RectTransform rect = playerTransform.GetComponent<RectTransform>();

        if (rect != null)
            rect.anchoredPosition = new Vector2(x, y);

        // 이동 세팅 및 실행
        playerMover.Setup(playerTransform, startIndex, 500f);
        playerMover.SetFinishCallback(CheckResult);
        playerMover.StartMove(this);

        // 결과 버튼 비활성화
        resultButton.interactable = false;

        // ✅ 기대값 텍스트 숨김
        if (rewardText != null)
            rewardText.gameObject.SetActive(false);
    }


    /// <summary>
/// 플레이어 도착 후 결과 처리
/// - 성공 여부 판단 및 최종 보상 계산
/// </summary>
private void CheckResult(int arrivedIndex)
{
    int goalIndex = generator.GetSelectedDestination();
    int betAmount = betAmountUIManager != null ? betAmountUIManager.GetBetAmount() : 0;

    // 🔢 배율 계산 (float로 처리)
    float goalMultiplier = verticalCount * goalMultiplierFactor;
    float startMultiplier = verticalCount * startMultiplierFactor;

    // ✅ 수동 선택 시 스타트 배율, 랜덤 시 골 배율 적용
    float finalMultiplier = selectedStartIndex >= 0 ? startMultiplier : goalMultiplier;

    float reward = betAmount * finalMultiplier;
    bool isSuccess = arrivedIndex == goalIndex;

    if (betAmountUIManager != null)
        betAmountUIManager.SetInteractable(true); // 배팅 UI 재활성화

    if (rewardText != null)
        rewardText.gameObject.SetActive(false); // 기대값 텍스트 숨김

    // 로그 출력
    Debug.Log($"🎯 도착 인덱스: {arrivedIndex}, 목표 인덱스: {goalIndex}");
    Debug.Log($"✅ 배팅 금액: {betAmount} 코인");
    Debug.Log($"✅ 적용 배율: {finalMultiplier:0.0}X");
    Debug.Log($"💰 최종 보상: {reward:0.0} 코인");
    Debug.Log(isSuccess ? "🎉 성공!" : "❌ 실패!");

    // 결과 텍스트 출력
    resultText.text = isSuccess
        ? $"🎉 성공! 보상 {reward:0.0}코인"
        : $"❌ 실패! 보상 {reward:0.0}코인";

    resultButton.interactable = true;
    resultButton.GetComponentInChildren<Text>().text = "READY";
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

    /// <summary>
    /// 골 버튼 클릭 시 처리 함수
    /// - 강조 색상 적용 및 Dim 처리
    /// - 텍스트 숨김/표시
    /// - 기대값 텍스트 갱신
    /// </summary>
    public void HighlightSelectedGoalButton(GoalBettingButton clickedButton)
    {
        // ✅ 같은 버튼을 다시 클릭한 경우 → 선택 해제
        if (selectedGoalButton == clickedButton)
        {
            // 색상 및 텍스트 초기화
            clickedButton.ResetColor();
            clickedButton.SetTextVisible(true);
            selectedGoalButton = null;

            // 모든 골 버튼 텍스트 다시 보이게
            foreach (var btn in destinationButtons)
                btn.SetTextVisible(true);

            // 결과 버튼 비활성화
            resultButton.interactable = false;

            // 보드 텍스트 초기화
            if (rewardText != null)
                rewardText.gameObject.SetActive(false);

            return;
        }

        // ✅ 기존 선택 골 버튼 초기화
        if (selectedGoalButton != null)
        {
            selectedGoalButton.ResetColor();           // 색상 복원
            selectedGoalButton.SetTextVisible(true);   // 텍스트 다시 표시
        }

        // ✅ 새로 선택된 골 버튼 강조 (노란색)
        clickedButton.HighlightWithColor(Color.yellow);
        clickedButton.SetTextVisible(true);

        // ✅ 나머지 골 버튼 Dim 처리 + 텍스트 숨김
        DimOtherGoalButtons(clickedButton);

        // ✅ 현재 선택된 골 버튼으로 등록
        selectedGoalButton = clickedButton;

        // ✅ 스타트 버튼 활성화
        SetStartButtonsInteractable(true);

        // ✅ 결과 버튼 텍스트 → "GO", 버튼 활성화
        var txt = resultButton.GetComponentInChildren<Text>();
        if (txt != null) txt.text = "GO";
        resultButton.interactable = true;

        // ✅ 스타트 버튼 배율 갱신 (골 선택 후)
        UpdateStartButtonMultiplierTexts();

        // ✅ 기대값 텍스트 갱신 (골 배율만 사용)
        if (rewardText != null && betAmountUIManager != null)
        {
            int betAmount = betAmountUIManager.GetBetAmount();               // 현재 배팅 금액
            float goalMultiplier = verticalCount * goalMultiplierFactor;     // 예: 3×0.9 = 2.7
            float expectedReward = betAmount * goalMultiplier;

            rewardText.text = $"기대값: {expectedReward:F1} 코인";
            rewardText.gameObject.SetActive(true);
        }
    }

    public void OnResultButtonPressed()
    {
        
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

            // 결과 실행 전 기대값 텍스트 숨기기
            if (rewardText != null)
                rewardText.gameObject.SetActive(false);

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
            {
                button.Dim();
                button.SetTextVisible(false); // ✅ 텍스트 숨김
            }
        }
    }

    public void ResetAllGoalButtonColors()
    {
        foreach (var button in destinationButtons)
        {
            if (button != null)
            {
                button.ResetColor();            // ✅ 색상 복구
                button.SetTextVisible(true);    // ✅ 텍스트 복구
            }
        }
        selectedGoalButton = null;

    }

    /// <summary>
    /// 도착 지점(골 버튼) 생성 및 배치, 배율 표시(float)
    /// </summary>
    public void InitializeDestinationButtons(int verticalCount)
    {
        // 기존 버튼 제거
        foreach (Transform child in destinationButtonsParent)
            Destroy(child.gameObject);
        destinationButtons.Clear();

        for (int i = 0; i < verticalCount; i++)
        {
            // 프리팹 생성
            GameObject buttonGO = Instantiate(destinationButtonPrefab, destinationButtonsParent);

            // 위치 계산 및 배치
            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            float x = LadderLayoutHelper.GetXPosition(i, ladderWidth, verticalCount);
            rect.anchoredPosition = new Vector2(x, -300f);

            // 컴포넌트 설정
            GoalBettingButton btn = buttonGO.GetComponent<GoalBettingButton>();
            btn.destinationIndex = i;

            // 배율 계산: 세로줄 수 × 계수
            float multiplier = verticalCount * goalMultiplierFactor;
            btn.SetMultiplierText(multiplier);  // float 처리

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

    /// <summary>
    /// 스타트 버튼이 선택되었을 때 실행되는 함수
    /// - 선택한 버튼은 주황색으로 하이라이트
    /// - 나머지 버튼은 Dim 처리 (흰색 등)
    /// - 배당 텍스트는 goalFactor × startFactor × 세로줄² 반영
    /// </summary>
    public void HighlightSelectedStartButton(StartBettingButton selectedButton)
    {
        // 🔁 같은 버튼을 다시 눌렀다면 선택 해제
        if (this.selectedStartButton == selectedButton)
        {
            // 기존 색상 초기화
            selectedStartButton.ResetColor();
            ResetAllStartButtonColors(); // 모든 텍스트 및 색 초기화
            selectedStartButton = null;
            selectedStartIndex = -1;

            // ✅ 기대값 텍스트 다시 골 기준으로 출력
            if (rewardText != null && selectedGoalButton != null && betAmountUIManager != null)
            {
                float goalFactor = goalMultiplierFactor;
                float multiplier = verticalCount * goalFactor;
                int bet = betAmountUIManager.GetBetAmount();
                rewardText.text = $"기대값: {(bet * multiplier):F1} 코인";
            }

            return;
        }

        // ✅ 선택한 스타트 버튼은 노란으로 강조
        selectedButton.HighlightWithColor(Color.yellow);

        // ✅ 다른 버튼들은 흰색 또는 흐린 색으로 처리
        DimOtherStartButtons(selectedButton);

        // ✅ 선택 상태 저장
        this.selectedStartButton = selectedButton;
        this.selectedStartIndex = selectedButton.startIndex;

        // ✅ 텍스트 업데이트: goalFactor × startFactor × vertical²
        if (rewardText != null && betAmountUIManager != null)
        {
            float gFactor = goalMultiplierFactor;
            float sFactor = startMultiplierFactor;
            float multiplier = gFactor * sFactor * (verticalCount * verticalCount);
            int bet = betAmountUIManager.GetBetAmount();
            rewardText.text = $"기대값: {(bet * multiplier):F1} 코인";
        }

        // ✅ 텍스트도 해당 배율로 업데이트
        UpdateStartButtonMultiplierTexts();
    }

    /// <summary>
    /// 선택된 스타트 버튼을 제외한 나머지를 dim 처리 (흰색으로 변경)
    /// </summary>
    private void DimOtherStartButtons(StartBettingButton selectedButton)
    {
        foreach (var btn in startButtons)
        {
            if (btn != null && btn != selectedButton)
            {
                btn.HighlightWithColor(Color.white); // 흰색으로 설정
            }
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

            // 프리팹 인스턴스화 및 부모 설정
            GameObject startButtonGO = Instantiate(startButtonPrefab, startButtonsParent);
            RectTransform rect = startButtonGO.GetComponent<RectTransform>();

            // 중앙 기준 위치 정렬
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, buttonY);

            // 버튼 스크립트 및 인덱스 설정
            StartBettingButton btn = startButtonGO.GetComponent<StartBettingButton>();
            btn.startIndex = i;
            startButtons.Add(btn);

            // ✅ 스타트 버튼 배율 계산
            // 배율 계산
            int multiplier = Mathf.RoundToInt(verticalCount * startMultiplierFactor);

            // 텍스트 UI에 설정
            Text label = startButtonGO.GetComponentInChildren<Text>();
            if (label != null)
                label.text = $"{multiplier}X";
        }
    }

    /// <summary>
    /// 모든 스타트 버튼의 색상과 텍스트를 초기화함
    /// </summary>
    public void ResetAllStartButtonColors()
    {
        foreach (var btn in startButtons)
        {
            if (btn != null)
            {
                btn.ResetColor(); // 색 초기화 (통상 white 또는 default)
                Text label = btn.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = ""; // 텍스트 제거 (선택 해제 시)
                }
            }
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

    public void SetMultiplierText(int multiplier)
    {
        Text label = GetComponentInChildren<Text>();
        if (label != null)
            label.text = $"{multiplier}X";
    }

    /// <summary>
    /// 모든 스타트 버튼의 텍스트에 정확한 배당률 출력
    /// - 골 × 스타트 × 세로줄^2
    /// </summary>
    private void UpdateStartButtonMultiplierTexts()
    {
        float gFactor = goalMultiplierFactor;
        float sFactor = startMultiplierFactor;

        float multiplier = gFactor * sFactor * (verticalCount * verticalCount);

        foreach (var btn in startButtons)
        {
            Text label = btn.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = $"{multiplier:F1}X";
            }
        }
    }
}
