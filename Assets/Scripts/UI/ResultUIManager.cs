using UnityEngine;
using UnityEngine.UI;
using TMPro; // ✅ TextMeshPro 사용을 위해 추가

/// <summary>
/// ResultUIManager
/// - 성공/실패 결과를 팝업으로 보여주는 UI 매니저 (TextMeshPro 적용됨)
/// </summary>
public class ResultUIManager : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject resultPanel;                // 결과 표시용 패널 (비활성화 상태로 시작)
    public TextMeshProUGUI resultMessageText;     // ✅ 결과 메시지를 보여줄 TMP 텍스트
    public Button closeButton;                    // 닫기 버튼

    [SerializeField] private LadderManager ladderManager; // 사다리 매니저 참조 (다음 라운드 준비용)

    private void Start()
    {
        // ✅ 시작 시 결과 패널은 비활성화
        if (resultPanel != null)
            resultPanel.SetActive(false);

        // ✅ 닫기 버튼 클릭 시 Hide 함수 연결
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    /// <summary>
    /// 결과 메시지를 설정하고 패널을 표시함
    /// </summary>
    public void ShowResult(string message)
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultMessageText != null)
            resultMessageText.text = message; // ✅ TMP 텍스트에 메시지 표시

        // ✅ 결과 버튼 비활성화
        if (ladderManager != null && ladderManager.resultButton != null)
            ladderManager.resultButton.interactable = false;

    }

    /// <summary>
    /// 결과 패널 숨김 처리
    /// </summary>
    public void Hide()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    /// <summary>
    /// 결과 패널의 닫기 버튼 클릭 시 호출되는 함수
    /// - 결과 패널을 숨기고
    /// - 다음 라운드를 위해 사다리 및 UI를 초기화함
    /// </summary>
    public void OnCloseResult()
    {
        // ✅ 1. 결과 패널 숨기기
        if (resultPanel != null)
            resultPanel.SetActive(false);

        // ✅ 2. 사다리 매니저가 연결된 경우
        if (ladderManager != null)
        {
            Debug.Log("OnCloseResult(): GenerateLadder 호출됨");

            // ✅ 3. 새로운 라운드용 사다리 생성
            ladderManager.GenerateLadder();

            // ✅ 4. 배팅 UI 다시 활성화
            if (ladderManager.betAmountUIManager != null)
                ladderManager.betAmountUIManager.SetInteractable(true);

            // ✅ 5. 보드 텍스트 강제 활성화
            if (ladderManager.boardText != null)
                ladderManager.boardText.gameObject.SetActive(true);

            // ✅ 6. 결과 버튼 상태를 "READY"로 설정 + 비활성화 (베팅 아직 안 했을 수 있음)
            ladderManager.SetResultButtonState("READY", false);
        }
        else
        {
            Debug.LogError("LadderManager가 연결되지 않았습니다.");
        }
    }

    /// <summary>
    /// 현재 결과 패널이 활성화되어 있는지 반환
    /// </summary>
    public bool IsResultVisible()
    {
        return resultPanel != null && resultPanel.activeSelf;
    }
}