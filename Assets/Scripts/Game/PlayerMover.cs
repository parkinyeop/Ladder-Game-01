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
    /// 사다리 맵을 따라 플레이어를 이동시키는 코루틴
    /// - 플레이어는 수직으로 내려가며 가로줄을 만나면 좌/우로 이동
    /// - 마지막엔 한 칸 더 내려가서 정확한 골 버튼 위치에 도착
    /// </summary>
    private IEnumerator MoveAlongLadder()
    {
        // 1. 이동 중 상태 설정
        isMoving = true;

        // 2. 현재 세로줄 인덱스
        int currentX = startIndex;

        // 3. 사다리 전체 정보
        int stepCount = ladderManager.stepCount;
        float stepHeight = ladderManager.stepHeight;

        // 4. 플레이어 UI의 RectTransform 확보
        RectTransform rectTransform = playerTransform.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("[PlayerMover] RectTransform이 없습니다.");
            yield break;
        }

        // 5. 시작 Y 위치 (최상단)
        float startY = LadderLayoutHelper.GetYPosition(0, stepCount, stepHeight);

        // 6. 초기 위치 설정
        rectTransform.anchoredPosition = new Vector2(
            LadderLayoutHelper.GetXPosition(currentX, ladderManager.ladderWidth, ladderManager.verticalCount),
            startY
        );

        // 🔁 7. 각 층마다 아래로 이동하며 경로 탐색
        for (int y = 0; y < stepCount; y++)
        {
            // 7-1. 현재 층의 Y 위치 계산
            float yPos = LadderLayoutHelper.GetYPosition(y, stepCount, stepHeight);

            // 수직 이동
            yield return MoveTo(new Vector2(
                LadderLayoutHelper.GetXPosition(currentX, ladderManager.ladderWidth, ladderManager.verticalCount),
                yPos
            ));

            // 정확한 좌우 이동 방향으로 수정
            if (currentX < ladderManager.verticalCount - 1 && ladderManager.HasHorizontalLine(y, currentX))
            {
                // 현재 x 위치에서 오른쪽으로 가는 가로줄이 있는 경우
                currentX++; // 오른쪽으로 이동
                float xPos = LadderLayoutHelper.GetXPosition(currentX, ladderManager.ladderWidth, ladderManager.verticalCount);
                yield return MoveTo(new Vector2(xPos, yPos));
            }
            else if (currentX > 0 && ladderManager.HasHorizontalLine(y, currentX - 1))
            {
                // 왼쪽(x-1)에서 현재 위치로 오는 가로줄이 있는 경우
                currentX--; // 왼쪽으로 이동
                float xPos = LadderLayoutHelper.GetXPosition(currentX, ladderManager.ladderWidth, ladderManager.verticalCount);
                yield return MoveTo(new Vector2(xPos, yPos));
            }

            // 7-4. 현재 위치 출력 로그
            Debug.Log($"▶ 위치 이동 완료: X={currentX}, Y={yPos}");
        }

        // 8. 최종 바닥으로 한 칸 더 하강 (골 버튼 위치 보정)
        float finalY = LadderLayoutHelper.GetYPosition(stepCount, stepCount, stepHeight);
        yield return MoveTo(new Vector2(
            LadderLayoutHelper.GetXPosition(currentX, ladderManager.ladderWidth, ladderManager.verticalCount),
            finalY
        ));

        // 9. 완료 처리
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
