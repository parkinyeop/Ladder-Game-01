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
                if (manager != null)
                {
                    manager.SetBetAmount(betAmount);
                }
            });
        }
    }
}
