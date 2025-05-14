// ✅ BetAmountUIManager.cs (float 기반 버전 + 자세한 주석 포함)
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BetAmountUIManager
/// - 배팅 금액을 관리하는 UI 시스템
/// - 슬라이더 또는 버튼을 통해 배팅 금액을 설정하고, 보유 코인에 따라 유효성 검사를 진행
/// - 설정된 금액은 float 기준으로 유지되며 정수처럼 보이도록 처리
/// </summary>
public class BetAmountUIManager : MonoBehaviour
{
    [Header("슬라이더 및 텍스트")]
    public Slider betSlider;                  // 배팅 금액 조절용 슬라이더
    public TMP_Text betAmountText;           // 현재 설정된 배팅 금액 표시 텍스트

    [Header("프리셋 버튼들")]
    public Button bet1Button;
    public Button bet5Button;
    public Button bet10Button;
    public Button bet50Button;
    public Button bet100Button;

    public LadderManager ladderManager;      // 외부의 LadderManager 연결

    private List<Button> betButtons = new(); // 버튼 목록 (색상 초기화 및 클릭 처리용)

    private readonly Color highlightColor = new Color(0.2f, 0.8f, 1f); // 선택된 버튼 색
    private readonly Color defaultColor = Color.white;                 // 기본 버튼 색

    public event Action<float> OnBetConfirmed; // 외부에서 배팅 확정 이벤트 리스닝

    [SerializeField] private Button resultButton; // 결과 버튼 참조

    private float betAmount = 0f;          // 설정된 배팅 금액
    private float currentBetAmount = 0f;   // 마지막으로 확정된 배팅 금액

    private void Start()
    {
        // 슬라이더 설정 및 이벤트 연결
        if (betSlider != null)
        {
            betSlider.minValue = 1;
            betSlider.maxValue = 100;
            betSlider.wholeNumbers = true; // UI상은 정수로 보이게
            betSlider.value = betAmount;
            betSlider.onValueChanged.AddListener((value) => SetBetAmount(value));
        }

        // 버튼 연결
        betButtons.AddRange(new[] { bet1Button, bet5Button, bet10Button, bet50Button, bet100Button });
        if (bet1Button != null) bet1Button.onClick.AddListener(() => SetBetAmount(1f));
        if (bet5Button != null) bet5Button.onClick.AddListener(() => SetBetAmount(5f));
        if (bet10Button != null) bet10Button.onClick.AddListener(() => SetBetAmount(10f));
        if (bet50Button != null) bet50Button.onClick.AddListener(() => SetBetAmount(50f));
        if (bet100Button != null) bet100Button.onClick.AddListener(() => SetBetAmount(100f));

        // 초기 배팅 금액 설정
        SetBetAmount(0f);
        UpdateBetAmountText();
    }

    /// <summary>
    /// 사용자가 배팅을 확정할 때 호출되는 함수 (외부에서 호출)
    /// </summary>
    public void ConfirmBet()
    {
        Debug.Log($"✅ 베팅 확정: {currentBetAmount} 코인");
        OnBetConfirmed?.Invoke(currentBetAmount);
    }

    /// <summary>
    /// 배팅 금액을 설정하고 UI와 로직을 동기화함
    /// </summary>
    // ✅ SetBetAmount()에서 유효한 배팅일 경우 ConfirmBet() 자동 호출 추가
    public void SetBetAmount(float amount)
    {
        float coin = ladderManager != null ? ladderManager.currentCoin : 0f;

        foreach (var btn in betButtons)
        {
            if (btn != null)
                btn.GetComponent<Image>().color = defaultColor;
        }

        // ❌ 잔고 부족 또는 0원일 경우: 배팅 불가 처리
        if (amount > coin || coin <= 0f)
        {
            if (ladderManager != null && ladderManager.boardText != null)
                ladderManager.boardText.text = coin <= 0f ? "잔고가 부족합니다" : "보유 금액보다 큰 배팅입니다";

            betAmount = 0f;
            currentBetAmount = 0f;

            if (betSlider != null)
            {
                betSlider.onValueChanged.RemoveAllListeners();
                betSlider.value = 0f;
                betSlider.onValueChanged.AddListener((value) => SetBetAmount(value));
            }

            UpdateBetAmountText();

            if (ladderManager != null) ladderManager.SetGoalButtonsInteractable(false);
            if (ladderManager != null && ladderManager.IsInReadyState())
                ladderManager.resultButton.interactable = false;

            return;
        }

        // ✅ 유효한 배팅 금액일 경우
        betAmount = amount;
        currentBetAmount = amount;

        if (betSlider != null && betSlider.value != amount)
        {
            betSlider.onValueChanged.RemoveAllListeners();
            betSlider.value = amount;
            betSlider.onValueChanged.AddListener((value) => SetBetAmount(value));
        }

        UpdateBetAmountText();

        foreach (var btn in betButtons)
        {
            if ((btn == bet1Button && amount == 1f) ||
                (btn == bet5Button && amount == 5f) ||
                (btn == bet10Button && amount == 10f) ||
                (btn == bet50Button && amount == 50f) ||
                (btn == bet100Button && amount == 100f))
            {
                btn.GetComponent<Image>().color = Color.yellow;
            }
        }

        if (ladderManager != null && ladderManager.IsInReadyState())
            ladderManager.resultButton.interactable = (betAmount > 0f);

        // ✅ 자동 확정 호출 추가
        ConfirmBet();
    }

    /// <summary>
    /// 현재 배팅 금액을 반환 (float 단위)
    /// </summary>
    public float GetBetAmount()
    {
        return betAmount;
    }

    /// <summary>
    /// 배팅 금액 텍스트 UI를 갱신
    /// </summary>
    private void UpdateBetAmountText()
    {
        if (betAmountText != null)
            betAmountText.text = $"Betting: {betAmount:F1} Coins";

        if (ladderManager != null)
            ladderManager.SetGoalButtonsInteractable(betAmount > 0f);
    }

    /// <summary>
    /// 보유 코인에 따라 퀵 배팅 버튼의 활성 상태를 갱신
    /// </summary>
    public void UpdateQuickBetButtons(float currentCoin)
    {
        foreach (var btn in betButtons)
        {
            if (btn == null) continue;
            float value = 0f;

            if (btn == bet1Button) value = 1f;
            else if (btn == bet5Button) value = 5f;
            else if (btn == bet10Button) value = 10f;
            else if (btn == bet50Button) value = 50f;
            else if (btn == bet100Button) value = 100f;

            btn.interactable = (value <= currentCoin);
        }
    }

    /// <summary>
    /// UI 요소들의 인터랙션 활성/비활성 설정 (슬라이더 및 버튼)
    /// </summary>
    public void SetInteractable(bool isInteractable)
    {
        if (betSlider != null) betSlider.interactable = isInteractable;
        if (bet1Button != null) bet1Button.interactable = isInteractable;
        if (bet5Button != null) bet5Button.interactable = isInteractable;
        if (bet10Button != null) bet10Button.interactable = isInteractable;
        if (bet50Button != null) bet50Button.interactable = isInteractable;
        if (bet100Button != null) bet100Button.interactable = isInteractable;
    }
}
