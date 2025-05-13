using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// LadderManager
/// - 사다리 UI 생성, 결과 판단, 버튼 인터랙션 등을 총괄하는 컨트롤러 클래스
/// - LadderGenerator, PlayerMover와 상호작용하며 사다리 게임 흐름을 관리
/// </summary>
public class LadderManager : MonoBehaviour
{
    [Header("사다리 관련 클래스")]
    public LadderGenerator ladderGenerator;
    public PlayerMover playerMover;

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
    public TMP_Text resultText;

    [Header("보드 UI")]
    public GameObject board;               // 보드 패널
    public TMP_Text boardText;
    public TMP_Text rewardText;

    [Header("결과 처리 UI")]
    public ResultUIManager resultUIManager; // 결과 처리 통합 관리자
    public TMP_Text resultMessageText;      // 🔴 결과 메시지용

    [Header("세로/가로줄 수 조절 UI")]
    public Button increaseVerticalButton;
    public Button decreaseVerticalButton;
    //public Button increaseHorizontalButton;
    //public Button decreaseHorizontalButton;
    //public Toggle randomizeToggle;
    public TMP_Text verticalCountText;
    public TMP_Text horizontalLineCountText;

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

    [Header("코인 관련")]
    public float currentCoin = 100; // 기본 보유 코인
    public TMP_Text coinTextUI;       // 인스펙터에서 연결할 텍스트 오브젝트

    private LadderGenerator generator;          // 사다리 생성기
    //private PlayerMover playerMover;            // 플레이어 이동기
    private GameObject spawnedPlayer;           // 현재 생성된 플레이어 오브젝트
    private List<Button> betButtons = new(); // 모든 배팅 버튼을 리스트로 관리

    private GoalBettingButton selectedGoalButton = null;                // 선택된 골 버튼 참조
    private List<GoalBettingButton> destinationButtons = new();        // 모든 골 버튼 리스트
    private List<RectTransform> verticalLines = new();

    [Header("출발 버튼 관련")]
    public GameObject startButtonPrefab;             // 출발 버튼 프리팹
    public Transform startButtonsParent;             // 출발 버튼들을 담을 부모 오브젝트

    [SerializeField] private BetAmountUIManager betUIManager;
    [SerializeField] private TextMeshProUGUI resultButtonLabel;

    public BetAmountUIManager betAmountUIManager;

    private bool isLadderGenerated = false;  // READY 상태 → GO 상태 전환 여부
    public float ladderWidth = 800f;

    private void Start()
    {
        generator = new LadderGenerator(this);
        playerMover = new PlayerMover(this);

        // 🔍 필수 컴포넌트 연결 여부 확인
        if (resultButton == null) Debug.LogError("🚨 resultButton 연결 오류");
        //if (generateButton == null) Debug.LogError("🚨 generateButton 연결 오류");
        if (ladderRoot == null) Debug.LogError("🚨 ladderRoot 연결 오류");
        if (startButtonPrefab == null || startButtonsParent == null) Debug.LogError("🚨 Start 버튼 관련 프리팹 누락");
        if (destinationButtonPrefab == null || destinationButtonsParent == null) Debug.LogError("🚨 Destination 버튼 프리팹 누락");

        // 🔧 결과 UI 초기 숨김
        if (resultUIManager != null) resultUIManager.Hide();

        // 🔗 배팅 UI 이벤트 연결
        if (betAmountUIManager != null)
            betAmountUIManager.OnBetConfirmed += OnBetConfirmedHandler;
        else
            Debug.LogError("🚨 BetAmountUIManager 연결 안됨");

        // ⛑ TMP 자동 연결 보정
        if (resultButtonLabel == null)
            resultButtonLabel = resultButton.GetComponentInChildren<TextMeshProUGUI>();

        // ✅ UI 버튼 이벤트 등록
        SetupUI();
        generateButton?.onClick.AddListener(GenerateLadder);
        resultButton?.onClick.AddListener(OnResultButtonPressed);

        UpdateVerticalCountText();

        // ✅ 상태 초기화: "READY" + 비활성화
        SetResultButtonState("READY", false);

        // ✅ 사다리 생성기 초기화
        if (ladderGenerator == null)
            ladderGenerator = new LadderGenerator(this);
        ladderGenerator.Initialize(this);

        // ✅ 코인 UI 갱신
        UpdateCoinUI();

        // ✅ 안내 메시지 출력
        if (board != null) board.SetActive(true);
        if (boardText != null) boardText.text = "INPUT YOUR BET AMOUNT.";
    }

