using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BetAmountUIManager
/// - 배팅 금액을 설정하는 UI를 관리하는 클래스
/// - 슬라이더 + 미리 지정된 버튼을 통해 금액 설정 가능
/// </summary>
public class BetAmountUIManager : MonoBehaviour
{
    [Header("슬라이더 및 텍스트")]
    public Slider betSlider;              // 금액 조절용 슬라이더
    public Text betAmountText;            // 현재 금액 표시용 텍스트

    [Header("프리셋 버튼들")]
    public Button bet1Button;
    public Button bet5Button;
    public Button bet10Button;
    public Button bet50Button;
    public Button bet100Button;

    public LadderManager ladderManager; // 인스펙터에서 연결 

    private List<Button> betButtons = new(); // ✅ 버튼들을 관리할 리스트

    private readonly Color highlightColor = new Color(0.2f, 0.8f, 1f);  // 하이라이트 색상 (하늘색)
    private readonly Color defaultColor = Color.white;                  // 기본 색상

    public event Action<int> OnBetConfirmed;

    [SerializeField] private Button resultButton;
    //[SerializeField] private LadderManager ladderManager;

    private int betAmount = 0;            // 현재 선택된 배팅 금액 (기본값 1)
    private int currentBetAmount = 0; // ✅ 여기 추가

    private void Start()
    {
        // ✅ 슬라이더 이벤트 연결
        if (betSlider != null)
        {
            betSlider.minValue = 1;
            betSlider.maxValue = 100;
            betSlider.wholeNumbers = true;
            betSlider.value = betAmount;

            betSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        // ✅ 버튼 이벤트 연결
        if (bet1Button != null) bet1Button.onClick.AddListener(() => SetBetAmount(1));
        if (bet5Button != null) bet5Button.onClick.AddListener(() => SetBetAmount(5));
        if (bet10Button != null) bet10Button.onClick.AddListener(() => SetBetAmount(10));
        if (bet50Button != null) bet50Button.onClick.AddListener(() => SetBetAmount(50));
        if (bet100Button != null) bet100Button.onClick.AddListener(() => SetBetAmount(100));
        // ✅ 시작 시 배팅금액 0으로 설정
        SetBetAmount(0);
        UpdateBetAmountText();
    }

    // 베팅 금액 확정 버튼에서 호출
    public void ConfirmBet()
    {
        Debug.Log($"✅ 베팅 확정: {currentBetAmount} 코인");

        // ✅ 여기서 이벤트를 호출하여 외부로 알림
        OnBetConfirmed?.Invoke(currentBetAmount);
    }

    /// <summary>
    /// 슬라이더가 변경되었을 때 호출됨
    /// </summary>
    private void OnSliderValueChanged(float value)
    {
        SetBetAmount(Mathf.RoundToInt(value));
    }

    /// <summary>
    /// 버튼 또는 슬라이더를 통해 배팅 금액을 설정함
    /// </summary>
    public void SetBetAmount(int amount)
    {
        Debug.Log($"🟢 SetBetAmount 호출됨: {amount}");

        betAmount = amount;
        currentBetAmount = amount; // ✅ 버튼 클릭 시 현재 배팅금액도 동기화

        // ✅ 슬라이더도 해당 값으로 이동시키되, 이벤트도 발생하게 둠
        if (betSlider != null && betSlider.value != amount)
        {
            betSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            betSlider.value = amount;
            betSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        UpdateBetAmountText();

        // ✅ 배팅 금액 1 이상이고 결과 버튼이 READY 상태일 때만 버튼 활성화
        if (ladderManager != null && ladderManager.IsInReadyState())
        {
            bool shouldEnable = betAmount > 0;
            ladderManager.resultButton.interactable = shouldEnable;
            Debug.Log($"🟡 결과 버튼 활성화 여부: {shouldEnable}");
        }
    }

    /// <summary>
    /// 현재 배팅 금액을 반환
    /// </summary>
    public int GetBetAmount()
    {
        return betAmount;
    }

    /// <summary>
    /// 금액 텍스트 업데이트
    /// </summary>
    private void UpdateBetAmountText()
    {
        if (betAmountText != null)
            betAmountText.text = $"베팅: {betAmount} 코인";

        // ✅ 조건에 따라 골 버튼 활성/비활성
        if (ladderManager != null)
           ladderManager.SetGoalButtonsInteractable(betAmount > 0);
    }



    // BetAmountUIManager.cs 안에 추가
    //public void OnBetConfirmed()
    //{
    //    int finalAmount = GetBetAmount();
    //    Debug.Log($"✅ 최종 배팅 금액 확정됨: {finalAmount} 코인");

    //    // 필요한 경우 LadderManager 또는 게임 로직에 전달
    //    // 예: ladderManager.SetBetAmount(finalAmount);
    //}
}