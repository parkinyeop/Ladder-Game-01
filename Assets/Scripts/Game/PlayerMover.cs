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

        // ✅ 디버깅: ladderMap 상태 출력
        var ladderMap = ladderManager.ladderGenerator.GetLadderMap();
        LadderDebugHelper.LogLadderMapReference(ladderMap, "PlayerMover.Setup");
        LadderDebugHelper.LogLadderMap(ladderMap, "PlayerMover.Setup");
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
    /// 사다리 맵을 따라 플레이어를 이동시키는 코루틴
    /// - 플레이어는 수직으로 내려가며 가로줄을 만나면 좌/우로 이동
    /// - 마지막엔 한 칸 더 내려가서 정확한 골 버튼 위치에 도착
    /// </summary>
    private IEnumerator MoveAlongLadder()
    {
        isMoving = true;

        int currentX = startIndex;
        int stepCount = ladderManager.stepCount;
        float stepHeight = ladderManager.stepHeight;
        RectTransform rectTransform = playerTransform.GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            Debug.LogError("[PlayerMover] RectTransform이 없습니다.");
            yield break;
        }

        float startY = LadderLayoutHelper.GetYPosition(0, stepCount, stepHeight);

        // ▶ 처음 위치 설정 (시작 위치)
        rectTransform.anchoredPosition = new Vector2(
            LadderLayoutHelper.GetXPosition(currentX, ladderManager.ladderWidth, ladderManager.verticalCount),
            startY
        );

        // 🔁 각 Y 층마다 수직→가로줄 검사→수평 이동
        for (int y = 0; y < stepCount; y++)
        {
            float yPos = LadderLayoutHelper.GetYPosition(y, stepCount, stepHeight);
            float xPos = LadderLayoutHelper.GetXPosition(currentX, ladderManager.ladderWidth, ladderManager.verticalCount);

            // ▶ 1. 수직 이동 먼저
            yield return MoveTo(new Vector2(xPos, yPos));

            // ▶ 2. 수평 이동 판단 (오른쪽 먼저)
            if (currentX < ladderManager.verticalCount - 1 && ladderManager.HasHorizontalLine(y, currentX))
            {
                currentX++; // 오른쪽으로 이동
                float newX = LadderLayoutHelper.GetXPosition(currentX, ladderManager.ladderWidth, ladderManager.verticalCount);
                yield return MoveTo(new Vector2(newX, yPos));
            }
            else if (currentX > 0 && ladderManager.HasHorizontalLine(y, currentX - 1))
            {
                currentX--; // 왼쪽으로 이동
                float newX = LadderLayoutHelper.GetXPosition(currentX, ladderManager.ladderWidth, ladderManager.verticalCount);
                yield return MoveTo(new Vector2(newX, yPos));
            }

            // (디버그용 로그)
            // Debug.Log($"📍 위치: X={currentX}, Y={y}");
        }

        // ▶ 마지막 바닥 위치로 한 칸 더 이동 (골 위치)
        float finalY = LadderLayoutHelper.GetYPosition(stepCount, stepCount, stepHeight);
        float finalX = LadderLayoutHelper.GetXPosition(currentX, ladderManager.ladderWidth, ladderManager.verticalCount);
        yield return MoveTo(new Vector2(finalX, finalY));

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

        while (Vector2.Distance(rectTransform.anchoredPosition, target) > 0.01f)
        {
            rectTransform.anchoredPosition = Vector2.MoveTowards(
                rectTransform.anchoredPosition, target, moveSpeed * Time.deltaTime
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
