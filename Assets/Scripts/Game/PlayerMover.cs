using System.Collections;
using UnityEngine;

/// <summary>
/// PlayerMover
/// - 사다리 이동만 전문적으로 담당하는 순수 클래스
/// - LadderManager를 통해 사다리 구조를 읽고
/// - 주어진 Transform(플레이어)을 이동시킴
/// </summary>
public class PlayerMover
{
    private LadderManager ladderManager;   // 연결된 사다리 매니저
    private Transform playerTransform;     // 실제 이동시킬 플레이어 Transform
    private Coroutine moveCoroutine;       // 현재 이동 중인 코루틴

    private int startIndex = 0;             // 시작할 세로줄 인덱스
    private float moveSpeed = 2f;           // 이동 속도

    private bool isMoving = false;          // 이동 중 플래그

    // 생성자
    public PlayerMover(LadderManager manager)
    {
        this.ladderManager = manager;
    }

    /// <summary>
    /// 플레이어 오브젝트를 지정하고
    /// 이동 속도 및 시작 위치를 초기화하는 함수
    /// </summary>
    public void Setup(Transform player, int startIndex, float speed)
    {
        this.playerTransform = player;
        this.startIndex = startIndex;
        this.moveSpeed = speed;
    }

    /// <summary>
    /// 사다리를 따라 이동을 시작하는 함수
    /// 반드시 Setup() 이후에 호출해야 함
    /// </summary>
    public void StartMove(MonoBehaviour owner)
    {
        if (isMoving || playerTransform == null || owner == null)
            return;

        moveCoroutine = owner.StartCoroutine(MoveAlongLadder());
    }

    /// <summary>
    /// 이동 코루틴을 정지시키는 함수
    /// </summary>
    public void StopMove(MonoBehaviour owner)
    {
        if (moveCoroutine != null)
        {
            owner.StopCoroutine(moveCoroutine);
            moveCoroutine = null;
            isMoving = false;
        }
    }

    /// <summary>
    /// 플레이어가 사다리의 세로줄을 따라 아래로 내려가며,
    /// 가로줄이 있는 경우 좌우로 이동하는 메인 코루틴
    /// </summary>
    private IEnumerator MoveAlongLadder()
    {
        isMoving = true; // 이동 시작 플래그 설정

        // 현재 세로줄 인덱스 (시작 위치)
        int currentX = startIndex;

        // 사다리 정보 가져오기
        int stepCount = ladderManager.stepCount;              // 전체 층 수
        float spacing = ladderManager.verticalSpacing;        // 세로줄 간격
        float height = ladderManager.stepHeight;              // 층 간 높이

        // 좌표 계산용 기본값 설정
        float yStart = ((stepCount - 1) * height) / 2f;        // 맨 위 Y좌표
        float xOffset = -((ladderManager.verticalCount - 1) * spacing) / 2f; // 중앙 정렬용 X 오프셋

        // 플레이어 초기 위치 설정 (현재 세로줄의 맨 위)
        playerTransform.position = new Vector3(currentX * spacing + xOffset, yStart + (height / 2f), 0f);

        // 사다리 층을 하나씩 내려가며 이동
        for (int y = 0; y < stepCount; y++)
        {
            float currentY = yStart - y * height; // 현재 층의 Y좌표 계산

            // ▼ 1. 세로로 한 칸 아래 이동
            yield return MoveTo(new Vector3(currentX * spacing + xOffset, currentY, 0f));

            // ▼ 2. 가로줄 이동 체크 (왼쪽 우선)
            if (currentX > 0 && ladderManager.HasHorizontalLine(y, currentX - 1))
            {
                // 왼쪽으로 이동
                currentX--;
                yield return MoveTo(new Vector3(currentX * spacing + xOffset, currentY, 0f));
            }
            else if (currentX < ladderManager.verticalCount - 1 && ladderManager.HasHorizontalLine(y, currentX))
            {
                // 오른쪽으로 이동
                currentX++;
                yield return MoveTo(new Vector3(currentX * spacing + xOffset, currentY, 0f));
            }
        }

        isMoving = false; // 이동 종료 플래그
        Debug.Log($"플레이어 도착 인덱스: {currentX}"); // 최종 도착 세로줄 인덱스 출력
    }

    /// <summary>
    /// 목표 위치로 부드럽게 이동하는 내부 코루틴
    /// </summary>
    private IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(playerTransform.position, target) > 0.01f)
        {
            playerTransform.position = Vector3.MoveTowards(playerTransform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        playerTransform.position = target;
    }
}