using UnityEngine;

public class CameraFitter : MonoBehaviour
{
    public LadderManager ladderManager;
    public Camera mainCamera;
    public float targetAspectRatio = 1920f / 1080f; // 목표 화면 비율
    public float horizontalMarginPercent = 0.1f; // 가로 여백 비율 (양쪽 5%씩)
    public float verticalMarginPercent = 0.05f; // 세로 여백 비율 (위아래 2.5%씩)

    private void Start()
    {
        FitCamera();
    }

    public void FitCamera()
    {
        int minVerticalCount = 2;
        int maxVerticalCount = 5;
        int stepCount = ladderManager.stepCount;
        float verticalSpacing = ladderManager.verticalSpacing;
        float stepHeight = ladderManager.stepHeight;

        // 최대 세로줄 개수일 때의 사다리 전체 가로 폭 계산
        float maxTotalWidth = (maxVerticalCount - 1) * verticalSpacing;
        // 사다리의 전체 세로 높이 계산
        float totalHeight = (stepCount - 1) * stepHeight;

        // 목표 화면 가로 폭에 대한 사다리 최대 가로 폭의 비율
        float widthRatio = maxTotalWidth / 1920f;
        // 원하는 가로 시야 크기 (목표 화면 가로 폭 * 비율 * (1 + 여백 비율)) / 2
        float desiredHalfWidth = (1920f * widthRatio * (1 + horizontalMarginPercent * 2)) / 2f;

        // 목표 화면 세로 높이에 대한 사다리 전체 세로 높이의 비율
        float heightRatio = totalHeight / 1080f;
        // 필요한 세로 시야 크기 (목표 화면 세로 높이 * 비율 * (1 + 여백 비율)) / 2
        float desiredHalfHeight = (1080f * heightRatio * (1 + verticalMarginPercent * 2)) / 2f;

        // 가로 시야를 기준으로 초기 orthographicSize 설정
        mainCamera.orthographicSize = desiredHalfWidth / targetAspectRatio;

        // 세로 시야가 필요한 크기보다 작으면 orthographicSize 조정
        if (mainCamera.orthographicSize < desiredHalfHeight)
        {
            mainCamera.orthographicSize = desiredHalfHeight;
        }

        // 카메라 위치 설정 (중앙으로)
        mainCamera.transform.position = new Vector3(0f, 0f, -10f);
    }
}