    //private void OnBetAmountConfirmed(int amount)
    //{
    //    Debug.Log($"💰 확정된 배팅 금액: {amount}");
    //    // 내부 게임 로직에서 활용
    //}

    /// <summary>
    /// 버튼 및 토글과 이벤트 연결 초기화
    /// </summary>
    private void SetupUI()
    {
        increaseVerticalButton?.onClick.AddListener(IncreaseVerticalCount);
        decreaseVerticalButton?.onClick.AddListener(DecreaseVerticalCount);
        //increaseHorizontalButton?.onClick.AddListener(IncreaseHorizontalLineCount);
        //decreaseHorizontalButton?.onClick.AddListener(DecreaseHorizontalLineCount);
        //randomizeToggle?.onValueChanged.AddListener(OnRandomizeToggleChanged);
    }

    /// <summary>
    /// 사다리 생성 함수 (READY 버튼 클릭 시 호출됨)
    /// - 세로줄만 생성하고 가로줄은 이후 GO 버튼에서 생성됨
    /// - 보드 메시지, 결과 버튼 상태 등 초기화 포함
    /// </summary>
    public void GenerateLadder()
    {
        // ✅ 1. 세로줄만 먼저 생성 (가로줄은 GO 버튼 클릭 시 생성됨)
        ladderGenerator.GenerateVerticalLines(verticalCount, stepCount);

        // ✅ 2. 이전 라운드의 골 버튼 상태 초기화 (색상 및 텍스트 리셋)
        ResetAllGoalButtonColors();

        // ✅ 3. 도착 버튼 및 출발 버튼 위치 생성 및 배치
        InitializeDestinationButtons(verticalCount);
        InitializeStartButtons(verticalCount);

        // ✅ 4. 보드 UI 활성화 및 기본 메시지 출력
        if (board != null) board.SetActive(true);
        if (boardText != null)
        {
            boardText.gameObject.SetActive(true);
            boardText.text = "CHOOSE YOUR DESTINATION!";
        }

        // ✅ 5. 현재 배팅 금액 확인
        int currentBet = betAmountUIManager != null ? betAmountUIManager.GetBetAmount() : 0;

        // ✅ 6. 결과 버튼 상태 결정 (READY 텍스트는 유지, 활성화 여부만 분기)
        if (currentBet <= 0)
        {
            // ❌ 금액 없음: 버튼 비활성화 + 보드 메시지 안내
            SetResultButtonState("READY", false);
            if (boardText != null)
                boardText.text = "SET YOUR BET AMOUNT!";
        }
        else
        {
            // ✅ 금액 있음: 결과 버튼은 "GO"가 아닌 "READY" 텍스트, 활성화 유지
            SetResultButtonState("READY", true);
            if (boardText != null)
                boardText.text = "CHOOSE YOUR DESTINATION!!";
        }

        // ✅ 7. 스타트 버튼은 초기에는 비활성화 (골 선택 후에만 활성화)
        SetStartButtonsInteractable(false);

        // ✅ 8. 기대 보상 텍스트는 숨김 처리
        if (rewardText != null)
            rewardText.gameObject.SetActive(false);

        // ✅ 9. 배팅 UI는 결과 실행(GO)까지는 비활성화 유지
        if (betAmountUIManager != null)
            betAmountUIManager.SetInteractable(false);

        // ✅ 10. 결과 패널은 새 라운드 시작 시 항상 숨김 처리
        if (resultUIManager != null)
            resultUIManager.Hide();
    }

