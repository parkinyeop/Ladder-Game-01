using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BetManager
/// - 배팅 관련 로직을 담당
/// - 금액 설정, 결과 버튼 활성화, 기대값 갱신 등의 책임 분리
/// </summary>
public class BetManager
{
    private LadderManager ladderManager;
    private BetAmountUIManager uiManager;

    public event Action<int> OnBetConfirmed;

    public BetManager(LadderManager ladderManager, BetAmountUIManager uiManager)
    {
        this.ladderManager = ladderManager;
        this.uiManager = uiManager;

        uiManager.OnBetConfirmed += HandleBetConfirmed;
    }

    /// <summary>
    /// UI를 초기화 (슬라이더, 버튼 등)
    /// </summary>
    public void Initialize()
    {
        uiManager.SetInteractable(true);
        uiManager.SetBetAmount(0);
    }

    /// <summary>
    /// 외부에서 금액을 직접 설정할 경우
    /// </summary>
    public void SetBetAmount(int amount)
    {
        uiManager.SetBetAmount(amount);
    }

    /// <summary>
    /// 현재 금액 반환
    /// </summary>
    public int GetCurrentBetAmount()
    {
        return uiManager.GetBetAmount();
    }

    /// <summary>
    /// UI 및 결과버튼 활성화 여부 갱신
    /// </summary>
    private void HandleBetConfirmed(int amount)
    {
        Debug.Log($"[BetManager] 배팅 확정: {amount}");

        OnBetConfirmed?.Invoke(amount);

        if (ladderManager != null && ladderManager.resultButton != null)
        {
            bool isValid = amount > 0;
            ladderManager.resultButton.interactable = isValid;
            ladderManager.resultButton.GetComponentInChildren<Text>().text = isValid ? "READY" : "잠금";
        }
    }

    public void SetInteractable(bool interactable)
    {
        uiManager?.SetInteractable(interactable);
    }
}