using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingUIManager : MonoBehaviour
{
    public GameObject loadingPanel;         // 로딩 패널
    public TextMeshProUGUI loadingText;     // "Loading..." 텍스트
    public Slider progressSlider;           // 슬라이더 방식의 진행도 바
    public float duration = 0.5f;

    /// <summary>
    /// 로딩 화면을 0.5초 동안 표시
    /// </summary>
    public void StartLoading()
    {
        StartCoroutine(ShowLoadingCoroutine());
    }

    private IEnumerator ShowLoadingCoroutine()
    {
        loadingPanel.SetActive(true);

        
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            if (progressSlider != null)
                progressSlider.value = progress;
            if (loadingText != null)
                loadingText.text = $"Loading... {(progress * 100f):0}%";
            yield return null;
        }

        loadingPanel.SetActive(false);
    }
}