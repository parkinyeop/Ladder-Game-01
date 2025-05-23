using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using static CoinManager;

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

    [Header("플레이어 관련")]
    public GameObject playerPrefab;             // 이동할 플레이어 프리팹
    public Transform playerTransform;           // 생성된 플레이어의 Transform 참조

    [Header("코인 관련")]
    public float currentCoin = 100; // 기본 보유 코인
    public TMP_Text coinTextUI;       // 인스펙터에서 연결할 텍스트 오브젝트

    [Header("출발 버튼 관련")]
    public GameObject startButtonPrefab;             // 출발 버튼 프리팹
    public Transform startButtonsParent;             // 출발 버튼들을 담을 부모 오브젝트

    public LoadingUIManager loadingUIManager;

    [SerializeField] private TextMeshProUGUI resultButtonLabel;

    public BetAmountUIManager betAmountUIManager;
    public string currentJwtToken;
    public List<StartBettingButton> startButtons = new List<StartBettingButton>(); // 생성된 출발 버튼 목록
    public float ladderWidth = 800f;

    private StartBettingButton selectedStartButton = null;          // 현재 선택된 출발 버튼
    private int selectedStartIndex = -1;                            // 선택된 출발 세로줄 

    [Header("🔗 연결된 스크립트")]
    [SerializeField] private LadderGenerator generator;
    //private LadderGenerator generator;          // 사다리 생성기

    private List<Button> betButtons = new(); // 모든 배팅 버튼을 리스트로 관리

    private GoalBettingButton selectedGoalButton = null;                // 선택된 골 버튼 참조
    private List<GoalBettingButton> destinationButtons = new();        // 모든 골 버튼 리스트
    private List<RectTransform> verticalLines = new();
    private CoinManager coinManager; // 서버 연동용 매니저
    private bool isLadderGenerated = false;  // READY 상태 → GO 상태 전환 여부

    RewardSender rewardSender;

    private void Awake()
    {
        // 🎯 씬 내에서 CoinManager 자동 연결
        coinManager = FindObjectOfType<CoinManager>();

        if (coinManager == null)
        {
            Debug.LogWarning("⚠️ CoinManager가 씬에 존재하지 않습니다. 서버 연동이 불가능합니다.");
        }

        if (generator == null)
        {
            //generator = FindObjectOfType<LadderGenerator>();
            if (generator == null)
                Debug.LogError("❌ Awake에서 generator 찾기 실패");
            else
                Debug.Log("✅ generator 자동 연결 성공");
        }
    }
    private void Start()
    {
        // 게임 시작 시 또는 사다리 리셋 시 호출
        loadingUIManager.StartLoading();
        if (loadingUIManager != null)
        {
            loadingUIManager.StartLoading();
        }
        else
        {
            Debug.LogError("❌ loadingUIManager가 할당되지 않았습니다. 인스펙터에서 확인하세요.");
        }

        //generator = new LadderGenerator(this);
        playerMover = new PlayerMover(this);

        rewardSender = GameObject.Find("RewardManager").GetComponent<RewardSender>();

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
        //if (ladderGenerator == null)
        //    ladderGenerator = new LadderGenerator(this);
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

    void Update()
    {
        // 마우스 왼쪽 버튼 클릭 시 Raycast 검사
        if (Input.GetMouseButtonDown(0))
        {
            DebugRaycastAt(Input.mousePosition);
        }
    }

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

        // LadderManager.cs (GenerateLadder 끝부분에 추가)
        //if (goalRaycastTester != null)
        //    goalRaycastTester.RunTestAfterGeneration();
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

        // ✅ [1] 잔고 부족 또는 배팅 미설정
        if (coin < bet || bet <= 0f)
        {
            SetBoardMessage("NOT ENOUGH BALANCE");
            SetResultButtonState("DISABLED", false);
            if (rewardText != null) rewardText.gameObject.SetActive(false);
            return;
        }

        // ✅ [2] 중복 클릭 방지
        if (playerMover.IsMoving()) return;

        // ✅ [3] 골 버튼 미선택
        if (selectedGoalButton == null)
        {
            SetBoardMessage("CHOOSE YOUR DESTINATION!");
            return;
        }

        // ✅ [4] 버튼 상태 및 보드 UI
        SetResultButtonState("WAIT", false);
        if (boardText != null) board.SetActive(false);

        // ✅ [5] 시작 위치 결정 (무작위 허용)
        int startIndex = selectedStartIndex >= 0 ? selectedStartIndex : Random.Range(0, verticalCount);

        // ✅ [6] 스타트 버튼 UI 초기화
        ClearStartButtonLabels();

        if (selectedStartIndex < 0 && IsValidStartIndex(startIndex))
        {
            selectedStartButton = startButtons[startIndex];
            selectedStartButton.HighlightWithColor(Color.yellow);
        }

        // ✅ [7] 이전 플레이어 제거
        RemovePreviousPlayer();

        // ✅ [8] 가로줄 생성 + ladderMap 초기화
        GenerateHorizontalLines();

        // ✅ [9] ladderMap null 체크 후 디버그
        bool[,] ladderMap = ladderGenerator.GetLadderMap();
        LadderDebugHelper.LogLadderMapReference(ladderMap, "OnResultButtonClicked");
        LadderDebugHelper.LogLadderMap(ladderMap, "OnResultButtonClicked");
        //Debug.Log($"🧪 ladderMap 상태: {(ladderMap == null ? "❌ null" : "✅ 생성됨")}");

        if (ladderMap == null)
        {
            Debug.LogError("❌ ladderMap이 생성되지 않아 이동을 중단합니다.");
            return;
        }

        // ✅ [10] 플레이어 생성 및 위치 설정
        if (!TryCreatePlayerAt(startIndex, out playerTransform))
        {
            Debug.LogError("❌ 플레이어 생성 실패");
            return;
        }

        // ✅ [11] 플레이어 이동 시작
        playerMover.Setup(playerTransform, startIndex, 500f);
        playerMover.SetFinishCallback(CheckResult);
        playerMover.StartMove(this);

        // ✅ [12] 기대 보상 텍스트 숨김
        if (rewardText != null)
            rewardText.gameObject.SetActive(false);

        Debug.Log($"🧩 generator (OnResultButtonClicked): {generator.GetHashCode()}");
    }

    // 스타트 버튼 텍스트 초기화
    private void ClearStartButtonLabels()
    {
        foreach (var btn in startButtons)
        {
            Text label = btn.GetComponentInChildren<Text>();
            if (label != null)
                label.text = "";
        }
    }

    // 스타트 인덱스 유효성 확인
    private bool IsValidStartIndex(int index)
    {
        return index >= 0 && index < startButtons.Count;
    }

    // 이전 플레이어 제거
    private void RemovePreviousPlayer()
    {
        if (playerTransform != null)
        {
            playerMover.StopMove(this);
            Destroy(playerTransform.gameObject);
            playerTransform = null;
        }
    }

    // 가로줄 및 ladderMap 생성
    private void GenerateHorizontalLines()
    {
        // ✅ 가로줄 개수 랜덤 설정
        int min = verticalCount - 1;
        int max = verticalCount + 3;
        int horizontalLineCount = Random.Range(min, max + 1);

        // ✅ 로그 출력
        Debug.Log($"🛠 SetupHorizontalLines 호출 → verticalCount={verticalCount}, stepCount={stepCount}, lines={horizontalLineCount}");

        // ✅ ladderMap 생성 (이 함수 안에서 ladderMap 생성됨)
        ladderGenerator.SetupHorizontalLines(verticalCount, stepCount, horizontalLineCount, true);
    }

    // 플레이어 생성 및 위치 설정
    private bool TryCreatePlayerAt(int startIndex, out Transform player)
    {
        player = null;

        if (playerPrefab == null)
        {
            Debug.LogError("🚨 playerPrefab이 설정되지 않았습니다.");
            return false;
        }

        GameObject playerGO = Instantiate(playerPrefab, ladderRoot);
        player = playerGO.transform;

        RectTransform verticalLine = GetVerticalLineAt(startIndex);
        if (verticalLine == null) return false;

        float x = verticalLine.anchoredPosition.x;
        float y = verticalLine.anchoredPosition.y + verticalLine.sizeDelta.y / 2f;

        RectTransform rect = player.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = new Vector2(x, y);

        return true;
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
    /// 플레이어가 도착한 인덱스(arrivedIndex)를 기반으로 결과를 확인하고
    /// 보상을 계산한 뒤 서버에 코인 증감 요청을 보낸다.
    /// </summary>
    private void CheckResult(int arrivedIndex)
    {
        // ✅ generator가 null이면 예외
        if (generator == null)
        {
            Debug.LogError("❌ generator가 연결되지 않았습니다.");
            return;
        }

        // ✅ ladderMap null 체크
        bool[,] ladderMap = generator.GetLadderMap();
        if (ladderMap == null || ladderMap.GetLength(0) == 0)
        {
            Debug.LogError("❌ ladderMap이 null입니다. 가로줄 생성 시점을 놓쳤거나, 초기화되지 않았습니다.");
            return;
        }

        int goalIndex = generator.GetSelectedDestination();
        float betAmount = betAmountUIManager != null ? betAmountUIManager.GetBetAmount() : 0f;

        // ✅ 도착 성공 여부 계산
        bool isSuccess = arrivedIndex == goalIndex;
        float multiplier = isSuccess ? verticalCount * verticalCount : 0;
        float rewardAmount = betAmount * multiplier;

        // ✅ UI 결과 패널 표시
        if (resultUIManager != null)
        {
            string resultMsg = isSuccess ? "You Gi=!" : "Opps !";
            string detailMsg = $"You reached {arrivedIndex}, goal was {goalIndex}\nReward: {rewardAmount:F1} Coins";
            resultUIManager.ShowResult(resultMsg + "\n" + detailMsg);
        }

        // ✅ 서버 보상 요청용 RewardSender 찾기
        RewardSender rewardSender = FindObjectOfType<RewardSender>();
        if (rewardSender == null)
        {
            Debug.LogError("❌ RewardSender를 찾지 못했습니다.");
            return;
        }

        // ✅ JWT 토큰
        string jwtToken = currentJwtToken;

        // ✅ 서버로 전송
        StartCoroutine(rewardSender.SendRewardRequest(
            token: jwtToken,
            betAmount: betAmount,
            verticalCount: verticalCount,
            startIndex: selectedStartIndex >= 0 ? selectedStartIndex : Random.Range(0, verticalCount),
            goalIndex: goalIndex,
            ladderMap: ladderMap
        ));

        // ✅ UI 처리
        SetResultButtonState("READY", true);
        if (betAmountUIManager != null)
            betAmountUIManager.SetInteractable(true);
        if (rewardText != null)
            rewardText.gameObject.SetActive(false);
        ResetAllStartButtonColors();
        selectedStartButton = null;
        selectedStartIndex = -1;
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
        Canvas.ForceUpdateCanvases();

        // ✅ 각 버튼에 대해 Raycast Target 보정
        foreach (var btn in destinationButtons)
        {
            var images = btn.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                img.raycastTarget = true; // 모든 이미지 Raycast 가능하도록
            }

            var tmps = btn.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                tmp.raycastTarget = false; // 텍스트는 비활성화
            }
        }

        // 버튼 생성 완료 후
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(destinationButtonsParent.GetComponent<RectTransform>());
        FixRaycastTargets(); // 🔧 레이캐스트 문제 해결용 보정
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

        // ✅ 서버에 현재 코인 잔액을 정확히 덮어쓰기
        StartCoroutine(SetBalanceOnServer(currentCoin));
    }

    // ✅ 서버에 현재 코인 잔액을 그대로 덮어쓰는 요청
    private IEnumerator SetBalanceOnServer(float balance)
    {
        string userId = "user001"; // 필요 시 외부에서 유저 ID 주입
        string url = "http://localhost:3000/coin";

        var requestData = new CoinSetRequest
        {
            user_id = userId,
            balance = balance
        };

        string json = JsonUtility.ToJson(requestData);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ 코인 설정 실패: {request.error}");
        }
        else
        {
            Debug.Log($"✅ 서버에 코인 잔액 {balance} 설정 성공");
        }
    }

    // 🔧 POST 요청용 구조체
    [System.Serializable]
    public class CoinSetRequest
    {
        public string user_id;
        public float balance;
    }

    public void AddCoin(float amount)
    {
        // 1. 코인 추가 또는 차감 (음수 가능)
        currentCoin = Mathf.Max(0f, currentCoin + amount);

        // 2. 코인 텍스트 UI 업데이트
        UpdateCoinUI();

        // 3. 배팅 UI 관련 상태 업데이트
        if (betAmountUIManager != null)
        {
            // 퀵 배팅 버튼 활성/비활성 조정
            betAmountUIManager.UpdateQuickBetButtons(currentCoin);

            // 현재 배팅 금액이 보유 코인을 초과하면 초기화
            if (betAmountUIManager.GetBetAmount() > currentCoin)
                betAmountUIManager.SetBetAmount(0f);
        }

        // 🎯 서버에 코인 잔액 업데이트 요청
        StartCoroutine(UpdateBalanceOnServer(amount));
    }

    private IEnumerator UpdateBalanceOnServer(float deltaAmount)
    {
        string userId = "user001"; // 유동적 처리 필요 시 외부에서 할당 가능
        string url = $"http://localhost:3000/coin/{userId}";

        // 서버에 보낼 데이터 포맷
        var body = new AmountRequest { amount = deltaAmount };
        string json = JsonUtility.ToJson(body);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ 서버에 코인 반영 실패: {request.error}");
        }
        else
        {
            Debug.Log($"✅ 서버에 코인 반영 성공: {deltaAmount} 적용됨");
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

    /// <summary>
    /// 목적지 골 버튼들의 raycastTarget 속성을 강제 활성화
    /// </summary>
    private void FixRaycastTargets()
    {
        foreach (var btn in destinationButtons)
        {
            var text = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.raycastTarget = false;
        }
    }

    private void DebugRaycastAt(Vector2 screenPos)
    {
        PointerEventData pointer = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        //Debug.Log($"🔎 Raycast Hit Count: {results.Count}");

        foreach (var result in results)
        {
            //Debug.Log($"🟢 Raycast Hit: {result.gameObject.name}");
        }
    }

    public void SetJwtToken(string token)
    {
        currentJwtToken = token;
    }
        
}
