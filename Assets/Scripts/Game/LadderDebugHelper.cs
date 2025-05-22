using UnityEngine;

public static class LadderDebugHelper
{
    /// <summary>
    /// ladderMap의 내용을 로그로 출력
    /// </summary>
    public static void LogLadderMap(bool[,] map, string context = "ladderMap")
    {
        if (map == null)
        {
            Debug.LogError($"❌ {context} is NULL");
            return;
        }

        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        Debug.Log($"📋 {context} 디버그 출력 (rows={rows}, cols={cols})");
        for (int y = 0; y < rows; y++)
        {
            string row = "";
            for (int x = 0; x < cols; x++)
            {
                row += map[y, x] ? "1 " : "0 ";
            }
            Debug.Log($"[{context}] y={y} : {row}");
        }
    }

    /// <summary>
    /// ladderMap의 참조값 출력 (GetHashCode 기반)
    /// </summary>
    public static void LogLadderMapReference(bool[,] map, string context)
    {
        if (map == null)
        {
            Debug.LogError($"❌ {context} map is NULL (참조 없음)");
            return;
        }

        Debug.Log($"🔗 {context} map ref: {map.GetHashCode()}");
    }
}