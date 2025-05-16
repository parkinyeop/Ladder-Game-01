using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

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
    public TMP_Text verticalCountText;
    
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
      private List<Button> betButtons = new(); // 모든 배팅 버튼을 리스트로 관리

    private GoalBettingButton selectedGoalButton = null;                // 선택된 골 버튼 참조
    private List<GoalBettingButton> destinationButtons = new();        // 모든 골 버튼 리스트
    private List<RectTransform> verticalLines = new();

    [Header("출발 버튼 관련")]
    public GameObject startButtonPrefab;             // 출발 버튼 프리팹
    public Transform startButtonsParent;             // 출발 버튼들을 담을 부모 오브젝트

    
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
        //else
        //    Debug.LogError("🚨 BetAmountUIManager 연결 안됨");

        // ⛑ TMP 자동 연결 보정
        if (resultButtonLabel == null)
            resultButtonLabel = resultButton.GetComponentInChildren<TextMeshProUGUI>();

        if (betAmountUIManager == null)
        {
            GameObject found = GameObject.FindWithTag("BetAmountUI");
            if (found != null)
                betAmountUIManager = found.GetComponent<BetAmountUIManager>();
        }

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

        if (rewardText != null)
            rewardText.gameObject.SetActive(false);

        // ✅ 안내 메시지 출력
        if (board != null) board.SetActive(true);
        if (boardText != null) boardText.text = "INPUT YOUR BET AMOUNT.";
        if (currentCoin <= 0 && boardText != null)
        {
            boardText.text = "NOT ENOUGH BALANCE";
        }

        DisableTMPTextRaycasts();
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

        float bet = betAmountUIManager != null ? betAmountUIManager.GetBetAmount() : 0f;

        if (boardText != null)
        {
            boardText.gameObject.SetActive(true); // 🔥 여기가 반드시 필요
            boardText.enabled = true;
            boardText.text = (bet <= 0)
                ? "SET YOUR BET AMOUNT!"
                : "CHOOSE YOUR DESTINATION!";
        }

        // ✅ 5. 현재 배팅 금액 확인
        float currentBet = betAmountUIManager != null ? betAmountUIManager.GetBetAmount() : 0f;

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
    /// 결과 버튼(GO) 클릭 시 실행되는 메인 함수
    /// - 조건 검사 후 플레이어 이동 실행
    /// - 플레이 상태에 따라 보드 메시지를 명확하게 출력
    /// </summary>
    public void OnResultButtonClicked()
    {
        float coin = currentCoin;
        float bet = betAmountUIManager.GetBetAmount();

        // ✅ [1] 잔고 부족 또는 배팅 미설정 상태
        if (coin < bet || bet <= 0f)
        {
            SetBoardMessage("NOT ENOUGH BALANCE");
            SetResultButtonState("DISABLED", false);
            if (rewardText != null) rewardText.gameObject.SetActive(false);
            return;
        }

        // ✅ [2] 중복 클릭 방지: 이동 중이면 무시
        if (playerMover.IsMoving()) return;

        // ✅ [3] 골 버튼 미선택 상태
        if (selectedGoalButton == null)
        {
            SetBoardMessage("CHOOSE YOUR DESTINATION!");
            return;
        }

        // ✅ [4] 이동 시작: 버튼 상태 → WAIT, 보드 유지
        SetResultButtonState("WAIT", false);
        if (boardText != null) board.SetActive(false);

        // ✅ [5] 시작 위치 설정 (선택 없으면 무작위)
        int startIndex = selectedStartIndex >= 0 ? selectedStartIndex : Random.Range(0, verticalCount);

        // ✅ [6] 스타트 버튼 UI 초기화
        foreach (var btn in startButtons)
        {
            Text label = btn.GetComponentInChildren<Text>();
            if (label != null) label.text = "";
        }
        ResetAllStartButtonColors();

        // ✅ [7] 무작위 스타트 선택 시 하이라이트
        if (selectedStartIndex < 0 && startIndex >= 0 && startIndex < startButtons.Count)
        {
            selectedStartButton = startButtons[startIndex];
            selectedStartButton.HighlightWithColor(Color.yellow);
        }

        // ✅ [8] 기존 플레이어 제거
        if (playerTransform != null)
        {
            playerMover.StopMove(this);
            Destroy(playerTransform.gameObject);
            playerTransform = null;
        }

        // ✅ [9] 가로줄 생성
        int min = verticalCount - 1;
        int max = verticalCount + 3;
        int horizontalLineCount = Random.Range(min, max + 1);
        ladderGenerator.SetupHorizontalLines(verticalCount, stepCount, horizontalLineCount, true);

        // ✅ [10] 플레이어 프리팹 생성 및 배치
        if (playerPrefab == null) return;
        GameObject playerGO = Instantiate(playerPrefab, ladderRoot);
        playerTransform = playerGO.transform;

        RectTransform verticalLine = GetVerticalLineAt(startIndex);
        float x = verticalLine.anchoredPosition.x;
        float y = verticalLine.anchoredPosition.y + verticalLine.sizeDelta.y / 2f;

        RectTransform rect = playerTransform.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = new Vector2(x, y);

        // ✅ [11] 이동 시작
        playerMover.Setup(playerTransform, startIndex, 500f);
        playerMover.SetFinishCallback(CheckResult);
        playerMover.StartMove(this);

        // ✅ [12] 기대 보상 숨김
        if (rewardText != null)
            rewardText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 보드 텍스트를 명확하게 출력하며 항상 활성화 상태로 보장함
    /// </summary>
    private void SetBoardMessage(string message)
    {
        if (!boardText.enabled)
            boardText.enabled = true;

        if (boardText != null)
        {
            boardText.gameObject.SetActive(true);    // 오브젝트 활성화 보장
            boardText.enabled = true;                 // ✅ 텍스트 렌더링 강제 활성화
            boardText.text = message;                 // 텍스트 설정
            boardText.ForceMeshUpdate();              // TMP 강제 리프레시
        }
    }

    /// <summary>
    /// 플레이어 도착 후 결과 판단 및 보상 지급 처리
    /// </summary>
    private void CheckResult(int arrivedIndex)
    {
        int goalIndex = generator.GetSelectedDestination();
        float betAmount = betAmountUIManager != null ? betAmountUIManager.GetBetAmount() : 0f;

        // 배율 계산 (float 기준 유지)
        float goalMultiplier = verticalCount * goalMultiplierFactor;
        float startMultiplier = verticalCount * verticalCount * startMultiplierFactor;
        bool hasSelectedStart = selectedStartIndex >= 0;
        float finalMultiplier = hasSelectedStart ? startMultiplier : goalMultiplier;

        // 보상 계산
        float reward = betAmount * finalMultiplier;
        bool isSuccess = arrivedIndex == goalIndex;

        if (resultUIManager != null)
        {
            if (!isSuccess)
            {
                reward = 0f;
            }

            string message = isSuccess
                ? $"YOU DID IT! Claim your {reward:F1} Coins"
                : "OH NO! Better luck next time!";
            resultUIManager.ShowResult(message);

            // 코인 증감
            if (isSuccess)
                AddCoin(reward);
            else
                AddCoin(-betAmount);
        }

        // 버튼 상태 반영
        bool resultVisible = resultUIManager != null && resultUIManager.IsResultVisible();
        SetResultButtonState("READY", !resultVisible);

        if (betAmountUIManager != null)
            betAmountUIManager.SetInteractable(true);

        if (rewardText != null)
            rewardText.gameObject.SetActive(false);
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
        // ⛔ 배팅 금액이 0원인 경우 골 선택 차단
        if (betAmountUIManager.GetBetAmount() <= 0)
        {
            if (boardText != null)
                boardText.text = "INPUT YOUR BET AMOUNT FIRST!";
            return;
        }

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
            // 골 선택 시 안내 메시지는 숨기고 리워드만 표시
            if (boardText != null)
                boardText.gameObject.SetActive(false); // ⬅ 안내 텍스트 숨김

            if (rewardText != null)
                rewardText.gameObject.SetActive(true); // ⬅ 기대 보상 출력

            float betAmount = betAmountUIManager.GetBetAmount();
            float goalMultiplier = verticalCount * goalMultiplierFactor;
            float expectedReward = betAmount * goalMultiplier;

            rewardText.text = $"Expected: {expectedReward:F1} Coins";
            rewardText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 결과 버튼 클릭 시 실행되는 함수
    /// - 상태에 따라 사다리 생성 또는 결과 실행으로 분기
    /// - 버튼 텍스트에 따라 "READY" 또는 "GO" 상태를 판단함
    /// </summary>
    public void OnResultButtonPressed()
    {
        // ✅ 1. 결과창이 열려 있으면 아무 것도 실행하지 않음 (중복 방지)
        if (resultUIManager != null && resultUIManager.IsResultVisible())
        {
            Debug.LogWarning("⛔ 결과창이 열려 있는 동안 결과 버튼은 비활성화되어야 합니다.");
            return;
        }

        // ✅ 2. 버튼 레이블(TextMeshProUGUI)을 가져옴
        var labelComponent = resultButton.GetComponentInChildren<TextMeshProUGUI>();
        if (labelComponent == null)
        {
            Debug.LogError("❌ resultButton에 TextMeshProUGUI가 연결되어 있지 않습니다.");
            return;
        }

        string label = labelComponent.text;

        // ───────────────────────────────────────────────
        // 🎯 READY 상태 → 사다리 생성 및 메시지 출력
        // ───────────────────────────────────────────────
        if (label == "READY")
        {
            GenerateLadder();

            float bet = betAmountUIManager != null ? betAmountUIManager.GetBetAmount() : 0f;

            if (bet <= 0f || currentCoin <= 0f)
            {
                SetBoardMessage("INPUT YOUR BET AMOUNT");
                SetResultButtonState("READY", false);
                SetGoalButtonsInteractable(false);
                return;
            }

            SetResultButtonState("GO", true);
            SetBoardMessage("CHOOSE YOUR DESTINATION!");

            // ✅ 골 버튼 인터랙션 설정 (배팅이 있을 때만 선택 가능)
            SetGoalButtonsInteractable(bet > 0f);

            // ✅ 결과 버튼의 인터랙션 가능 여부 결정
            SetResultButtonState("GO", bet > 0f);

            isLadderGenerated = true;

            StartCoroutine(ReenableGoalButtons());
        }

        // ───────────────────────────────────────────────
        // 🏁 GO 상태 → 결과 실행 시작
        // ───────────────────────────────────────────────
        else if (label == "GO")
        {
            // ❌ 골 버튼이 선택되지 않은 경우 → 안내 메시지 출력 후 중단
            if (selectedGoalButton == null)
            {
                if (boardText != null)
                    boardText.text = "CHOOSE YOUR DESTINATION!";
                return;
            }

            // 🔕 보상 기대값 텍스트 숨김 처리
            if (rewardText != null)
                rewardText.gameObject.SetActive(false);

            // ✅ 결과 실행 함수 호출
            OnResultButtonClicked();

            // 🕐 버튼 상태를 WAIT으로 변경
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

        // ✅ 클릭 이벤트가 꼬이는 문제 해결을 위한 강제 정렬 보정
        Canvas.ForceUpdateCanvases(); // ✅ 레이아웃 강제 갱신
        //destinationButtonsParent.GetComponent<VerticalLayoutGroup>()?.SetLayoutVertical(); // 만약 사용 중일 경우

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
        // 🔚 생성 완료 후 강제 Layout 갱신
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(destinationButtonsParent.GetComponent<RectTransform>());
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
        if (betAmountUIManager == null)
            return;

        float bet = betAmountUIManager.GetBetAmount();

        bool hasValidBet = (bet > 0f && currentCoin >= bet);

        if (boardText != null)
        {
            boardText.gameObject.SetActive(true);
            boardText.enabled = true;
            boardText.text = hasValidBet ? "PRESS READY BUTTON" : "INPUT YOUR BET AMOUNT";
        }

        SetResultButtonState("READY", hasValidBet);

        // 🔧 슬라이더와 버튼은 유효한 배팅이 있을 때만 활성화
        betAmountUIManager.SetInteractable(hasValidBet);

        // ✅ 퀵 버튼 상태 갱신 (보유 금액이 변경됐을 수 있음)
        betAmountUIManager.UpdateQuickBetButtons(currentCoin);
    }



    private void CorrectHorizontalLineCount()
    {
        int min = verticalCount - 1;
        int max = verticalCount + 3;
        horizontalLineCount = Mathf.Clamp(horizontalLineCount, min, max);
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
                float bet = betAmountUIManager.GetBetAmount();
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
            float bet = betAmountUIManager.GetBetAmount();
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
    private void OnBetConfirmedHandler(float betAmount)
    {
        float coin = currentCoin;
        
        if (boardText != null)
        {
            boardText.gameObject.SetActive(true);
            boardText.enabled = true;

            if (betAmount <= 0f || betAmount > coin)
                boardText.text = "INPUT YOUR BET AMOUNT";
            else
                boardText.text = "PRESS READY BUTTON";
        }

        SetResultButtonState(
        state: (betAmount > 0f && betAmount <= coin) ? "READY" : "DISABLED",
        isInteractable: (betAmount > 0f && betAmount <= coin)
    );


        // ✅ 배팅 UI는 비활성화
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

    public void SetCoin(float amount)
    {
        currentCoin = Mathf.Max(0f, amount);
        UpdateCoinUI();

        // 코인 UI 변경 후 퀵배팅 버튼 업데이트
        if (betAmountUIManager != null)
        {
            betAmountUIManager.UpdateQuickBetButtons(currentCoin);

            // 🟡 배팅 금액이 보유 금액보다 클 경우 초기화
            if (betAmountUIManager.GetBetAmount() > currentCoin)
                betAmountUIManager.SetBetAmount(0f);
        }
    }

    public void AddCoin(float amount)
    {
        currentCoin = Mathf.Max(0f, currentCoin + amount);
        UpdateCoinUI();

        if (betAmountUIManager != null)
        {
            betAmountUIManager.UpdateQuickBetButtons(currentCoin);

            if (betAmountUIManager.GetBetAmount() > currentCoin)
                betAmountUIManager.SetBetAmount(0f);
        }
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

    private IEnumerator HandlePostResultUIClosedRoutine()
    {
        yield return new WaitForEndOfFrame(); // UI가 완전히 닫힌 후 적용

        float coin = currentCoin;
        float bet = betAmountUIManager.GetBetAmount();

        if (boardText != null)
            boardText.text = "CHOOSE YOUR DESTINATION!";

        if (selectedGoalButton != null)
            SetResultButtonState("GO");
        else
            SetResultButtonState("DISABLED");

        // 🔒 잔고 부족하면 배팅 초기화
        if (coin <= 0f || coin < bet)
        {
            Debug.LogWarning("💸 배팅 초기화: 보유 코인이 부족합니다.");
            betAmountUIManager.SetBetAmount(0f);

            if (boardText != null)
                boardText.text = "NOT ENOUGH BALANCE.";
        }
        else
        {
            betAmountUIManager.SetBetAmount(bet); // 유효한 금액이면 그대로 유지
        }
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

    void DisableTMPTextRaycasts()
    {
        TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true); // 모든 자식 포함
        foreach (var tmp in allTexts)
        {
            if (tmp != null)
                tmp.raycastTarget = false;
        }
    }

    // LadderManager.cs 안에 추가
    private IEnumerator ReenableGoalButtons()
    {
        yield return null; // 한 프레임 대기

        foreach (var btn in destinationButtons)
        {
            var uiBtn = btn.GetComponent<Button>();
            if (uiBtn != null)
            {
                uiBtn.interactable = false;
                uiBtn.interactable = true; // ✅ 강제로 리셋
            }
        }
    }

}
