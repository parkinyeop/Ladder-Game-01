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
    /// 닫기 버튼 클릭 시 호출되는 함수
    /// - 사다리 UI를 초기화하고, 배팅 UI를 다시 활성화
    /// </summary>
    public void OnCloseResult()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        // ✅ 결과 버튼 다시 활성화
        if (ladderManager != null && ladderManager.resultButton != null)
            ladderManager.resultButton.interactable = true;


        // ✅ 사다리 매니저가 연결되어 있다면 다음 라운드 초기화
        if (ladderManager != null)
        {
            Debug.Log("OnCloseResult(): GenerateLadder 호출됨");

            ladderManager.GenerateLadder();

            // ✅ 배팅 UI도 다시 활성화
            if (ladderManager.betAmountUIManager != null)
            {
                ladderManager.betAmountUIManager.SetInteractable(true);
            }

            // ✅ 보드 텍스트 강제 활성화
            if (ladderManager.boardText != null)
                ladderManager.boardText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("LadderManager가 연결되지 않았습니다.");
        }

        // ✅ 배팅 UI 다시 사용 가능
        if (ladderManager?.betAmountUIManager != null)
            ladderManager.betAmountUIManager.SetInteractable(true);
    }
}