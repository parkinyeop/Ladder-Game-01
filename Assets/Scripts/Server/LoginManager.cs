using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;

/// <summary>
/// Unity에서 사용자 로그인을 처리하는 클래스
/// 서버로 로그인 요청을 보내고, 응답을 받아 로그인 성공 여부를 판단
/// </summary>
public class LoginManager : MonoBehaviour
{
    [Header("🔐 로그인 UI 입력")]
    public TMP_InputField usernameInput;         // 사용자 ID 입력 필드
    public TMP_InputField passwordInput;         // 사용자 비밀번호 입력 필드
    public TextMeshProUGUI resultText;           // 로그인 결과 메시지를 출력할 텍스트

    [Header("🪟 로그인 UI 패널")]
    public GameObject loginPanel;                // 로그인 UI 전체 패널 (성공 시 비활성화)

    private string currentJwtToken;

    /// <summary>
    /// 로그인 버튼 클릭 시 호출
    /// </summary>
    public void OnLoginClicked()
    {
        string userId = usernameInput.text.Trim();
        string password = passwordInput.text.Trim();

        // ✅ 입력값 검사
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            resultText.text = "Please enter your ID and password.";
            return;
        }

        // ✅ 로그인 요청 코루틴 시작
        StartCoroutine(SendLoginRequest(userId, password));
    }

    /// <summary>
    /// 로그인 성공 후 JWT 토큰을 저장하는 메서드
    /// </summary>
    public void SetJwtToken(string token)
    {
        currentJwtToken = token;
        Debug.Log("✅ JWT 토큰 저장됨: " + token);
    }


    void OnLoginSuccess(string token)
    {
        Debug.Log("✅ 로그인 성공, JWT 토큰 수신: " + token);

        // LadderManager 찾기
        LadderManager ladderManager = FindObjectOfType<LadderManager>();
        if (ladderManager != null)
        {
            ladderManager.SetJwtToken(token); // ✅ 토큰 전달
        }
        else
        {
            Debug.LogWarning("⚠ LadderManager를 찾을 수 없습니다. 토큰 전달 실패");
        }

        // 이후 UI 닫기, 로그인 패널 숨기기 등 처리
    }

    /// <summary>
    /// <summary>
    /// 로그인 요청에 사용되는 JSON 구조
    /// 서버와 key 이름 일치 필요 (user_id, password)
    /// </summary>
    [System.Serializable]
    public class LoginRequest
    {
        public string user_id;
        public string password;
        public string token; // ✅ JWT 토큰 필드 추가
    }


    /// <summary>
    /// 서버에서 응답받는 JSON 구조
    /// </summary>
    [System.Serializable]
    public class LoginResponse
    {
        public bool success;
        public string user_id;
        public string token;  // ✅ 추가: 서버에서 전달되는 JWT 토큰을 저장할 변수
    }

    /// 서버로 로그인 POST 요청을 전송하는 코루틴
    /// </summary>
    private IEnumerator SendLoginRequest(string userId, string password)
    {
        string url = "http://127.0.0.1:3000/auth/login"; // ✅ localhost 대신 127.0.0.1

        // ✅ JSON 요청 본문 구성
        LoginRequest loginData = new LoginRequest
        {
            user_id = userId,
            password = password
        };

        string jsonBody = JsonUtility.ToJson(loginData);

        // ✅ UnityWebRequest 구성
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log($"🔐 로그인 요청: {userId}, {password}");

        // ✅ 요청 전송
        yield return request.SendWebRequest();

        // ✅ 실패 처리
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ 로그인 요청 실패: " + request.error);
            Debug.Log($"🔴 응답 코드: {request.responseCode}");
            Debug.Log($"🔴 응답 본문: {request.downloadHandler.text}");
            resultText.text = "Login Failed";
            yield break;
        }

        // ✅ 성공 응답 처리
        string responseJson = request.downloadHandler.text;
        Debug.Log("✅ 로그인 성공 응답 수신: " + responseJson);

        LoginResponse response = JsonUtility.FromJson<LoginResponse>(responseJson);
        Debug.Log($"🧾 받은 토큰: {response.token}");

        if (response.success)
        {
            resultText.text = $"Login Success! Welcome, {response.user_id}";

            // ✅ 로그인 패널 비활성화
            if (loginPanel != null)
                loginPanel.SetActive(false);

            // ✅ CoinManager 처리
            CoinManager coinManager = FindObjectOfType<CoinManager>();
            if (coinManager != null)
            {
                coinManager.SetUserId(response.user_id);
                coinManager.SetAuthToken(response.token); // ✅ 반드시 토큰 먼저

                // ✅ 코루틴도 CoinManager가 실행하도록 위임
                coinManager.StartBalanceRequest(); // 아래 정의 참고
                //StartCoroutine(coinManager.GetBalance()); // ✅ 토큰 설정 후 호출
            }

            // ✅ LadderManager 토큰 전달
            LadderManager ladderManager = FindObjectOfType<LadderManager>();
            if (ladderManager != null)
            {
                ladderManager.SetJwtToken(response.token);
            }

            // ✅ 추가 처리: 게임 시작 UI로 전환 등
            // OnLoginSuccess(response.token); // 원한다면 콜백 방식으로 분리 가능
        }
        else
        {
            resultText.text = "Invalid ID or password.";
        }
    }

}