// CameraMoveZoom.cs
// 따라오는 카메라 연출: 가만히 있을 땐 원래 상태, 이동을 시작하면 카메라가 살짝 점점 멀어지고
// (시야가 넓어짐), 이동 중엔 멀어진 상태 유지, 멈추면 서서히 원래 거리로 돌아온다.
// 각도(45°)는 그대로 두고 FollowOffset 크기만 키워 "뒤로 빠지는" 느낌 + 약간의 FOV 보강.
// CinemachineCamera(Body = CinemachineFollow, WorldSpace 오프셋)에 붙인다.
using UnityEngine;
using UnityEngine.AI;
using Unity.Cinemachine;

namespace JSHWWedding
{
    [RequireComponent(typeof(CinemachineCamera))]
    [RequireComponent(typeof(CinemachineFollow))]
    public class CameraMoveZoom : MonoBehaviour
    {
        [Header("이동 중 더 멀어지는 정도")]
        [Tooltip("FollowOffset 추가 배율 (0.15 = 15% 더 멀리)")]
        [Range(0f, 1f)] public float zoomAmount = 0.15f;
        [Tooltip("이동 중 추가할 시야각 FOV(도). 0이면 거리만으로")]
        public float fovBoost = 3f;

        [Header("반응(초)")]
        [Tooltip("이동 시작 → 멀어지는 데 걸리는 시간감")]
        public float extendTime = 0.45f;
        [Tooltip("멈춤 → 원래 거리로 돌아오는 시간감")]
        public float returnTime = 0.8f;
        [Tooltip("이 속도(m/s) 이상이면 '이동 중'으로 판단")]
        public float moveThreshold = 0.2f;

        CinemachineCamera vcam;
        CinemachineFollow follow;
        Vector3 baseOffset;
        float baseFov;
        NavMeshAgent agent;
        float zoom, zoomVel;

        void Awake()
        {
            vcam = GetComponent<CinemachineCamera>();
            follow = GetComponent<CinemachineFollow>();
            baseOffset = follow.FollowOffset;   // 원래(가만히 있을 때) 오프셋
            baseFov = vcam.Lens.FieldOfView;
        }

        void Update()
        {
            if (agent == null) AcquireAgent();   // 추격 대상(로컬 플레이어)이 스폰되면 잡는다

            bool moving = agent != null && agent.velocity.sqrMagnitude > moveThreshold * moveThreshold;

            // 이동=1 / 정지=0 로 부드럽게 보간 (시작/복귀 시간 분리)
            float target = moving ? 1f : 0f;
            zoom = Mathf.SmoothDamp(zoom, target, ref zoomVel, moving ? extendTime : returnTime);

            // 거리(각도 유지) + 약간의 FOV 로 시야를 넓힌다
            follow.FollowOffset = baseOffset * (1f + zoom * zoomAmount);
            if (fovBoost != 0f)
            {
                LensSettings lens = vcam.Lens;
                lens.FieldOfView = baseFov + zoom * fovBoost;
                vcam.Lens = lens;
            }
        }

        void AcquireAgent()
        {
            Transform f = vcam.Follow;
            if (f == null) return;
            agent = f.GetComponentInParent<NavMeshAgent>();
            if (agent == null) agent = f.GetComponentInChildren<NavMeshAgent>();
        }
    }
}
