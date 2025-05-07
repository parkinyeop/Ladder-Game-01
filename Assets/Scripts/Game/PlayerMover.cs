using System.Collections;
using UnityEngine;
using System;

/// <summary>
/// PlayerMover
/// - 사다리 맵을 따라 플레이어 UI를 좌우/하강 이동시키는 클래스
/// - ladderMap 정보를 LadderManager를 통해 조회하고, RectTransform 기반 UI 이동을 수행
/// </summary>
public class PlayerMover
{
    private LadderManager ladderManager;         // 사다리 정보 접근용 참조
    private Transform playerTransform;           // 이동 대상 (UI)
    private Coroutine moveCoroutine;             // 현재 실행 중인 이동 코루틴
    private int startIndex = 0;                  // 시작 세로줄 인덱스
    private float moveSpeed = 2f;                // 이동 속도
    private bool isMoving = false;               // 현재 이동 중 여부

    private Action<int> onFinishMove;            // 이동 완료 후 콜백 (도착한 세로줄 인덱스 전달)
    private const float ladderWidth = 800f;      // 사다리 전체 너비 (위치 기준 일치화)

    public PlayerMover(LadderManager manager)
    {
        this.ladderManager = manager;
    }

    /// <summary>
    /// 이동 대상 설정 및 시작 인덱스, 속도 초기화
    /// </summary>
    public void Setup(Transform player, int startIndex, float speed)
    {
        this.playerTransform = player;
        this.startIndex = startIndex;
        this.moveSpeed = speed;
    }

    /// <summary>
    /// 도착 후 호출될 콜백 함수 등록
    /// </summary>
    public void SetFinishCallback(Action<int> callback)
    {
        onFinishMove = callback;
    }

    /// <summary>
    /// MonoBehaviour 기반에서 코루틴을 실행하여 이동 시작
    /// </summary>
    public void StartMove(MonoBehaviour owner)
    {
        if (isMoving || playerTransform == null || owner == null)
            return;

        moveCoroutine = owner.StartCoroutine(MoveAlongLadder());
    }

    /// <summary>
    /// 사다리 경로를 따라 플레이어를 이동시키는 메인 코루틴
    /// 1. 최초 Y축 아래로 이동 시작
    /// 2. 각 층마다 가로줄 존재 여부에 따라 좌/우 이동
    /// 3. 최종적으로 골 버튼이 있는 바닥까지 정확히 도착
    /// </summary>
    private IEnumerator MoveAlongLadder()
    {
        isMoving = true;
        bool moved = false;

        int currentX = startIndex;                           // 현재 세로줄 위치
        int stepCount = ladderManager.stepCount;             // 전체 층 수
        float stepHeight = ladderManager.stepHeight;         // 한 칸 높이

        // RectTransform으로 UI 위치 이동
        RectTransform rectTransform = playerTransform.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("[PlayerMover] RectTransform이 없습니다.");
            yield break;
        }

        // 세로줄 기준 정확한 Y 시작 위치 계산
        RectTransform verticalLine = ladderManager.GetVerticalLineAt(currentX);
        float startY = LadderLayoutHelper.GetVisualStartY(verticalLine);

        // 플레이어 최초 위치 설정 (X: 선택된 세로줄, Y: 최상단)
        rectTransform.anchoredPosition = new Vector2(
            LadderLayoutHelper.GetXPosition(currentX, ladderManager.ladderWidth, ladderManager.verticalCount),
            startY
        );

        // 🔁 각 층마다 내려가며 가로줄 탐색 및 이동
        for (int y = 0; y < stepCount; y++)
        {
            // 1. 해당 층의 Y 위치로 먼저 수직 이동
            float yPos = LadderLayoutHelper.GetYPosition(y, stepCount, stepHeight);
            yield return MoveTo(new Vector2(
                LadderLayoutHelper.GetXPosition(currentX, ladderManager.ladderWidth, ladderManager.verticalCount),
                yPos
            ));

            // 2. 가로줄 존재 시 좌/우로 이동 (Y 위치는 유지)
            if (currentX < ladderManager.verticalCount - 1 && ladderManager.HasHorizontalLine(y, currentX))
            {
                currentX++; // 우측 이동
                yield return MoveTo(new Vector2(
                    LadderLayoutHelper.GetXPosition(currentX, ladderManager.ladderWidth, ladderManager.verticalCount),
                    yPos
                ));
                moved = true;
            }

            if (!moved && currentX > 0 && ladderManager.HasHorizontalLine(y, currentX - 1))
            {
                currentX--; // 좌측 이동
                yield return MoveTo(new Vector2(
                    LadderLayoutHelper.GetXPosition(currentX, ladderManager.ladderWidth, ladderManager.verticalCount),
                    yPos
                ));
            }
        }

        // ✅ 마지막 도착 지점으로 정확히 한 칸 더 하강 (최종 골 위치 보정)
        float finalY = LadderLayoutHelper.GetYPosition(stepCount, stepCount, stepHeight);
        yield return MoveTo(new Vector2(
            LadderLayoutHelper.GetXPosition(currentX, ladderManager.ladderWidth, ladderManager.verticalCount),
            finalY
        ));

        isMoving = false;
        onFinishMove?.Invoke(currentX);
    }

    /// <summary>
    /// 주어진 목표 위치로 anchoredPosition 기준 부드럽게 이동
    /// </summary>
    private IEnumerator MoveTo(Vector2 target)
    {
        if (playerTransform == null)
            yield break;

        RectTransform rectTransform = playerTransform.GetComponent<RectTransform>();

        // rectTransform이 이미 파괴된 경우 즉시 종료
        if (rectTransform == null)
            yield break;

        while (true)
        {
            // 이 지점에서도 계속 살아있는지 확인
            if (playerTransform == null || rectTransform == null)
                yield break;

            // 목표 위치에 도달했으면 종료
            if (Vector2.Distance(rectTransform.anchoredPosition, target) <= 0.01f)
                break;

            rectTransform.anchoredPosition = Vector2.MoveTowards(
                rectTransform.anchoredPosition,
                target,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        // 마지막 위치 보정
        if (rectTransform != null)
            rectTransform.anchoredPosition = target;
    }

    /// <summary>
    /// 현재 이동 중인지 여부 반환
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }

    // PlayerMover.cs 내부
    public void StopMove(MonoBehaviour owner)
    {
        if (moveCoroutine != null)
        {
            owner.StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        isMoving = false;
    }
}
