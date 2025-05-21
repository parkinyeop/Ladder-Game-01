using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

[System.Serializable]
public class CoinBalanceResponse
{
    public string user_id;
    public float balance;
}

[System.Serializable]
public class AmountRequest
{
    public float amount;
}

[System.Serializable]
public class CoinUpdateResponse
{
    public string user_id;
    public float updated_balance;
}

public class CoinManager : MonoBehaviour
{
    [Header("🧾 사용자 정보")]
    public string userId = "user002";

    [Header("💰 코인 잔액")]
    public float playerBalance;

    [Header("🔗 UI 연결")]
    public TextMeshProUGUI balanceText; // TMP 텍스트 연결

    [Header("⏱️ 갱신 주기 (초)")]
    public float refreshInterval = 5f; // 5초마다 갱신

    [System.Serializable]
    public class AmountRequest
    {
        public float amount;
    }


    private void Start()
    {
        // 최초 1회 조회
        //StartCoroutine(GetBalance());

        // 주기적 갱신 시작
        StartCoroutine(RefreshBalanceLoop());
    }

    private string jwtToken;

    public void SetAuthToken(string token)
    {
        jwtToken = token;
        Debug.Log("🟢 JWT 토큰 설정됨: " + jwtToken);
    }


    /// <summary>
    /// 주기적으로 코인 잔액을 조회하는 루프
    /// </summary>
    private IEnumerator RefreshBalanceLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(refreshInterval);
            yield return StartCoroutine(GetBalance());
        }
    }

    /// <summary>
    /// 로그인 후 사용자 ID를 설정하고 최초 잔액을 조회
    /// </summary>
    public void SetUserId(string userId)
    {
        this.userId = userId;

        if (!string.IsNullOrEmpty(userId))
        {
            StartCoroutine(GetBalance());
        }
    }

    /// <summary>
    /// 서버에서 잔액 조회
    /// </summary>
    private IEnumerator GetBalance()
    {
        string url = $"http://localhost:3000/coin/{userId}";
        UnityWebRequest request = UnityWebRequest.Get(url);

        // 🔐 JWT 토큰 헤더 추가
        if (!string.IsNullOrEmpty(jwtToken))
            request.SetRequestHeader("Authorization", "Bearer " + jwtToken);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ 서버 통신 실패: Code {request.responseCode} - {request.error} - URL: {url}");
        }
    
        else
        {
            string json = request.downloadHandler.text;
            Debug.Log($"✅ 서버 응답: {json}");

            CoinBalanceResponse data = JsonUtility.FromJson<CoinBalanceResponse>(json);
            playerBalance = data.balance;

            // ✅ UI 텍스트 갱신
            if (balanceText != null)
                balanceText.text = $"Balance: {playerBalance:F1} Coins";
        }
    }

    /// <summary>
    /// 서버에 POST 요청을 보내 코인 잔액을 변경
    /// </summary>
    /// <param name="amount">변경할 금액 (양수: 추가, 음수: 차감)</param>
    public void ModifyBalance(float amount)
    {
        StartCoroutine(UpdateBalanceRequest(amount));
    }

    private IEnumerator UpdateBalanceRequest(float amount)
    {
        string url = $"http://localhost:3000/coin/{userId}";

        // JSON 데이터 생성
        string jsonBody = JsonUtility.ToJson(new AmountRequest { amount = amount });

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // ✅ JWT 토큰 헤더 추가
        if (!string.IsNullOrEmpty(jwtToken))
            request.SetRequestHeader("Authorization", "Bearer " + jwtToken);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ 코인 업데이트 실패: {request.error}");
        }
        else
        {
            string response = request.downloadHandler.text;
            Debug.Log($"✅ 코인 업데이트 응답: {response}");

            CoinUpdateResponse data = JsonUtility.FromJson<CoinUpdateResponse>(response);
            playerBalance = data.updated_balance;

            if (balanceText != null)
                balanceText.text = $"Balance: {playerBalance:F1} Coins";
        }
    }

    // 🟡 🔽 이 아래 위치에 추가하세요
    public IEnumerator SendRewardRequest(
        string user_id,
        float betAmount,
        float goalMultiplier,
        float startMultiplier,
        int verticalCount,
        bool isSuccess
    )
    {
        string url = "http://localhost:3000/api/reward";

        // ✅ JSON 데이터 준비
        RewardRequestData data = new RewardRequestData
        {
            user_id = user_id,
            bet_amount = betAmount,
            goal_multiplier = goalMultiplier,
            start_multiplier = startMultiplier,
            vertical_count = verticalCount,
            is_success = isSuccess
        };

        string jsonBody = JsonUtility.ToJson(data);

        // ✅ 요청 구성
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        // ✅ JWT 토큰 인증 헤더 추가
        if (!string.IsNullOrEmpty(jwtToken))
            request.SetRequestHeader("Authorization", "Bearer " + jwtToken);

        // ✅ 요청 전송
        yield return request.SendWebRequest();

        // ✅ 응답 처리
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ 보상 요청 실패: {request.responseCode} - {request.error}");
        }
        else
        {
            string response = request.downloadHandler.text;
            Debug.Log($"✅ 보상 응답: {response}");

            RewardResponse reward = JsonUtility.FromJson<RewardResponse>(response);
            playerBalance = reward.updated_balance;

            if (balanceText != null)
                balanceText.text = $"Balance: {playerBalance:F1} Coins";
        }
    }
    [System.Serializable]
        public class RewardRequestData
        {
            public string user_id;
            public float bet_amount;
            public float goal_multiplier;
            public float start_multiplier;
            public int vertical_count;
            public bool is_success;
        }

        [System.Serializable]
        public class RewardResponse
        {
            public string user_id;
            public float updated_balance;
        }
                
    }

