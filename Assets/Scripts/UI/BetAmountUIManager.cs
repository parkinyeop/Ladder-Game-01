using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // ✅ TextMeshPro 네임스페이스 추가

/// <summary>
/// BetAmountUIManager
/// - 배팅 금액을 설정하는 UI를 관리하는 클래스
/// - 슬라이더 + 미리 지정된 버튼을 통해 금액 설정 가능
/// </summary>
public class BetAmountUIManager : MonoBehaviour
{
    [Header("슬라이더 및 텍스트")]
    public Slider betSlider;                  // 금액 조절용 슬라이더
    public TMP_Text betAmountText;           // ✅ Text -> TMP_Text 변경: 현재 금액 표시 텍스트

    [Header("프리셋 버튼들")]
    public Button bet1Button;
    public Button bet5Button;
    public Button bet10Button;
    public Button bet50Button;
    public Button bet100Button;

    public LadderManager ladderManager;       // 외부에서 연결

    private List<Button> betButtons = new();  // 프리셋 버튼 목록

    private readonly Color highlightColor = new Color(0.2f, 0.8f, 1f);  // 하이라이트 색상 (하늘색)
    private readonly Color defaultColor = Color.white;                  // 기본 색상

    public event Action<int> OnBetConfirmed;   // 외부에서 배팅 금액 확정 시 사용할 이벤트

    [SerializeField] private Button resultButton;

    private int betAmount = 0;                // 설정된 배팅 금액
    private int currentBetAmount = 0;         // 확정된 배팅 금액

    private void Start()
    {
        // ✅ 슬라이더 초기 설정 및 이벤트 연결
        if (betSlider != null)
        {
            betSlider.minValue = 1;
            betSlider.maxValue = 100;
            betSlider.wholeNumbers = true;
            betSlider.value = betAmount;
            betSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        // ✅ 각 프리셋 버튼 클릭 시 배팅 금액 설정
        if (bet1Button != null) bet1Button.onClick.AddListener(() => SetBetAmount(1));
        if (bet5Button != null) bet5Button.onClick.AddListener(() => SetBetAmount(5));
        if (bet10Button != null) bet10Button.onClick.AddListener(() => SetBetAmount(10));
        if (bet50Button != null) bet50Button.onClick.AddListener(() => SetBetAmount(50));
        if (bet100Button != null) bet100Button.onClick.AddListener(() => SetBetAmount(100));

        // ✅ 초기 상태 설정
        SetBetAmount(0);
        UpdateBetAmountText();
    }

    /// <summary>
    /// 배팅 확정 버튼에서 호출되는 함수
    /// </summary>
    public void ConfirmBet()
    {
        Debug.Log($"✅ 베팅 확정: {currentBetAmount} 코인");
        OnBetConfirmed?.Invoke(currentBetAmount);
    }

    /// <summary>
    /// 슬라이더 값 변경 시 호출됨
    /// </summary>
    private void OnSliderValueChanged(float value)
    {
        SetBetAmount(Mathf.RoundToInt(value));
    }

    /// <summary>
    /// 배팅 금액을 설정하고 UI 및 슬라이더 동기화
    /// </summary>
    public void SetBetAmount(int amount)
    {
        Debug.Log($"🟢 SetBetAmount 호출됨: {amount}");

        betAmount = amount;
        currentBetAmount = amount; // 동기화

        // 슬라이더 위치 변경
        if (betSlider != null && betSlider.value != amount)
        {
            betSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            betSlider.value = amount;
            betSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        UpdateBetAmountText();

        // 배팅 금액 > 0 && READY 상태일 경우에만 결과 버튼 활성화
        if (ladderManager != null && ladderManager.IsInReadyState())
        {
            bool shouldEnable = betAmount > 0;
            ladderManager.resultButton.interactable = shouldEnable;
            Debug.Log($"🟡 결과 버튼 활성화 여부: {shouldEnable}");
        }
    }

    /// <summary>
    /// 현재 설정된 배팅 금액 반환
    /// </summary>
    public int GetBetAmount()
    {
        return betAmount;
    }

    /// <summary>
    /// 배팅 텍스트를 업데이트하고 골 버튼 활성화 여부를 조정
    /// </summary>
    private void UpdateBetAmountText()
    {
        if (betAmountText != null)
            betAmountText.text = $"베팅: {betAmount} 코인";

        // 금액이 0이면 골 버튼 비활성화
        if (ladderManager != null)
            ladderManager.SetGoalButtonsInteractable(betAmount > 0);
    }

    /// <summary>
    /// UI 활성/비활성 설정 (슬라이더 및 버튼들)
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
