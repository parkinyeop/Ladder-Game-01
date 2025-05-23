// ✅ BetAmountUIManager.cs (리팩토링 버전 with 자세한 주석)
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BetAmountUIManager
/// - 배팅 금액을 관리하는 UI 시스템
/// - 슬라이더 또는 버튼을 통해 배팅 금액을 설정하고, 잔액에 따라 제한을 둠
/// - 설정된 금액은 float 기준으로 유지되며 UI에는 정수처럼 표현됨
/// </summary>
public class BetAmountUIManager : MonoBehaviour
{
    [Header("슬라이더 및 텍스트")]
    public Slider betSlider;
    public TMP_Text betAmountText;

    [Header("퀵 배팅 버튼")]
    public Button bet1Button;
    public Button bet5Button;
    public Button bet10Button;
    public Button bet50Button;
    public Button bet100Button;

    [Header("외부 연결")]
    public LadderManager ladderManager; // 사다리 매니저
    [SerializeField] private Button resultButton; // 결과 버튼

    private readonly List<Button> betButtons = new();
    private readonly Color highlightColor = new Color(0.2f, 0.8f, 1f);
    private readonly Color defaultColor = Color.white;

    private float betAmount = 0f; // 현재 설정된 배팅 금액
    private float currentBetAmount = 0f; // 마지막 확정 배팅 금액

    public event Action<float> OnBetConfirmed; // 외부 리스너

    private void Start()
    {
        InitSlider();
        InitButtons();
        SetBetAmount(0f);
        UpdateBetAmountText();
    }

    private void InitSlider()
    {
        if (betSlider == null) return;

        betSlider.minValue = 1;
        betSlider.maxValue = 100;
        betSlider.wholeNumbers = true;
        betSlider.value = betAmount;
        betSlider.onValueChanged.AddListener(SetBetAmount);
    }

    private void InitButtons()
    {
        betButtons.AddRange(new[] { bet1Button, bet5Button, bet10Button, bet50Button, bet100Button });

        AddButtonListener(bet1Button, 1f);
        AddButtonListener(bet5Button, 5f);
        AddButtonListener(bet10Button, 10f);
        AddButtonListener(bet50Button, 50f);
        AddButtonListener(bet100Button, 100f);
    }

    private void AddButtonListener(Button button, float amount)
    {
        if (button != null)
            button.onClick.AddListener(() => SetBetAmount(amount));
    }

    /// <summary>
    /// 배팅 금액을 설정하고 UI 동기화, 유효성 검사, 버튼 강조, 퀵배팅 취소까지 모두 처리
    /// </summary>
    public void SetBetAmount(float amount)
    {
        // ✅ 항상 최신 CoinManager 기준으로 보유 금액을 확인
        float coin = FindObjectOfType<CoinManager>()?.playerBalance ?? 0f;

        Debug.Log($"💰 [BetAmountUI] SetBetAmount 호출됨 - 보유: {coin:F1}, 시도: {amount}");

        // ✅ 동일 금액 다시 클릭 시 → 선택 해제 처리
        if (Mathf.Approximately(betAmount, amount))
        {
            ResetBet(); // → 아래 정의된 초기화 함수
            return;
        }

        // ✅ 잔액 부족 또는 0원인 경우: 배팅 불가 처리
        if (amount > coin || coin <= 0f)
        {
            Debug.LogWarning($"❌ 배팅 실패 - 잔액 부족 또는 0원. 보유: {coin}, 요청: {amount}");

            ResetBet(); // UI 초기화 및 상태 초기화
            return;
        }

        // ✅ 정상 배팅 처리
        betAmount = amount;
        currentBetAmount = amount;

        // ✅ 슬라이더 위치 동기화
        if (betSlider != null && !Mathf.Approximately(betSlider.value, amount))
        {
            betSlider.onValueChanged.RemoveAllListeners();
            betSlider.value = amount;
            betSlider.onValueChanged.AddListener(SetBetAmount); // 간결한 람다 제거
        }

        // ✅ 텍스트 및 버튼 UI 반영
        UpdateBetAmountText();
        UpdateButtonColors(amount);

        // ✅ 결과 버튼 활성화
        if (ladderManager != null && ladderManager.IsInReadyState())
            ladderManager.resultButton.interactable = (betAmount > 0f);

        ConfirmBet(); // ✅ 외부 로직에 배팅 확정 전달
    }

    // 🔄 선택 취소 함수 추가
    private void ResetBet()
    {
        Debug.Log("🔄 ResetBet() 호출됨 - 선택 해제");

        betAmount = 0f;
        currentBetAmount = 0f;

        if (betSlider != null)
        {
            // 🔐 슬라이더 이벤트 잠시 제거
            betSlider.onValueChanged.RemoveListener(SetBetAmount);
            betSlider.value = 0f;
            betSlider.onValueChanged.AddListener(SetBetAmount);
        }

        UpdateBetAmountText();
        ResetButtonColors();

        if (ladderManager != null)
        {
            ladderManager.SetGoalButtonsInteractable(false);
            ladderManager.resultButton.interactable = false;
        }

        // ✅ 외부에 확정 취소 알림
        OnBetConfirmed?.Invoke(0f);
    }

