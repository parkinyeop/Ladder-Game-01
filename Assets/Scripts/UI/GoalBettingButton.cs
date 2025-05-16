using UnityEngine;
using UnityEngine.UI;
using TMPro; // ✅ TextMeshPro 사용을 위한 네임스페이스 추가

/// <summary>
/// GoalBettingButton
/// - 골 선택 버튼 UI 클래스
/// - 클릭 시 LadderManager에 선택 전달 및 시각적 하이라이트 처리
/// - 배당률 텍스트만 출력 (번호 없음)
/// </summary>
public class GoalBettingButton : MonoBehaviour
{
    public int destinationIndex; // 이 버튼이 나타내는 도착 지점 인덱스

    [Header("배당률 출력용 TMP 텍스트")]
    public TMP_Text multiplierText; // 🎯 2.7X, 3.0X 등 출력용 TextMeshPro 텍스트

    private Button button;
    private LadderManager ladderManager;

    // 버튼 색상 상태 정의
    private static readonly Color HighlightColor = new Color(0.2f, 0.8f, 0.4f); // 강조색
    private static readonly Color DimColor = new Color(0.7f, 0.7f, 0.7f);        // 비활성화 색
    private static readonly Color DefaultColor = Color.white;                  // 기본색

    private void Awake()
    {
        // ✅ 텍스트나 이미지가 Raycast를 막지 않도록 처리
        DisableChildTMPRaycasts();
    }

    private void Start()
    {
        // 버튼 컴포넌트 및 LadderManager 찾아 연결
        button = GetComponent<Button>();
        ladderManager = FindObjectOfType<LadderManager>();

        // 클릭 이벤트 연결
        if (button != null)
            button.onClick.AddListener(SelectDestination);

        // ✅ 디버그용 Raycast 타겟 검사
        var graphics = GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics)
        {
            if (!g.raycastTarget)
                Debug.LogWarning($"❌ {name} 하위 컴포넌트 '{g.name}' RaycastTarget = false");
        }
    }

    /// <summary>
    /// 이 골 버튼이 클릭되었을 때 LadderManager에 알림
    /// </summary>
    private void SelectDestination()
    {
        if (ladderManager == null)
            return;

        // 💥 배팅 금액이 0이면 클릭 차단 + 메시지 출력
        if (ladderManager.betAmountUIManager != null && ladderManager.betAmountUIManager.GetBetAmount() <= 0)
        {
            if (ladderManager.boardText != null)
                ladderManager.boardText.text = "INPUT YOUR BET AMOUNT FIRST!";
            return;
        }

        ladderManager.SetSelectedDestination(destinationIndex); // 선택 처리
        ladderManager.HighlightSelectedGoalButton(this);        // 시각적 하이라이트
    }

    /// <summary>
    /// 기본 노란색으로 하이라이트
    /// </summary>
    public void Highlight()
    {
        var image = GetComponentInChildren<Image>();
        var tmp = GetComponentInChildren<TextMeshProUGUI>();

        if (image != null) image.raycastTarget = true;
        if (tmp != null) tmp.raycastTarget = true;

        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = Color.yellow;
    }

    /// <summary>
    /// 특정 색상으로 하이라이트 적용
    /// </summary>
    public void HighlightWithColor(Color color)
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = color;
    }

    /// <summary>
    /// 버튼을 흐리게 표시 (선택되지 않았을 때)
    /// </summary>
    public void Dim()
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = DimColor;
    }

    /// <summary>
    /// 버튼 색상과 텍스트 표시 복원
    /// </summary>
    public void ResetColor()
    {
        if (button?.targetGraphic != null)
        {
            button.targetGraphic.color = DefaultColor;
        }

        SetTextVisible(true);
    }

    /// <summary>
    /// 배율 텍스트 설정 (예: 2.7X)
    /// </summary>
    public void SetMultiplierText(float multiplier)
    {
        if (multiplierText != null)
            multiplierText.text = $"{multiplier:F1}X"; // 소수점 1자리
    }

    /// <summary>
    /// 텍스트 표시 여부 설정
    /// </summary>
    public void SetTextVisible(bool isVisible)
    {
        if (multiplierText != null)
            multiplierText.enabled = isVisible;
    }

    private void DisableChildTMPRaycasts()
    {
        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>())
        {
            tmp.raycastTarget = false; // 🔒 Raycast 방지
        }

        foreach (var img in GetComponentsInChildren<Image>())
        {
            if (img.gameObject != this.gameObject)
                img.raycastTarget = false;
        }
    }
}
