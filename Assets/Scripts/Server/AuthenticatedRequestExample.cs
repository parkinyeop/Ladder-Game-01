using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AuthenticatedRequestExample : MonoBehaviour
{
    public string userToken; // 로그인 후 받은 JWT 토큰 (예: "eyJhbGciOi...")

    public void SendAuthenticatedRequest()
    {
        StartCoroutine(SendRequest());
    }

    private IEnumerator SendRequest()
    {
        string url = "http://localhost:3000/api/reward";

        UnityWebRequest request = new UnityWebRequest(url, "POST");

        // 예시용 JSON 데이터
        string jsonBody = "{\"example\":\"value\"}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // ✅ JWT 토큰 추가
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {userToken}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ 요청 실패: {request.responseCode} - {request.error}");
        }
        else
        {
            Debug.Log($"✅ 응답 성공: {request.downloadHandler.text}");
        }
    }
}