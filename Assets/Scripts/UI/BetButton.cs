using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BetButton
/// - 고정된 베팅 금액을 갖는 버튼
/// - 클릭 시 BetAmountUIManager에 베팅 금액 전달
/// </summary>
public class BetButton : MonoBehaviour
{
    [Tooltip("이 버튼이 설정할 베팅 금액")]
    public int betAmount = 1;

    private void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() =>
            {
                BetAmountUIManager manager = FindObjectOfType<BetAmountUIManager>();

                // ✅ 보유 금액보다 높은 경우 클릭 무시
                if (manager != null && manager.ladderManager != null)
                {
                    float currentCoin = manager.ladderManager.currentCoin;

                    if (betAmount > currentCoin)
                    {
                        Debug.LogWarning($"🚫 {betAmount} 코인 버튼 클릭 무시됨 (보유: {currentCoin})");
                        return; // 무시 처리
                    }

                    manager.SetBetAmount(betAmount);
                }
            });
        }
    }
}
