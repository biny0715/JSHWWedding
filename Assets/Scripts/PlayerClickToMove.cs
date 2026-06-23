using UnityEngine;
using Photon.Pun;
using UnityEngine.AI;

namespace Photon.Pun.Demo.PunBasics
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerClickToMove : MonoBehaviourPun
    {
        [Header("Raycast")]
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Header("이동(꾹 누르기)")]
        [Tooltip("누른 지점 방향으로 향할 목표 거리(m). 클수록 한 번에 멀리 걷는 느낌")]
        [SerializeField] private float holdAheadDistance = 3.5f;
        [Tooltip("이 거리(m) 안을 누르면 무시(캐릭터 바로 위를 눌렀을 때 떨림 방지)")]
        [SerializeField] private float holdDeadzone = 0.4f;

        private enum PointerState { None, Held, Released }

        private NavMeshAgent agent;
        private Camera cam;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        void Start()
        {
            cam = Camera.main;

            if (!photonView.IsMine)
            {
                agent.enabled = false;
                return;
            }

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogError("NavMesh 위에 Player가 없음!");
            }
        }

        void Update()
        {
            if (!photonView.IsMine)
                return;

#if UNITY_EDITOR
            // 에디터 테스트: 웹 닫힘 콜백이 없으니 Esc 로 잠금 해제
            if (UIInputLock.Locked && Input.GetKeyDown(KeyCode.Escape)) UIInputLock.Locked = false;
#endif
            // 방명록/사진앨범 등 웹 창이 열려있으면 이동 금지 + 진행 중 경로 정지
            if (UIInputLock.Locked)
            {
                if (agent.enabled && agent.hasPath) agent.ResetPath();
                return;
            }

            // 꾹 누르고 있는 동안: 누른 지점 방향으로 계속 이동. 손을 떼면 멈춘다.
            PointerState state = GetPointer(out Vector2 screenPos);
            if (state == PointerState.Held)
            {
                Ray ray = cam.ScreenPointToRay(screenPos);

                // 건물 조각 MeshCollider(가림 fade 판정용)는 Default(0) 레이어에 있음.
                // 클릭 이동은 그것들을 통과해 terrain(Ground)만 맞도록 Default 를 제외 → 오목 건물 안뜰도 타겟 가능.
                int clickMask = groundLayerMask & ~(1 << 0);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickMask))
                {
                    // 누른 지점의 (수평) 방향으로 일정 거리 앞을 목표로 → 누르고 있는 동안 계속 그 방향으로 걷는다.
                    Vector3 dir = hit.point - transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > holdDeadzone * holdDeadzone)
                    {
                        dir.Normalize();
                        Vector3 ahead = transform.position + dir * holdAheadDistance;
                        if (NavMesh.SamplePosition(ahead, out NavMeshHit nav, 2f, NavMesh.AllAreas))
                            agent.SetDestination(nav.position);
                        else
                            agent.SetDestination(hit.point);   // 앞쪽이 NavMesh 밖이면 누른 지점 자체로
                    }
                }
            }
            else if (state == PointerState.Released)
            {
                if (agent.enabled && agent.hasPath) agent.ResetPath();   // 손을 떼면 즉시 멈춤
            }
        }

        // 현재 포인터(터치/마우스) 상태와 화면 좌표를 반환.
        private PointerState GetPointer(out Vector2 screenPos)
        {
            screenPos = default;

            // 모바일: 한 손가락 기준
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                screenPos = t.position;
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    return PointerState.Released;
                return PointerState.Held;   // Began / Moved / Stationary
            }

            // 데스크탑(마우스)
            if (Input.GetMouseButton(0))
            {
                screenPos = Input.mousePosition;
                return PointerState.Held;
            }
            if (Input.GetMouseButtonUp(0))
            {
                screenPos = Input.mousePosition;
                return PointerState.Released;
            }

            return PointerState.None;
        }
    }
}