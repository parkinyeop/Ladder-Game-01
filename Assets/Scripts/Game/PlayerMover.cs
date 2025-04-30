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
    /// - 좌/우 판단 → 해당 위치로 이동 → 아래로 한 칸 하강 반복
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

        // 정확한 초기 위치를 step 0 기준으로 계산
        float currentY = LadderLayoutHelper.GetYPosition(0, stepCount, stepHeight);

        // 정확한 초기 위치로 플레이어 설정
        rectTransform.anchoredPosition = new Vector2(
            LadderLayoutHelper.GetXPosition(currentX, ladderWidth, ladderManager.verticalCount),
            currentY
        );

        for (int y = 0; y < stepCount; y++)
        {
            float yPos = LadderLayoutHelper.GetYPosition(y, stepCount, stepHeight);

            // 오른쪽 가로줄 존재 시 우측 이동
            if (currentX < ladderManager.verticalCount - 1 && ladderManager.HasHorizontalLine(y, currentX))
            {
                currentX++;
                yield return MoveTo(
                    new Vector2(
                        LadderLayoutHelper.GetXPosition(currentX, ladderWidth, ladderManager.verticalCount),
                        yPos
                    )
                );
            }
            // 왼쪽 가로줄 존재 시 좌측 이동
            else if (currentX > 0 && ladderManager.HasHorizontalLine(y, currentX - 1))
            {
                currentX--;
                yield return MoveTo(
                    new Vector2(
                        LadderLayoutHelper.GetXPosition(currentX, ladderWidth, ladderManager.verticalCount),
                        yPos
                    )
                );
            }

            // 아래 한 칸 내려감
            float nextY = yPos - stepHeight;
            yield return MoveTo(
                new Vector2(
                    LadderLayoutHelper.GetXPosition(currentX, ladderWidth, ladderManager.verticalCount),
                    nextY
                )
            );
        }

        isMoving = false;
        onFinishMove?.Invoke(currentX);
    }

    /// <summary>
    /// 목표 위치로 anchoredPosition을 사용해 부드럽게 이동
    /// </summary>
    private IEnumerator MoveTo(Vector2 target)
    {
        RectTransform rectTransform = playerTransform.GetComponent<RectTransform>();
        if (rectTransform == null)
            yield break;

        while (Vector2.Distance(rectTransform.anchoredPosition, target) > 0.01f)
        {
            rectTransform.anchoredPosition = Vector2.MoveTowards(
                rectTransform.anchoredPosition,
                target,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        rectTransform.anchoredPosition = target;
    }

    /// <summary>
    /// 현재 이동 중인지 여부 반환
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }
}
