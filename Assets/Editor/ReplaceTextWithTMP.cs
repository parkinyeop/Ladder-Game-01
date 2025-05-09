// Assets/Editor/ReplaceTextWithTMP.cs
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Diagnostics;
using System.Numerics;

public class ReplaceTextWithTMP : EditorWindow
{
    [MenuItem("Tools/Replace UI Text With TextMeshPro")]
    public static void ReplaceAllUIText()
    {
        int replacedCount = 0;

        // 모든 Text 컴포넌트 찾기
        Text[] texts = GameObject.FindObjectsOfType<Text>(true);

        foreach (Text oldText in texts)
        {
            // Canvas 안에 있어야 함
            if (oldText.GetComponentInParent<Canvas>() == null)
                continue;

            GameObject go = oldText.gameObject;

            // 백업용 이름 변경
            string originalName = go.name;
            go.name = originalName + "_OldText";

            // 기존 Text 속성 저장
            string content = oldText.text;
            Color color = oldText.color;
            FontStyle fontStyle = oldText.fontStyle;
            int fontSize = oldText.fontSize;
            TextAnchor alignment = oldText.alignment;
            bool raycastTarget = oldText.raycastTarget;

            // 위치 및 정렬 정보
            RectTransform rectTransform = go.GetComponent<RectTransform>();
            UnityEngine.Vector2 anchoredPos = rectTransform.anchoredPosition;
            UnityEngine.Vector2 sizeDelta = rectTransform.sizeDelta;
            UnityEngine.Vector2 pivot = rectTransform.pivot;

            // 새 TMP 오브젝트 생성
            GameObject tmpGO = new GameObject(originalName + "_TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            tmpGO.transform.SetParent(go.transform.parent);
            tmpGO.transform.SetSiblingIndex(go.transform.GetSiblingIndex());

            // RectTransform 정보 복사
            RectTransform tmpRect = tmpGO.GetComponent<RectTransform>();
            tmpRect.localPosition = rectTransform.localPosition;
            tmpRect.localRotation = rectTransform.localRotation;
            tmpRect.localScale = rectTransform.localScale;
            tmpRect.anchoredPosition = anchoredPos;
            tmpRect.sizeDelta = sizeDelta;
            tmpRect.pivot = pivot;

            // TMP 설정 복사
            TextMeshProUGUI tmp = tmpGO.GetComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.color = color;
            tmp.fontSize = fontSize;
            tmp.raycastTarget = raycastTarget;

            // 정렬 변환
            tmp.alignment = ConvertAlignment(alignment);

            // 스타일 적용 (간단히 변환)
            if (fontStyle == FontStyle.Bold)
                tmp.fontStyle = FontStyles.Bold;
            else if (fontStyle == FontStyle.Italic)
                tmp.fontStyle = FontStyles.Italic;

            // 원본 Text 제거
            DestroyImmediate(oldText);

            replacedCount++;
        }

        UnityEngine.Debug.Log($"✅ Text → TMP 변환 완료: {replacedCount}개 변환됨.");
    }

    // UnityEngine.TextAnchor → TMPro.TextAlignmentOptions 매핑 함수
    private static TextAlignmentOptions ConvertAlignment(TextAnchor anchor)
    {
        return anchor switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
            TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
            TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.Center
        };
    }
}