using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// LadderManager
/// - 세로줄, 가로줄, 골 버튼을 관리하는 최상위 매니저
/// - UI 조작을 통해 사다리를 생성 및 갱신
/// </summary>
public class LadderManager : MonoBehaviour
{
    [Header("사다리 설정")]
    public int verticalCount = 3;             // 현재 세로줄 수
    public int stepCount = 10;                // 사다리 층 수
    public int horizontalLineCount = 2;       // 현재 가로줄 수
    public bool randomizeHorizontalLines = true; // 가로줄 랜덤 생성 여부

    [Header("사다리 간격 설정")]
    public float verticalSpacing = 1.5f;      // 세로줄 간격
    public float stepHeight = 0.6f;            // 층 간 간격

    [Header("UI 연결")]
    public Text verticalCountText;            // 세로줄 개수 표시 텍스트
    public Button increaseVerticalButton;     // 세로줄 수 증가 버튼
    public Button decreaseVerticalButton;     // 세로줄 수 감소 버튼
    public Text horizontalLineCountText;      // 가로줄 개수 표시 텍스트
    public Button increaseHorizontalButton;   // 가로줄 수 증가 버튼
    public Button decreaseHorizontalButton;   // 가로줄 수 감소 버튼
    public Toggle randomizeToggle;             // 랜덤 생성 여부 토글
    public Button generateButton;              // 사다리 생성 버튼

    [Header("사다리 프리팹 연결")]
    public Transform ladderRoot;              // 사다리 오브젝트 부모
    public GameObject verticalLinePrefab;     // 세로줄 프리팹
    public GameObject horizontalLinePrefab;   // 가로줄 프리팹
    public Transform destinationButtonsParent;// 골 버튼 부모
    public GameObject destinationButtonPrefab;// 골 버튼 프리팹

    private LadderGenerator generator;        // 사다리 생성기
    private List<GameObject> verticalLines = new List<GameObject>(); // ⭐ 여기에 verticalLines 변수 선언 ⭐
    private List<GoalBettingButton> destinationButtons = new List<GoalBettingButton>();
    private GoalBettingButton selectedGoalButton = null;


    private void Start()
    {
        // 생성기 연결
        generator = new LadderGenerator(this);

        // UI 버튼 설정
        SetupUI();

        // 시작할 때 가로줄 수 교정
        CorrectHorizontalLineCount();

        UpdateVerticalCountText();
        UpdateHorizontalLineCountText();
    }

    /// <summary>
    /// 버튼 이벤트 연결
    /// </summary>
    private void SetupUI()
    {
        increaseVerticalButton?.onClick.AddListener(IncreaseVerticalCount);
        decreaseVerticalButton?.onClick.AddListener(DecreaseVerticalCount);
        increaseHorizontalButton?.onClick.AddListener(IncreaseHorizontalLineCount);
        decreaseHorizontalButton?.onClick.AddListener(DecreaseHorizontalLineCount);
        randomizeToggle?.onValueChanged.AddListener(OnRandomizeToggleChanged);
        generateButton?.onClick.AddListener(GenerateLadder);
    }

    /// <summary>
    /// 사다리 생성 버튼 클릭시 호출
    /// </summary>
    public void GenerateLadder()
    {
        CorrectHorizontalLineCount(); // 세로/가로 수 확인
        generator.GenerateLadder(verticalCount, stepCount, horizontalLineCount, randomizeHorizontalLines);
    }

    // ---------------- 세로줄 조작 ----------------

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

    private void UpdateVerticalCountText()
    {
        if (verticalCountText != null)
            verticalCountText.text = $"세로줄 개수: {verticalCount}";
    }