    private void UpdateButtonColors(float activeAmount)
    {
        foreach (var btn in betButtons)
        {
            float value = 0f;

            if (btn == bet1Button) value = 1f;
            else if (btn == bet5Button) value = 5f;
            else if (btn == bet10Button) value = 10f;
            else if (btn == bet50Button) value = 50f;
            else if (btn == bet100Button) value = 100f;

            btn.GetComponent<Image>().color = Mathf.Approximately(activeAmount, value) ? highlightColor : defaultColor;
        }
    }

    /// <summary>
    /// 퀵배팅 버튼 중 선택된 버튼을 강조 표시하고 나머지는 기본색으로 초기화
    /// </summary>
    private void HighlightSelectedButton(Button selected)
{
    foreach (var btn in betButtons)
    {
        if (btn == null) continue;

        var image = btn.GetComponent<Image>();
        image.color = (btn == selected) ? highlightColor : defaultColor;
    }
}

    private void SetInvalidBet(float coin)
    {
        betAmount = 0f;
        currentBetAmount = 0f;

        UpdateSlider(0f);
        UpdateBetAmountText();

        if (ladderManager != null)
        {
            ladderManager.SetGoalButtonsInteractable(false);
            if (ladderManager.IsInReadyState())
                resultButton.interactable = false;
            if (ladderManager.boardText != null)
                ladderManager.boardText.text = "NOT ENOUGH BALANCE";
        }
    }

    private void UpdateSlider(float value)
    {
        if (betSlider != null && betSlider.value != value)
        {
            betSlider.onValueChanged.RemoveAllListeners();
            betSlider.value = value;
            betSlider.onValueChanged.AddListener(SetBetAmount);
        }
    }

    private void UpdateBetAmountText()
    {
        if (betAmountText != null)
            betAmountText.text = $"Betting: {betAmount:F1} Coins";

        if (ladderManager != null)
            ladderManager.SetGoalButtonsInteractable(betAmount > 0f);
    }

    private void ResetButtonColors()
    {
        foreach (var btn in betButtons)
        {
            if (btn != null)
                btn.GetComponent<Image>().color = defaultColor;
        }
    }

    private void HighlightButton(float amount)
    {
        foreach (var btn in betButtons)
        {
            float val = GetButtonValue(btn);
            if (Mathf.Approximately(val, amount))
                btn.GetComponent<Image>().color = Color.yellow;
        }
    }

    private float GetButtonValue(Button btn)
    {
        if (btn == bet1Button) return 1f;
        if (btn == bet5Button) return 5f;
        if (btn == bet10Button) return 10f;
        if (btn == bet50Button) return 50f;
        if (btn == bet100Button) return 100f;
        return 0f;
    }

    private void UpdateBoardText(float coin)
    {
        if (ladderManager != null && ladderManager.boardText != null)
        {
            ladderManager.boardText.text = (betAmount > 0f && coin >= betAmount)
                ? "PRESS READY BUTTON"
                : "INPUT YOUR BET AMOUNT";
        }
    }

    public float GetBetAmount() => betAmount;

    public void ConfirmBet()
    {
        Debug.Log($"✅ 베팅 확정: {currentBetAmount} 코인");
        OnBetConfirmed?.Invoke(currentBetAmount);
    }

    /// <summary>
    /// 보유 코인에 따라 퀵 배팅 버튼의 활성 상태를 갱신
    /// </summary>
    public void UpdateQuickBetButtons(float currentCoin)
    {
        foreach (var btn in betButtons)
        {
            if (btn == null) continue;

            float value = GetButtonValue(btn); // 1, 5, 10, 50, 100 등
            bool isAffordable = (value <= currentCoin);

            btn.interactable = isAffordable;

            // ✅ 색상 적용
            if (btn.targetGraphic != null)
            {
                btn.targetGraphic.color = isAffordable ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
            }

            // ✅ 텍스트 색상도 조정
            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.color = isAffordable ? Color.black : new Color(0.6f, 0.6f, 0.6f, 1f);
            }
        }
    }

    /// <summary>
    /// 슬라이더와 퀵 배팅 버튼들의 활성화 상태를 설정합니다.
    /// </summary>
    public void SetInteractable(bool isInteractable)
    {
        if (betSlider != null)
            betSlider.interactable = isInteractable;

        Debug.Log($"🟢 SetInteractable({isInteractable}) - 버튼은 항상 활성화");
    }
}