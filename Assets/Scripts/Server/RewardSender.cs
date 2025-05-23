using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json; // ✅ Unity 패키지 매니저에서 Newtonsoft.Json 추가 필요

/// <summary>
/// RewardSender
/// - 서버에 사다리 결과를 보내고 보상 처리 응답을 받는 클래스
/// </summary>
public class RewardSender : MonoBehaviour
{
    /// <summary>
    /// 서버에 보상 요청을 전송하는 코루틴
    /// </summary>
    public IEnumerator SendRewardRequest(
        string token,
        float betAmount,
        int verticalCount,
        int startIndex,
        int goalIndex,
        bool[,] ladderMap
    )
    {
        string url = "http://localhost:3000/api/reward";

        // ✅ 1. ladderMap을 2차원 리스트로 변환
        List<List<bool>> ladderList = new List<List<bool>>();
        for (int y = 0; y < ladderMap.GetLength(0); y++)
        {
            List<bool> row = new List<bool>();
            for (int x = 0; x < ladderMap.GetLength(1); x++)
            {
                row.Add(ladderMap[y, x]);
            }
            ladderList.Add(row);
        }

        // ✅ 2. 서버에 보낼 데이터 객체 생성
        RewardRequestData requestData = new RewardRequestData
        {
            bet_amount = betAmount,
            vertical_count = verticalCount,
            start_index = startIndex,
            goal_index = goalIndex,
            ladder_map = ladderList
        };

        // ✅ 3. JSON 변환 (Newtonsoft.Json 사용)
        string json = JsonConvert.SerializeObject(requestData);

        // ✅ 4. 디버그용 JSON 출력
        Debug.Log("📤 전송 JSON:\n" + json);

        // ✅ 🔍 디버그 출력 (서버 전송 전에 구조 확인)
        Debug.Log("📤 서버 전송 데이터:\n" + json);

        // ✅ 5. 요청 생성 및 설정
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + token);

        // ✅ 6. 요청 전송 및 응답 대기
        yield return request.SendWebRequest();

        // ✅ 7. 응답 처리
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ 보상 응답 수신 완료: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError($"❌ 서버 요청 실패: {request.error}");
            Debug.Log($"🔴 응답 코드: {request.responseCode}");
            Debug.Log($"🔴 응답 본문: {request.downloadHandler.text}");
        }
    }

    /// <summary>
    /// 서버에 전송할 보상 요청 데이터 구조
    /// </summary>
    [System.Serializable]
    public class RewardRequestData
    {
        public float bet_amount;
        public int vertical_count;
        public int start_index;
        public int goal_index;
        public List<List<bool>> ladder_map;
    }
}