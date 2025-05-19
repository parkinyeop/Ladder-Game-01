using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public string userId = "user002";
    public float playerBalance;

    [Header("🔗 UI 연결")]
    public TextMeshProUGUI balanceText; // UI에서 연결할 텍스트 필드

    void Start()
    {
        Debug.Log("🟢 CoinManager 시작");
        StartCoroutine(GetBalance(userId)); // 테스트용 유저 ID
    }

    IEnumerator GetBalance(string userId)
    {
        string url = $"http://localhost:3000/coin/{userId}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ 요청 실패: {request.error}");
        }
        else
        {
            string json = request.downloadHandler.text;
            Debug.Log($"✅ 서버 응답: {json}");

            // 🎯 JSON → C# 객체로 변환
            CoinBalanceResponse data = JsonUtility.FromJson<CoinBalanceResponse>(json);

            // 🎯 결과 확인 및 변수에 저장
            Debug.Log($"🟢 User: {data.user_id}, Balance: {data.balance}");
            playerBalance = data.balance;

            // 💡 이후 로직에 활용 가능

            // ✅ UI에 표시
            if (balanceText != null)
                balanceText.text = $"Balance: {playerBalance:F1} Coins";
        }
    }
}