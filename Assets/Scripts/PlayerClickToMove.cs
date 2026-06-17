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

            if (TryGetPointerDown(out Vector2 screenPos))
            {
                Ray ray = cam.ScreenPointToRay(screenPos);

                // 건물 조각 MeshCollider(가림 fade 판정용)는 Default(0) 레이어에 있음.
                // 클릭 이동은 그것들을 통과해 terrain(Ground)만 맞도록 Default 를 제외 → 오목 건물 안뜰도 타겟 가능.
                int clickMask = groundLayerMask & ~(1 << 0);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickMask))
                {
                    agent.SetDestination(hit.point);
                }
            }
        }

        private bool TryGetPointerDown(out Vector2 screenPos)
        {
            // 모바일
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began)
                {
                    screenPos = t.position;
                    return true;
                }
            }

            // 데스크탑
            if (Input.GetMouseButtonDown(0))
            {
                screenPos = Input.mousePosition;
                return true;
            }

            screenPos = default;
            return false;
        }
    }
}