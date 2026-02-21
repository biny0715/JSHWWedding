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
                agent.enabled = false;
        }

        void Update()
        {
            if (!photonView.IsMine)
                return;

            if (TryGetPointerDown(out Vector2 screenPos))
            {
                Ray ray = cam.ScreenPointToRay(screenPos);

                if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayerMask))
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