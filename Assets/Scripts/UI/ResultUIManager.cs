using UnityEngine;
using UnityEngine.UI; // ✅ UI 요소 (Text, Image, Button)를 위한 네임스페이스

/// <summary>
/// ResultUIManager
/// - 성공/실패 결과를 팝업으로 보여주는 UI 매니저
/// </summary>
public class ResultUIManager : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject resultPanel;      // 결과 패널 (처음엔 꺼져 있음)
    public Text resultMessageText;             // 결과 메시지 텍스트
    public Button closeButton;          // 닫기 버튼

    [SerializeField] private LadderManager ladderManager;

    private void Start()
    {
        // 초기에는 패널 비활성화
        if (resultPanel != null)
            resultPanel.SetActive(false);

        // 닫기 버튼 연결
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    /// <summary>
    /// 결과 UI 표시
    /// </summary>
    public void ShowResult(string message)
    {
        if (resultPanel != null)
            resultPanel.SetActive(true); // ✅ 결과 판넬 보이게

        if (resultMessageText != null)
            resultMessageText.text = message;
    }

    /// <summary>
    /// 결과 UI 닫기
    /// </summary>
    public void Hide()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    public void OnCloseResult()
    {
        // ✅ 결과창 숨기기
        resultPanel.SetActive(false);

        // ✅ 사다리 매니저가 연결되어 있는지 확인
        if (ladderManager != null)
        {
            Debug.Log("🟢 OnCloseResult(): GenerateLadder 호출됨");

            // 🔄 사다리 다시 생성 (다음 라운드 시작)
            ladderManager.GenerateLadder();

            // ✅ 배팅 UI 다시 활성화
            if (ladderManager.betAmountUIManager != null)
            {
                ladderManager.betAmountUIManager.SetInteractable(true);
            }
        }
        else
        {
            Debug.LogError("❌ LadderManager가 연결되지 않았습니다.");
        }
    }
}