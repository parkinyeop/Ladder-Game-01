using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GoalBettingButton
/// - 사다리 게임에서 도착 지점을 선택하는 골 버튼
/// - 버튼 클릭 시 LadderManager에 선택 결과를 알림
/// - 선택된 버튼은 초록색, 비선택 버튼은 회색 처리
/// </summary>
public class GoalBettingButton : MonoBehaviour
{
    [Header("설정")]
    public int destinationIndex; // 이 버튼이 담당하는 도착 지점 인덱스 (0부터 시작)

    private Button button;                // 버튼 자체 컴포넌트
    private LadderManager ladderManager;  // 사다리 매니저 참조

    private void Start()
    {
        // 1. Button 컴포넌트 가져오기
        button = GetComponent<Button>();

        // 2. LadderManager 찾기
        ladderManager = FindObjectOfType<LadderManager>();

        // 3. 버튼 정상 연결 시 클릭 이벤트 등록
        if (button != null)
        {
            button.onClick.AddListener(SelectDestination);
        }
        else
        {
            Debug.LogError($"[GoalBettingButton] Button 컴포넌트를 찾을 수 없습니다: {gameObject.name}");
        }

        // 4. LadderManager 연결 여부 체크
        if (ladderManager == null)
        {
            Debug.LogError("[GoalBettingButton] LadderManager를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 버튼 클릭 시 호출
    /// - 선택된 destinationIndex를 LadderManager에 전달
    /// </summary>
    private void SelectDestination()
    {
        if (ladderManager != null)
        {
            ladderManager.SetSelectedDestination(destinationIndex); // 도착지 선택 알림
            ladderManager.HighlightSelectedGoalButton(this);         // ⭐ 선택한 버튼 하이라이트 추가
            Debug.Log($"[GoalBettingButton] 선택된 도착 지점: {destinationIndex + 1}번");
        }
    }

    /// <summary>
    /// 버튼을 외부에서 다시 활성화할 때 호출
    /// </summary>
    public void EnableButton()
    {
        if (button != null)
        {
            button.interactable = true; // 버튼 다시 클릭 가능하게 설정
        }
    }

    /// <summary>
    /// 선택된 버튼을 초록색으로 하이라이트 처리
    /// </summary>
    public void Highlight()
    {
        if (button != null && button.targetGraphic != null)
        {
            button.targetGraphic.color = Color.green; // ⭐ Button의 targetGraphic 색을 초록색으로 변경
        }
    }

    /// <summary>
    /// 비선택된 버튼을 회색으로 처리
    /// </summary>
    public void Dim()
    {
        if (button != null && button.targetGraphic != null)
        {
            button.targetGraphic.color = Color.gray; // ⭐ targetGraphic 색을 회색으로 변경
        }
    }

    /// <summary>
    /// 버튼 색깔을 기본 흰색으로 초기화
    /// </summary>
    public void ResetColor()
    {
        if (button != null && button.targetGraphic != null)
        {
            button.targetGraphic.color = Color.white; // ⭐ targetGraphic 색을 흰색으로 복구
        }
    }
}