    /// <summary>
    /// LadderGenerator에서 생성된 세로줄 리스트를 받음
    /// </summary>
    public void SetVerticalLines(List<GameObject> lines)
    {
        verticalLines = lines;
        // 필요하다면 여기서 세로줄 길이 조정 등의 추가 처리
        foreach (GameObject line in verticalLines)
        {
            if (line.TryGetComponent<RectTransform>(out RectTransform rectTransform))
            {
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, stepCount * stepHeight);
                rectTransform.anchoredPosition = new Vector2(0f, (stepCount * stepHeight) / 2f);
            }
            else if (line.TryGetComponent<LineRenderer>(out LineRenderer lineRenderer))
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, new Vector3(0f, 0f, 0f));
                lineRenderer.SetPosition(1, new Vector3(0f, stepCount * stepHeight, 0f));
            }
            else
            {
                Debug.LogWarning("세로줄 프리팹에 RectTransform 또는 LineRenderer가 없어 길이 조절이 되지 않았습니다.");
            }
        }
    }

    // ---------------- 가로줄 조작 ----------------

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

    private void UpdateHorizontalLineCountText()
    {
        if (horizontalLineCountText != null)
            horizontalLineCountText.text = $"가로줄 개수: {horizontalLineCount}";
    }

    /// <summary>
    /// 세로줄 수에 맞춰 가로줄 수를 자동 조정
    /// </summary>
    private void CorrectHorizontalLineCount()
    {
        int min = verticalCount - 1;
        int max = verticalCount + 3;
        horizontalLineCount = Mathf.Clamp(horizontalLineCount, min, max);
    }

    /// <summary>
    /// 랜덤 토글 변경 시 호출
    /// </summary>
    private void OnRandomizeToggleChanged(bool isOn)
    {
        randomizeHorizontalLines = isOn;
    }

    /// <summary>
    /// 외부(GoalBettingButton)에서 선택한 도착지를 설정 요청
    /// </summary>
    public void SetSelectedDestination(int index)
    {
        generator.SetSelectedDestination(index);
    }

    /// <summary>
    /// 현재 사다리 상태에서 특정 위치에 가로줄이 있는지 확인
    /// PlayerMover 등 외부에서 호출 가능
    /// </summary>
    public bool HasHorizontalLine(int y, int x)
    {
        return generator.HasHorizontalLine(y, x);
    }

    /// <summary>
    /// 세로줄 하단에 골 버튼 생성 및 배치
    /// </summary>
    public void InitializeDestinationButtons(int verticalCount)
    {
        // 기존에 생성된 골 버튼들을 삭제하고 리스트를 초기화합니다.
        ClearDestinationButtons();

        // 골 버튼을 생성하는 데 필요한 프리팹이나 부모 오브젝트가 설정되지 않았다면 함수를 종료합니다.
        if (destinationButtonPrefab == null || destinationButtonsParent == null) return;

        // 세로줄 리스트가 null이거나, 현재 설정된 세로줄 개수와 리스트의 요소 수가 다르다면 에러 로그를 출력하고 함수를 종료합니다.
        if (verticalLines == null || verticalLines.Count != verticalCount)
        {
            Debug.LogError("세로줄 리스트가 유효하지 않습니다.");
            return;
        }

        // 첫 번째 세로줄의 X축 시작 위치를 계산합니다. 이는 모든 세로줄이 균등한 간격으로 배치되기 위함입니다.
        float startX = -((verticalCount - 1) * verticalSpacing) / 2f;

        // 원하는 골 버튼의 고정된 Y축 위치를 설정합니다. (anchoredPosition 기준)
        float buttonYOverride = -300f;

        // 설정된 세로줄 개수만큼 반복하여 각 세로줄 아래에 골 버튼을 생성합니다.
        for (int i = 0; i < verticalCount; i++)
        {
            // 현재 인덱스(i)를 기반으로 골 버튼의 X축 위치를 계산합니다.
            // (i - (verticalCount - 1f) / 2f)는 가운데 세로줄을 기준으로 각 버튼의 상대적인 X축 위치를 -1, 0, 1 등으로 만듭니다.
            // 여기에 horizontalButtonSpacing(현재 400f)를 곱하여 실제 X축 간격을 적용합니다.
            float buttonX = (i - (verticalCount - 1f) / 2f) * 400f;

            // 골 버튼 프리팹을 생성합니다. 초기 위치는 Vector3.zero로 설정하고, 부모를 destinationButtonsParent로 설정합니다.
            GameObject buttonGO = Instantiate(destinationButtonPrefab, Vector3.zero, Quaternion.identity, destinationButtonsParent);

            // 생성된 골 버튼 오브젝트에서 RectTransform 컴포넌트를 가져옵니다. UI 요소의 위치 및 레이아웃을 제어하는 데 사용됩니다.
            RectTransform buttonRectTransform = buttonGO.GetComponent<RectTransform>();

            // RectTransform 컴포넌트가 제대로 가져왔는지 확인합니다.
            if (buttonRectTransform != null)
            {
                // 가져온 RectTransform의 anchoredPosition 속성을 사용하여 골 버튼의 최종 위치를 설정합니다.
                // anchoredPosition은 부모 RectTransform의 앵커를 기준으로 한 로컬 위치입니다.
                buttonRectTransform.anchoredPosition = new Vector2(buttonX, buttonYOverride);
            }
            else
            {
                // 골 버튼 프리팹에 RectTransform 컴포넌트가 없다면 에러 로그를 출력하고 생성된 오브젝트를 삭제한 후 다음 반복으로 넘어갑니다.
                Debug.LogError("골 버튼 프리팹에 RectTransform 컴포넌트가 없습니다.");
                Destroy(buttonGO);
                continue; // 다음 골 버튼 생성을 위해 반복문의 다음 순서로 진행합니다.
            }

            // 생성된 골 버튼 오브젝트에서 GoalBettingButton 스크립트를 가져옵니다.
            GoalBettingButton buttonScript = buttonGO.GetComponent<GoalBettingButton>();

            // GoalBettingButton 스크립트가 제대로 가져왔는지 확인합니다.
            if (buttonScript != null)
            {
                // 가져온 GoalBettingButton 스크립트의 destinationIndex 변수를 현재 인덱스(i)로 설정합니다.
                // 이는 각 버튼이 어떤 도착 지점을 담당하는지 알려주는 역할을 합니다.
                buttonScript.destinationIndex = i;
                // 생성된 GoalBettingButton 스크립트를 리스트에 추가하여 관리합니다.
                destinationButtons.Add(buttonScript);
            }
            else
            {
                // 골 버튼 프리팹에 GoalBettingButton 스크립트가 없다면 에러 로그를 출력하고 생성된 오브젝트를 삭제합니다.
                Debug.LogError("골 버튼 프리에 GoalBettingButton 스크립트가 없습니다.");
                Destroy(buttonGO);
            }

            // 생성된 골 버튼 오브젝트의 자식에 Text 컴포넌트가 있는지 확인합니다.
            Text buttonText = buttonGO.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                // Text 컴포넌트가 있다면 텍스트 내용을 현재 인덱스 + 1로 설정하여 버튼에 번호를 표시합니다.
                buttonText.text = (i + 1).ToString();
            }
        }
    }

    /// <summary>
    /// 생성된 골 버튼 삭제
    /// </summary>
    private void ClearDestinationButtons()
    {
        if (destinationButtonsParent != null)
        {
            foreach (Transform child in destinationButtonsParent)
            {
                Destroy(child.gameObject);
            }
        }
        destinationButtons.Clear();
        selectedGoalButton = null; // 선택된 골 버튼 참조 해제
    }

    /// <summary>
    /// 선택된 골 버튼을 초록색으로 하이라이트하고, 이전에 선택된 버튼은 원래 색으로 되돌립니다.
    /// </summary>
    /// <param name="selectedButton">새롭게 선택된 GoalBettingButton 스크립트</param>
    public void HighlightSelectedGoalButton(GoalBettingButton selectedButton)
    {
        // 이전에 선택된 버튼이 있다면 원래 색으로 되돌림
        if (selectedGoalButton != null)
        {
            selectedGoalButton.ResetColor();
        }

        // 새로 선택된 버튼 하이라이트
        if (selectedButton != null)
        {
            selectedButton.Highlight();
        }

        // 현재 선택된 버튼 업데이트
        selectedGoalButton = selectedButton;

        // 다른 버튼들 Dim 처리 (선택 사항)
        DimOtherGoalButtons(selectedButton);
    }

    /// <summary>
    /// 선택되지 않은 다른 골 버튼들을 회색으로 Dim 처리합니다. (선택 사항)
    /// </summary>
    /// <param name="selectedButton">현재 선택된 GoalBettingButton 스크립트</param>
    private void DimOtherGoalButtons(GoalBettingButton selectedButton)
    {
        foreach (GoalBettingButton button in destinationButtons)
        {
            if (button != null && button != selectedButton)
            {
                button.Dim();
            }
        }
    }

    /// <summary>
    /// 모든 골 버튼의 색상을 기본 색상으로 초기화합니다. (게임 재시작 등에 사용 가능)
    /// </summary>
    public void ResetAllGoalButtonColors()
    {
        foreach (GoalBettingButton button in destinationButtons)
        {
            if (button != null)
            {
                button.ResetColor();
            }
        }
        selectedGoalButton = null;
    }
}