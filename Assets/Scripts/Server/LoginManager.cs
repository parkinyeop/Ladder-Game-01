using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

/// <summary>
/// 로그인 기능을 담당하는 클래스
/// </summary>
public class LoginManager : MonoBehaviour
{
    [Header("🔐 로그인 UI 입력")]
    public TMP_InputField usernameInput;         // 사용자 ID 입력창
    public TMP_InputField passwordInput;         // 비밀번호 입력창
    public TextMeshProUGUI resultText;           // 결과 메시지를 출력할 텍스트

    [Header("🪟 로그인 패널")]
    public GameObject loginPanel; // 로그인 UI 패널을 할당

    /// <summary>
    /// 로그인 버튼 클릭 시 호출됨
    /// </summary>
    public void OnLoginClicked()
    {
        string userId = usernameInput.text.Trim();
        string password = passwordInput.text.Trim();

        // 유효성 검사
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            resultText.text = "Please enter your ID and password.";
            return;
        }

        // 로그인 요청 코루틴 시작
        StartCoroutine(SendLoginRequest(userId, password));
    }

    /// <summary>
    /// 서버로 로그인 POST 요청 전송
    /// </summary>
    private IEnumerator SendLoginRequest(string userId, string password)
    {
        string url = "http://localhost:3000/auth/login";

        // 서버에서 요구하는 JSON 키에 맞게 구조화
        LoginRequest loginData = new LoginRequest
        {
            user_id = userId,
            password = password
        };

        string jsonBody = JsonUtility.ToJson(loginData);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // 요청 전송
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            resultText.text = "Login Failed";
            Debug.Log($"🔑 로그인 요청: {userId}, {password}");
            Debug.LogError("❌ 요청 오류: " + request.error);
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log("✅ 서버 응답: " + responseJson);

            // 응답 데이터 파싱
            LoginResponse response = JsonUtility.FromJson<LoginResponse>(responseJson);

            if (response.success)
            {
                resultText.text = $"Login Success! Welcome, {response.user_id}";
                // TODO: 로그인 성공 후 후속 처리 (예: 사용자 ID 저장, 화면 전환 등)
                // ✅ 로그인 UI 패널 비활성화 (닫기)
                if (loginPanel != null)
                    loginPanel.SetActive(false);

                // ✅ CoinManager에 로그인한 user_id 전달
                CoinManager coinManager = FindObjectOfType<CoinManager>();
                if (coinManager != null)
                    coinManager.SetUserId(response.user_id);
            }
        
            else
            {
                resultText.text = "Invalid ID or password.";
            }
        }
    }

    /// <summary>
    /// 로그인 요청에 사용할 데이터 구조 (서버와 key 일치 필요)
    /// </summary>
    [System.Serializable]
    public class LoginRequest
    {
        public string user_id;   // 서버는 user_id 라는 key 를 사용
        public string password;
    }

    /// <summary>
    /// 로그인 응답 구조
    /// </summary>
    [System.Serializable]
    public class LoginResponse
    {
        public bool success;
        public string user_id;
    }
}