using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 골 버튼 생성 후 Raycast가 작동하는지 자동 검사
/// </summary>
public class GoalButtonRaycastTester : MonoBehaviour
{
    public RectTransform destinationButtonsParent; // 골 버튼 부모
    private bool isReadyToTest = false;

    /// <summary>
    /// LadderManager가 GenerateLadder() 이후 호출함
    /// </summary>
    public void RunTestAfterGeneration()
    {
        isReadyToTest = true;
        StartCoroutine(DelayedRaycastTest());
    }

    private IEnumerator DelayedRaycastTest()
    {
        yield return new WaitForEndOfFrame(); // 다음 프레임까지 대기 (배치 안정화)
        Canvas.ForceUpdateCanvases();

        Button[] goalButtons = destinationButtonsParent.GetComponentsInChildren<Button>();
        if (goalButtons.Length == 0)
        {
            Debug.LogError("❌ 골 버튼 없음 - 테스트 실패");
            yield break;
        }

        Button mid = goalButtons[goalButtons.Length / 2];
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, mid.transform.position);

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        GraphicRaycaster raycaster = destinationButtonsParent.GetComponentInParent<Canvas>().GetComponent<GraphicRaycaster>();
        List<RaycastResult> results = new();
        raycaster.Raycast(pointerData, results);

        bool found = false;
        foreach (var result in results)
        {
            if (result.gameObject == mid.gameObject)
            {
                found = true;
                break;
            }
        }

        if (found)
            Debug.Log("✅ 중앙 골 버튼 정상 클릭 가능 (Raycast OK)");
        else
            Debug.LogWarning("❌ 중앙 골 버튼 Raycast 안됨 - 클릭 불가");
    }
}