    /// <summary>
    /// 결과 버튼(GO) 클릭 시 실행되는 함수
    /// - 사다리의 가로줄을 생성하고 플레이어를 이동시킴
    /// - 도착 인덱스를 확인 후 결과 처리로 이어짐
    /// </summary>
    public void OnResultButtonClicked()
    {
        // 🔒 이미 이동 중인 경우 중복 실행 방지
        if (playerMover.IsMoving())
            return;

        // ❗ 도착(골) 버튼이 선택되지 않은 경우 경고 메시지 출력
        if (selectedGoalButton == null)
        {
            if (boardText != null)
                boardText.text = "CHOOSE YOUR DESTINATION!";
            return;
        }

        // 📴 결과 버튼 즉시 비활성화 (중복 클릭 방지) + 상태 텍스트 "WAIT"
        SetResultButtonState("WAIT", false);

        // 📕 보드 UI 숨김
        if (board != null)
            board.SetActive(false);

        // ⭐ 시작 위치 인덱스 결정
        // - 선택된 인덱스가 있으면 그것을 사용
        // - 없으면 무작위로 하나 선택
        int startIndex = selectedStartIndex >= 0
            ? selectedStartIndex
            : Random.Range(0, verticalCount);

        // 🧹 기존 스타트 버튼들의 텍스트 제거 및 색상 초기화
        foreach (var btn in startButtons)
        {
            Text label = btn.GetComponentInChildren<Text>();
            if (label != null) label.text = "";
        }
        ResetAllStartButtonColors();

        // 🟡 랜덤 선택된 경우 해당 스타트 버튼을 노란색으로 강조
        if (selectedStartIndex < 0 && startIndex >= 0 && startIndex < startButtons.Count)
        {
            selectedStartButton = startButtons[startIndex];
            selectedStartButton.HighlightWithColor(Color.yellow);
        }

        // 🗑 이전 라운드에서 생성된 플레이어 오브젝트 제거
        if (playerTransform != null)
        {
            playerMover.StopMove(this);                         // 이동 중지
            Destroy(playerTransform.gameObject);                // 삭제
            playerTransform = null;
        }

        // ✅ 가로줄 생성: 보장된 규칙에 따라 랜덤 개수 생성
        int min = verticalCount - 1;
        int max = verticalCount + 3;
        int horizontalLineCount = Random.Range(min, max + 1);
        ladderGenerator.SetupHorizontalLines(verticalCount, stepCount, horizontalLineCount, true);

        // 🎮 플레이어 프리팹 유효성 검사
        if (playerPrefab == null) return;

        // 🎮 플레이어 오브젝트 생성 및 사다리 위에 배치
        GameObject playerGO = Instantiate(playerPrefab, ladderRoot);
        playerTransform = playerGO.transform;

        // 🎯 정확한 시작 위치 계산 (선택한 세로줄의 최상단 기준)
        RectTransform verticalLine = GetVerticalLineAt(startIndex);
        float x = verticalLine.anchoredPosition.x;
        float y = verticalLine.anchoredPosition.y + verticalLine.sizeDelta.y / 2f;

        RectTransform rect = playerTransform.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = new Vector2(x, y);

        // ▶ 플레이어 이동 준비 및 시작
        playerMover.Setup(playerTransform, startIndex, 500f);     // 이동 대상, 시작 인덱스, 속도
        playerMover.SetFinishCallback(CheckResult);               // 도착 후 결과 처리 콜백 등록
        playerMover.StartMove(this);                              // 이동 시작

        // 🧊 보상 기대값 텍스트 숨김
        if (rewardText != null)
            rewardText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 플레이어 도착 후 결과 판단 및 보상 지급 처리
    /// </summary>
    private void CheckResult(int arrivedIndex)
    {
        // ✅ 골 버튼 인덱스 가져오기
        int goalIndex = generator.GetSelectedDestination();

        // ✅ 현재 배팅 금액 (float로 유지)
        float betAmount = betAmountUIManager != null ? betAmountUIManager.GetBetAmount() : 0f;

        // ✅ 골/스타트 배율 계산
        float goalMultiplier = verticalCount * goalMultiplierFactor;
        float startMultiplier = verticalCount * verticalCount * startMultiplierFactor;

        // ✅ 스타트 버튼 수동 선택 여부 확인
        bool hasSelectedStart = selectedStartIndex >= 0;

        // ✅ 최종 배율 결정
        float finalMultiplier = hasSelectedStart ? startMultiplier : goalMultiplier;

        // ✅ 보상 계산 (실수 그대로 유지)
        float reward = betAmount * finalMultiplier;

        // ✅ 성공 여부 판단
        bool isSuccess = arrivedIndex == goalIndex;

        // ✅ 결과 패널 표시
        if (resultUIManager != null)
        {
            // 실패 시 보상은 0 처리
            if (!isSuccess)
            {
                reward = 0f;
            }

            // 메시지 작성
            string message = isSuccess
                ? $"YOU DID IT! Claim your {reward:F1} Coins"
                : "OH NO! Better luck next time!";

            resultUIManager.ShowResult(message);

            // ✅ 보유 코인 증감 (float 단위로 처리)
            if (isSuccess)
            {
                AddCoin(reward); // 성공 시 보상 추가
            }
            else
            {
                AddCoin(-betAmount); // 실패 시 배팅 금액 차감
            }
        }

        // ✅ 결과 패널이 열려 있으면 → 버튼은 READY지만 비활성화 상태
        bool resultVisible = resultUIManager != null && resultUIManager.IsResultVisible();
        SetResultButtonState("READY", !resultVisible); // 열려 있으면 버튼 비활성화

        // ✅ 배팅 UI 다시 사용 가능하게 설정
        if (betAmountUIManager != null)
        {
            betAmountUIManager.SetInteractable(true);
        }

        // ✅ 기대 보상 텍스트 숨기기
        if (rewardText != null)
        {
            rewardText.gameObject.SetActive(false);
        }
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
    /// 골 버튼 클릭 시 호출됨
    /// - 동일한 버튼 클릭 시 선택 해제
    /// - 새로운 골 버튼 선택 시 강조 및 결과 버튼 활성화
    /// </summary>
    public void HighlightSelectedGoalButton(GoalBettingButton clickedButton)
    {
        // ✅ 동일한 골 버튼을 다시 클릭 → 선택 해제
        if (selectedGoalButton == clickedButton)
        {
            clickedButton.ResetColor();                 // 색상 원상 복구
            clickedButton.SetTextVisible(true);         // 텍스트 다시 표시
            selectedGoalButton = null;

            // 모든 골 버튼의 텍스트 다시 보이게
            foreach (var btn in destinationButtons)
                btn.SetTextVisible(true);

            // ❌ 골 선택 해제 상태 → 결과 버튼은 "READY" 상태로 전환 + 비활성화
            SetResultButtonState("READY", false);

            // 기대 보상 텍스트 숨기기
            if (rewardText != null)
                rewardText.gameObject.SetActive(false);

            // 스타트 버튼도 선택 불가 상태로 전환
            SetStartButtonsInteractable(false);

            return;
        }

        // ✅ 이전에 선택된 골 버튼이 있다면 초기화
        if (selectedGoalButton != null)
        {
            selectedGoalButton.ResetColor();         // 색상 초기화
            selectedGoalButton.SetTextVisible(true); // 텍스트 표시 복원
        }

        // ✅ 새로 선택된 골 버튼 강조 및 등록
        clickedButton.HighlightWithColor(Color.yellow); // 강조 색상 적용
        clickedButton.SetTextVisible(true);             // 텍스트 표시
        DimOtherGoalButtons(clickedButton);             // 나머지 버튼 흐리게

        selectedGoalButton = clickedButton;             // 현재 선택 상태 저장

        // ✅ 스타트 버튼 활성화 (이제 선택 가능)
        SetStartButtonsInteractable(true);

        // ✅ 결과 버튼은 "GO" 상태로 활성화
        SetResultButtonState("GO", true);

        // ✅ 스타트 버튼 배당률 텍스트 갱신 (골 기준 + 스타트 계수 반영)
        UpdateStartButtonMultiplierTexts();

        // ✅ 기대 보상 텍스트 출력 (골 기준 배율 × 배팅 금액)
        if (rewardText != null && betAmountUIManager != null)
        {
            int betAmount = betAmountUIManager.GetBetAmount();
            float goalMultiplier = verticalCount * goalMultiplierFactor;
            float expectedReward = betAmount * goalMultiplier;

            rewardText.text = $"Expected: {expectedReward:F1} Coins";
            rewardText.gameObject.SetActive(true);
        }
    }

    public void OnResultButtonPressed()
    {
        // ✅ 결과 패널이 열려 있으면 아무 동작도 하지 않음 (강제 차단)
        if (resultUIManager != null && resultUIManager.IsResultVisible())
        {
            Debug.LogWarning("⛔ 결과창이 열려 있는 동안 결과 버튼은 비활성화되어야 합니다.");
            return;
        }

        var labelComponent = resultButton.GetComponentInChildren<TextMeshProUGUI>();
        if (labelComponent == null)
        {
            Debug.LogError("❌ resultButton에 TextMeshProUGUI가 연결되어 있지 않습니다.");
            return;
        }

        string label = labelComponent.text;

        if (label == "READY")
        {
            GenerateLadder();
            labelComponent.text = "GO";

            // 골이 선택되었는지에 따라 버튼 활성화 여부 결정
            if (selectedGoalButton == null)
                SetResultButtonState("GO", false); // 골 선택 전 → GO 상태지만 비활성화
            else
                SetResultButtonState("GO", true);

            isLadderGenerated = true;
        }
        else if (label == "GO")
        {
            if (selectedGoalButton == null)
            {
                if (boardText != null)
                    boardText.text = "CHOOSE YOUR DESTINATION!";
                return;
            }

            if (rewardText != null)
                rewardText.gameObject.SetActive(false);

            OnResultButtonClicked();

            SetResultButtonState("WAIT", false);
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

        // 🔽 골 버튼의 Y 위치 계산: 사다리 맨 아래 기준
        float bottomY = LadderLayoutHelper.GetYPosition(stepCount, stepCount, stepHeight);
        float goalButtonY = bottomY - 100f; // 약간 아래에 위치 (100f는 여백)

        for (int i = 0; i < verticalCount; i++)
        {
            // 프리팹 생성
            GameObject buttonGO = Instantiate(destinationButtonPrefab, destinationButtonsParent);

            // 위치 계산 및 배치
            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            float x = LadderLayoutHelper.GetXPosition(i, ladderWidth, verticalCount);
            //rect.anchoredPosition = new Vector2(x, -300f);
            rect.anchoredPosition = new Vector2(x, goalButtonY); // 🔁 고정값 -300f → 계산된 Y값 사용

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
        return ladderGenerator.ladderMap != null &&
               y >= 0 && y < ladderGenerator.ladderMap.GetLength(0) &&
               x >= 0 && x < ladderGenerator.ladderMap.GetLength(1) &&
               ladderGenerator.ladderMap[y, x];
    }

    /// <summary>
    /// 생성된 세로줄 오브젝트 리스트를 저장
    /// </summary>
    public void SetVerticalLines(List<RectTransform> lines)
    {
        verticalLines = lines;
    }

    private void UpdateVerticalCountText()
    {
        if (verticalCountText != null)
            verticalCountText.text = $"Vertical Lines Count: {verticalCount}";
    }

    /// <summary>
    /// 세로줄 개수 증가
    /// - 최대 5줄까지 제한
    /// - 증가 시 상태 초기화 및 안내 메시지 출력
    /// </summary>
    private void IncreaseVerticalCount()
    {
        if (verticalCount < 5)
        {
            verticalCount++;                         // 세로줄 개수 증가
            CorrectHorizontalLineCount();            // 가로줄 범위 보정
            UpdateVerticalCountText();               // 텍스트 갱신

            HandleVerticalCountChange();              // 상태 초기화
        }
    }

    /// <summary>
    /// 세로줄 개수 감소
    /// - 최소 2줄까지 제한
    /// - 감소 시 상태 초기화 및 안내 메시지 출력
    /// </summary>
    private void DecreaseVerticalCount()
    {
        if (verticalCount > 2)
        {
            verticalCount--;                         // 세로줄 개수 감소
            CorrectHorizontalLineCount();            // 가로줄 범위 보정
            UpdateVerticalCountText();               // 텍스트 갱신

            HandleVerticalCountChange();              // 상태 초기화
        }
    }

    /// <summary>
    /// 세로줄 변경 후 공통 처리 함수
    /// </summary>
    private void HandleVerticalCountChange()
    {
        SetResultButtonState("READY", true); // ✅ READY 상태 활성화
        if (boardText != null)
            boardText.text = "PRESS READY BUTTON";

        if (betAmountUIManager != null)
            betAmountUIManager.SetInteractable(false);
    }

    /// <summary>
    /// 세로줄 변경 시 공통 초기화 처리
    /// - 결과 버튼 상태 → READY & 비활성화
    /// - 보드 텍스트 → "PRESS READY BUTTON"
    /// - 배팅 UI도 비활성화
    /// </summary>
    //private void ResetAfterVerticalChange()
    //{
    //    // ✅ 결과 버튼 텍스트는 READY, 클릭은 막음
    //    SetResultButtonState("READY", false);

    //    // ✅ 보드 텍스트 안내
    //    if (boardText != null)
    //    {
    //        boardText.gameObject.SetActive(true);
    //        boardText.text = "PRESS READY BUTTON";
    //    }

    //    // ✅ 배팅 UI 비활성화 (READY 이후에 다시 활성화)
    //    if (betAmountUIManager != null)
    //        betAmountUIManager.SetInteractable(false);
    //}
    
    private void CorrectHorizontalLineCount()
    {
        int min = verticalCount - 1;
        int max = verticalCount + 3;
        horizontalLineCount = Mathf.Clamp(horizontalLineCount, min, max);
    }

    //private void UpdateHorizontalLineCountText()
    //{
    //    if (horizontalLineCountText != null)
    //        horizontalLineCountText.text = $"가로줄 개수: {horizontalLineCount}";
    //}

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
                rewardText.text = $"Expected: {(bet * multiplier):F1} Coins";
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
            rewardText.text = $"Expected: {(bet * multiplier):F1} Coins";
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
        if (verticalLines.Count < verticalCount)
        {
            Debug.LogError($"🚨 verticalLines 개수가 부족합니다! 현재: {verticalLines.Count}, 기대값: {verticalCount}");
            return;
        }

        if (startButtonsParent == null || startButtonPrefab == null)
            return;

        foreach (Transform child in startButtonsParent)
            Destroy(child.gameObject);
        startButtons.Clear();

        //float buttonY = 300f;
        // ✅ 사다리의 맨 위 Y 위치를 계산하여 약간 위에 버튼 배치
        float topY = LadderLayoutHelper.GetYPosition(0, stepCount, stepHeight);
        float buttonY = topY + 100f; // 위쪽 여백 추가


        // ✅ 세로줄의 RectTransform 기준으로 직접 위치 참조
        for (int i = 0; i < verticalCount; i++)
        {
            // ⛳ 세로줄 X 위치 직접 가져오기
            float x = verticalLines[i].anchoredPosition.x;
            Debug.Log($"🚩 StartButton #{i} X pos: {x}");

            GameObject startButtonGO = Instantiate(startButtonPrefab, startButtonsParent);
            RectTransform rect = startButtonGO.GetComponent<RectTransform>();

            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, buttonY);

            StartBettingButton btn = startButtonGO.GetComponent<StartBettingButton>();
            btn.startIndex = i;
            startButtons.Add(btn);

            // 배율 표시
            int multiplier = Mathf.RoundToInt(verticalCount * startMultiplierFactor);
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
        if (resultMessageText != null)
            resultMessageText.text = message;

        if (resultMessageText != null)
        {
            resultMessageText.text = message;
            resultMessageText.gameObject.SetActive(true);     // ⬅ 텍스트 오브젝트 활성화
            resultMessageText.color = new Color(1, 1, 1, 1);      // ⬅ 알파값 보정
        }
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

    /// <summary>
    /// ✅ BetAmountUIManager에서 배팅 확정되었을 때 호출되는 핸들러
    /// - 코인에 따라 보드 메시지와 버튼 상태를 설정
    /// </summary>
    private void OnBetConfirmedHandler(int betAmount)
    {
        Debug.Log($"💰 배팅 확정: {betAmount} 코인");

        // ✅ 보드 텍스트가 비활성화 상태이면 강제로 보여줌
        if (boardText != null && !boardText.gameObject.activeInHierarchy)
        {
            boardText.gameObject.SetActive(true);
        }

        // ✅ 결과 버튼 텍스트 객체 가져오기
        var txt = resultButton.GetComponentInChildren<TextMeshProUGUI>();

        // ❌ 베팅 금액이 0 이하인 경우: 결과 버튼 비활성화 및 안내 출력
        if (betAmount <= 0)
        {
            SetResultButtonState("DISABLED", false);

            if (boardText != null)
                boardText.text = "INPUT YOUR BET AMOUNT.";
            return;
        }

       // 🔵 버튼 텍스트 설정 및 READY 상태로 복구
        SetResultButtonState("READY", true);  // 텍스트와 활성화 처리

        // 🔵 보드에 Ready 버튼 누르라는 안내 출력
        if (boardText != null)
            boardText.text = "PRESS READY BUTTON";

        // ✅ 배팅 UI를 다시 비활성화 (Ready 이후에 활성화됨)
        if (betAmountUIManager != null)
            betAmountUIManager.SetInteractable(false);
    }

    public bool IsInReadyState()
    {
        var tmp = resultButton?.GetComponentInChildren<TextMeshProUGUI>();
        return tmp != null && tmp.text == "READY";
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
            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = $"{multiplier:F1}X";
            }
        }
    }

    /// <summary> 1
    /// 결과 버튼의 상태와 텍스트, 인터랙션 가능 여부를 설정한다.
    /// </summary>
    /// <param name="state">READY / GO / DISABLED 중 하나</param>
    //public void SetResultButtonState(string state, bool isInteractable)
    //{
    //    if (resultButton == null) return;

    //    var label = resultButton.GetComponentInChildren<TextMeshProUGUI>();
    //    if (label != null)
    //        label.text = state;

    //    resultButton.interactable = isInteractable;
    //}

    /// <summary>
    /// 보유 코인을 설정하고 UI 갱신
    /// </summary>
    public void SetCoin(float amount)
    {
        currentCoin = Mathf.Max(0, amount);
        UpdateCoinUI();

        // 🔄 퀵 배팅 버튼 업데이트
        if (betAmountUIManager != null)
            betAmountUIManager.UpdateQuickBetButtons(currentCoin);
    }

    /// <summary>
    /// 보유 코인을 증가 또는 감소시키고 UI 갱신
    /// </summary>
    public void AddCoin(float amount)
    {
        currentCoin = Mathf.Max(0, currentCoin + amount);
        UpdateCoinUI();

        // 🔄 퀵 배팅 버튼 업데이트
        if (betAmountUIManager != null)
            betAmountUIManager.UpdateQuickBetButtons(currentCoin);
    }

    /// <summary>
    /// 보유 코인 텍스트 UI 업데이트
    /// </summary>
    private void UpdateCoinUI()
    {
        Debug.Log($"💰 현재 보유 코인: {currentCoin}"); // 실제 float 값 확인

        if (coinTextUI != null)
        {
            // 💡 소수점 1자리까지 표현 (예: 102.7)
            coinTextUI.text = $"Balance: {currentCoin:F1}";
            Debug.Log($"🧪 표시 문자열: {currentCoin:F1}"); // 표시 문자열 확인
            
        }
    }

    // ✅ 정확한 함수 정의 예시
    public List<RectTransform> GetVerticalLines()
    {
        return verticalLines;
    }

    /// <summary>
    /// 결과창(ResultUIManager)이 닫힌 후 보드 메시지 및 결과 버튼 상태를 처리하는 함수
    /// </summary>
    public void HandlePostResultUIClosed()
    {
        // ✅ 항상 안내 메시지 출력
        if (boardText != null)
            boardText.text = "CHOOSE YOUR DESTINATION!";

        // ✅ 골 선택 여부에 따라 상태 처리
        if (selectedGoalButton != null)
            SetResultButtonState("GO");
        else
            SetResultButtonState("DISABLED"); // 골 선택 전에는 잠금
    }

    /// <summary>
    /// 결과 버튼 상태 텍스트 및 활성화 여부를 통합 설정
    /// </summary>
    public void SetResultButtonState(string state, bool isInteractable = true)
    {
        if (resultButton == null) return;

        TextMeshProUGUI label = resultButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = state;

        resultButton.interactable = isInteractable;
    }
       
}
