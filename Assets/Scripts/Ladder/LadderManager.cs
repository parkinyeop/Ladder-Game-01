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

    [Header("플레이어 관련")]
    public GameObject playerPrefab;             // 이동할 플레이어 프리팹
    public Transform playerTransform;           // 생성된 플레이어의 Transform 참조

    private LadderGenerator generator;          // 사다리 생성기
    private PlayerMover playerMover;            // 플레이어 이동기
    private GameObject spawnedPlayer;           // 현재 생성된 플레이어 오브젝트

    private GoalBettingButton selectedGoalButton = null;                // 선택된 골 버튼 참조
    private List<GoalBettingButton> destinationButtons = new();        // 모든 골 버튼 리스트
    private List<GameObject> verticalLines = new();                    // 생성된 세로줄 오브젝트 저장

    private const float ladderWidth = 800f;      // 사다리 전체 너비 (위치 정렬 기준)

    private void Start()
    {
        generator = new LadderGenerator(this);
        playerMover = new PlayerMover(this);

        SetupUI();

        generateButton.onClick.AddListener(GenerateLadder);
        resultButton.onClick.AddListener(OnResultButtonClicked);

        UpdateVerticalCountText();
        UpdateHorizontalLineCountText();
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
        generator.GenerateLadder(verticalCount, stepCount, horizontalLineCount, randomizeHorizontalLines);
        ResetAllGoalButtonColors();
        resultText.text = "도착 지점을 선택하세요!";
        resultButton.interactable = true;
    }

    /// <summary>
    /// 결과 버튼 클릭 시 처리
    /// - 골 선택 여부 확인, 플레이어 생성 및 이동 실행
    /// </summary>
    public void OnResultButtonClicked()
    {
        if (playerMover.IsMoving()) return;

        if (selectedGoalButton == null)
        {
            resultText.text = "도착 지점을 선택하세요!";
            return;
        }

        if (playerTransform != null)
        {
            Destroy(playerTransform.gameObject);
            playerTransform = null;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[LadderManager] Player 프리팹이 연결되지 않았습니다.");
            return;
        }

        GameObject playerGO = Instantiate(playerPrefab, ladderRoot);
        playerTransform = playerGO.transform;

        int randomStartIndex = Random.Range(0, verticalCount);
        float x = LadderLayoutHelper.GetXPosition(randomStartIndex, ladderWidth, verticalCount);
        float y = LadderLayoutHelper.GetStartY(stepCount, stepHeight);

        RectTransform rect = playerTransform.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = new Vector2(x, y);

        playerMover.Setup(playerTransform, randomStartIndex, 500f);
        playerMover.SetFinishCallback(CheckResult);
        playerMover.StartMove(this);

        resultButton.interactable = false;
    }

    /// <summary>
    /// 플레이어 도착 후 성공 여부 확인
    /// </summary>
    private void CheckResult(int arrivedIndex)
    {
        int selectedIndex = generator.GetSelectedDestination();
        resultText.text = (arrivedIndex == selectedIndex) ? "🎉 성공!" : "❌ 실패!";
        resultButton.interactable = true;
    }

    /// <summary>
    /// 선택된 도착 인덱스 저장
    /// </summary>
    public void SetSelectedDestination(int index)
    {
        generator.SetSelectedDestination(index);
    }

    /// <summary>
    /// 버튼 하이라이트 처리 (선택 버튼 강조, 나머지 Dim 처리)
    /// </summary>
    public void HighlightSelectedGoalButton(GoalBettingButton selectedButton)
    {
        selectedGoalButton?.ResetColor();
        selectedButton?.Highlight();
        DimOtherGoalButtons(selectedButton);
        selectedGoalButton = selectedButton;
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
    /// </summary>
    public void InitializeDestinationButtons(int verticalCount)
    {
        foreach (Transform child in destinationButtonsParent)
            Destroy(child.gameObject);
        destinationButtons.Clear();

        for (int i = 0; i < verticalCount; i++)
        {
            GameObject buttonGO = Instantiate(destinationButtonPrefab, destinationButtonsParent);
            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            float x = LadderLayoutHelper.GetXPosition(i, ladderWidth, verticalCount);
            rect.anchoredPosition = new Vector2(x, -300f); // 아래 고정 위치

            GoalBettingButton btn = buttonGO.GetComponent<GoalBettingButton>();
            btn.destinationIndex = i;
            destinationButtons.Add(btn);

            Text txt = buttonGO.GetComponentInChildren<Text>();
            if (txt != null)
                txt.text = (i + 1).ToString();
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
}
