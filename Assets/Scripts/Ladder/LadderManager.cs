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

    [Header("세로/가로줄 수 조절 UI")]
    public Button increaseVerticalButton;
    public Button decreaseVerticalButton;
    public Button increaseHorizontalButton;
    public Button decreaseHorizontalButton;
    public Toggle randomizeToggle;
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

        if (generateButton == null) Debug.LogError("🚨 generateButton 연결 오류");
        if (resultButton == null) Debug.LogError("🚨 resultButton 연결 오류");
        if (ladderRoot == null) Debug.LogError("🚨 ladderRoot 연결 오류");
        if (startButtonPrefab == null || startButtonsParent == null) Debug.LogError("🚨 Start 버튼 관련 프리팹 누락");
        if (destinationButtonPrefab == null || destinationButtonsParent == null) Debug.LogError("🚨 Destination 버튼 프리팹 누락");

        if (resultUIManager != null) resultUIManager.Hide();

        if (betAmountUIManager != null)
            betAmountUIManager.OnBetConfirmed += OnBetConfirmedHandler;
        else
            Debug.LogError("🚨 BetAmountUIManager 연결 안됨");

        // 혹시 인스펙터에 연결 안 했으면 자동 연결 시도
        if (resultButtonLabel == null)
            resultButtonLabel = resultButton.GetComponentInChildren<TextMeshProUGUI>();

        SetupUI();
        generateButton?.onClick.AddListener(GenerateLadder);
        resultButton?.onClick.AddListener(OnResultButtonPressed);

        UpdateVerticalCountText();
        UpdateHorizontalLineCountText();

        if (resultButton != null)
        {
            TMP_Text txt = resultButton.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = "READY";
            resultButton.interactable = false;
        }

        if (ladderGenerator == null)
            ladderGenerator = new LadderGenerator(this);

        UpdateCoinUI();
        ladderGenerator.Initialize(this);
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
    /// 사다리 생성 시작 함수 (READY 상태에서 실행됨)
    /// - 세로줄만 생성하고, 버튼 및 보드 UI 초기화
    /// - 가로줄은 GO 버튼 클릭 시 생성됨
    /// </summary>
    public void GenerateLadder()
    {
        // ✅ 세로줄 생성만 먼저 수행 (가로줄은 나중에)
        ladderGenerator.GenerateVerticalLines(verticalCount, stepCount);

        // ✅ 이전 라운드에서 선택되었던 골 버튼 초기화
        ResetAllGoalButtonColors();

        // ✅ 결과 버튼은 초기화 시점에서 비활성화
        resultButton.interactable = false;

        // ✅ 도착 버튼 배치 (사다리 위치 기준으로)
        InitializeDestinationButtons(verticalCount);

        // ✅ 출발 버튼 배치
        InitializeStartButtons(verticalCount);

        // ✅ 보드 UI 활성화 및 안내 메시지 출력
        if (board != null) board.SetActive(true);
        if (boardText != null) boardText.text = "PINPOINT YOUR JOURNEY'S END!!";

        // ✅ 배팅 금액이 설정되지 않은 경우 결과 버튼 비활성화
        int currentBet = betAmountUIManager != null ? betAmountUIManager.GetBetAmount() : 0;
        resultButton.interactable = (currentBet > 0);

        // ✅ 결과 버튼 텍스트 초기화 ("READY")
        resultButton.GetComponentInChildren<TextMeshProUGUI>().text = "READY";

        // ✅ 스타트 버튼은 아직 선택할 수 없도록 비활성화
        SetStartButtonsInteractable(false);

        // ✅ 보상 텍스트 숨기기
        if (rewardText != null)
            rewardText.gameObject.SetActive(false);

        // ✅ 사다리 생성 이후 배팅 UI는 비활성화 (GO까지 진행)
        if (betAmountUIManager != null)
            betAmountUIManager.SetInteractable(false);

        // ✅ 결과 UI 패널 숨기기 (새 라운드 시작)
        if (resultUIManager != null)
            resultUIManager.Hide();
    }

    /// <summary>
    /// 결과 버튼(GO) 클릭 시 실행
    /// - 가로줄 생성 (보장된 방식)
    /// - 플레이어 생성 및 이동 시작
    /// </summary>
    public void OnResultButtonClicked()
    {
        // 🔒 이미 이동 중이면 중복 실행 방지
        if (playerMover.IsMoving())
            return;

        // ❗ 도착 버튼을 선택하지 않은 경우 경고 출력
        if (selectedGoalButton == null)
        {
            if (boardText != null)
                boardText.text = "PINPOINT YOUR JOURNEY'S END!";
            return;
        }

        // 📕 보드 UI 비활성화
        if (board != null)
            board.SetActive(false);

        // ⭐ 시작 위치 인덱스 결정 (선택 안 됐으면 랜덤)
        int startIndex = selectedStartIndex >= 0
            ? selectedStartIndex
            : Random.Range(0, verticalCount);

        // 🧹 모든 스타트 버튼 텍스트 및 색상 초기화
        foreach (var btn in startButtons)
        {
            Text label = btn.GetComponentInChildren<Text>();
            if (label != null) label.text = "";
        }
        ResetAllStartButtonColors();

        // 🟡 랜덤 선택일 경우 노란색 하이라이트
        if (selectedStartIndex < 0 && startIndex >= 0 && startIndex < startButtons.Count)
        {
            selectedStartButton = startButtons[startIndex];
            selectedStartButton.HighlightWithColor(Color.yellow);
        }

        // 🗑 기존 플레이어 제거
        if (playerTransform != null)
        {
            playerMover.StopMove(this);
            Destroy(playerTransform.gameObject);
            playerTransform = null;
        }

        // ✅ 무작위 보장형 방식으로 가로줄 생성
        int min = verticalCount - 1;
        int max = verticalCount + 3;
        int horizontalLineCount = Random.Range(min, max + 1);

        ladderGenerator.SetupHorizontalLines(verticalCount, stepCount, horizontalLineCount, true);

        // 🎮 플레이어 프리팹 유효성 검사
        if (playerPrefab == null) return;

        // 🎮 플레이어 생성 및 배치
        GameObject playerGO = Instantiate(playerPrefab, ladderRoot);
        playerTransform = playerGO.transform;

        // 정확한 위치 계산: 선택된 세로줄 최상단 (Visual 기준)
        RectTransform verticalLine = GetVerticalLineAt(startIndex);
        float x = verticalLine.anchoredPosition.x;
        float y = verticalLine.anchoredPosition.y + verticalLine.sizeDelta.y / 2f;

        RectTransform rect = playerTransform.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = new Vector2(x, y);

        // ▶ 이동 시작 설정
        playerMover.Setup(playerTransform, startIndex, 500f);
        playerMover.SetFinishCallback(CheckResult);
        playerMover.StartMove(this);

        // 📴 결과 버튼 비활성화 (중복 클릭 방지)
        resultButton.interactable = false;

        // 🧊 기대값 텍스트 숨김
        if (rewardText != null)
            rewardText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 플레이어 도착 후 성공 여부 판단 및 보상 처리
    /// </summary>
    private void CheckResult(int arrivedIndex)
    {
        // ✅ 도착 목표 인덱스 확인 (골버튼 선택된 인덱스)
        int goalIndex = generator.GetSelectedDestination();

        // ✅ 배팅 금액 가져오기
        float betAmount = betAmountUIManager != null ? betAmountUIManager.GetBetAmount() : 0f;

        // ✅ 배율 계산 (세로줄 개수 × 설정된 배율 팩터)
        float goalMultiplier = verticalCount * goalMultiplierFactor;                   // 예: 3줄 × 0.9 = 2.7
        float startMultiplier = verticalCount * verticalCount * startMultiplierFactor; // 예: 3줄 × 3줄 × 0.9 = 8.1

        // ✅ 사용자가 스타트 버튼을 선택했는지 여부 판단
        bool hasSelectedStart = selectedStartIndex >= 0;

        // ✅ 최종 배율 결정: 스타트 버튼 선택 여부에 따라
        float finalMultiplier = hasSelectedStart ? startMultiplier : goalMultiplier;

        // ✅ 최종 보상 계산
        float reward = betAmount * finalMultiplier;
        int roundedReward = Mathf.FloorToInt(reward); // 정수로 처리

        // ✅ 성공 여부 판단 (도착 인덱스와 골 인덱스 일치 여부)
        bool isSuccess = arrivedIndex == goalIndex;

        // ✅ 새 방식: ResultUIManager 통해 결과 패널 표시
        if (resultUIManager != null)
        {
            if (!isSuccess)
                reward = 0f; // ⛔ 실패 시 보상은 0

            string message = isSuccess ? $"🎉 YOU DID IT! Claim your {reward} Coins" : $"❌ OH NO! Better luck next time!";
            resultUIManager.ShowResult(message); // ✅ 결과창 호출

            // ✅ 보유 코인 업데이트
            if (isSuccess)
            {
                AddCoin(roundedReward); // 보상 지급
            }
            else
            {
                AddCoin(-Mathf.FloorToInt(betAmount)); // 배팅 금액만큼 차감
            }
        }

        // ✅ 결과 버튼 다시 활성화 및 텍스트 복구
        resultButton.interactable = true;
        resultButton.GetComponentInChildren<TextMeshProUGUI>().text = "READY";

        // ✅ 다음 라운드를 위해 배팅 UI 다시 활성화
        if (betAmountUIManager != null)
        {
            betAmountUIManager.SetInteractable(true);
        }

        // ✅ 기대값 텍스트는 비활성화
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

            rewardText.text = $"Expected: {expectedReward:F1} Coins";
            rewardText.gameObject.SetActive(true);
        }
    }

    public void OnResultButtonPressed()
    {
        // TMP 컴포넌트 가져오기
        var labelComponent = resultButton.GetComponentInChildren<TextMeshProUGUI>();
        if (labelComponent == null)
        {
            Debug.LogError("❌ resultButton에 TextMeshProUGUI가 연결되어 있지 않습니다.");
            return;
        }

        string label = labelComponent.text;

        if (label == "READY")
        {
            GenerateLadder(); // 사다리 및 UI 생성
            labelComponent.text = "GO"; // ✅ 상태 전환 (TMP 기준)
            isLadderGenerated = true;
        }
        else if (label == "GO")
        {
            if (selectedGoalButton == null)
            {
                if (boardText != null) boardText.text = "CHOOSE YOUR DESTINATION!";
                return;
            }

            // 결과 실행 전 기대값 텍스트 숨기기
            if (rewardText != null)
                rewardText.gameObject.SetActive(false);

            // 결과 실행
            OnResultButtonClicked();

            // 상태 변화 처리
            labelComponent.text = "WAIT";
            labelComponent.text = "READY";
            isLadderGenerated = false;
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
            verticalCountText.text = $"Vewrtical Lines Count: {verticalCount}";
    }

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

        float buttonY = 300f;

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
        Debug.Log($"💰 배팅 확정: {betAmount} 코인");

        if (betAmount <= 0)
        {
            resultButton.interactable = false;
            if (boardText != null) boardText.text = "SET YOUR STAKES!";
            return;
        }

        resultButton.interactable = true;
        var txt = resultButton.GetComponentInChildren<TMP_Text>();
        if (txt != null) txt.text = "READY";
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

    public void SetResultButtonState(string state)
    {
        if (resultButton != null)
        {
            // ✅ Text → TextMeshProUGUI로 교체
            TextMeshProUGUI label = resultButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = state;

            // 상태에 따라 버튼 비활성화 처리
            resultButton.interactable = (state == "READY" || state == "GO");
        }
    }

    /// <summary>
    /// 보유 코인을 설정하고 UI 갱신
    /// </summary>
    public void SetCoin(float amount)
    {
        currentCoin = Mathf.Max(0, amount); // 음수 방지
        UpdateCoinUI();
    }

    /// <summary>
    /// 보유 코인을 증가 또는 감소시키고 UI 갱신
    /// </summary>
    public void AddCoin(float amount)
    {
        currentCoin = Mathf.Max(0, currentCoin + amount); // 음수 방지
        UpdateCoinUI();
    }

    /// <summary>
    /// 보유 코인 텍스트 UI 업데이트
    /// </summary>
    private void UpdateCoinUI()
    {
        if (coinTextUI != null)
            coinTextUI.text = $"Coins: {currentCoin:F1}";
    }

    // ✅ 정확한 함수 정의 예시
    public List<RectTransform> GetVerticalLines()
    {
        return verticalLines;
    }
}
