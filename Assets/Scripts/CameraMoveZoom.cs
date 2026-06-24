// CameraMoveZoom.cs
// 따라오는 카메라 연출: 가만히 있을 땐 원래 상태, 이동을 시작하면 카메라가 살짝 점점 멀어지고
// (시야가 넓어짐), 이동 중엔 멀어진 상태 유지, 멈추면 서서히 원래 거리로 돌아온다.
// 각도(45°)는 그대로 두고 FollowOffset 크기만 키워 "뒤로 빠지는" 느낌 + 약간의 FOV 보강.
// CinemachineCamera(Body = CinemachineFollow, WorldSpace 오프셋)에 붙인다.
// 추가: ZoneCameraView(존 근처 시점 변경)의 오프셋/FOV도 여기서 함께 합성한다(단일 카메라 제어자 → 충돌 없음).
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

        // 존 시점(ZoneCameraView) 합성용
        Vector3 zoneOffset, zoneOffsetVel;
        float zoneFov, zoneFovVel;
        float curBlend = 0.6f;
        CinemachineRotationComposer composer;   // 타겟 화면 위치(있을 때만)
        Vector2 baseScreenPos;
        Vector2 zoneScreen, zoneScreenVel;

        void Awake()
        {
            vcam = GetComponent<CinemachineCamera>();
            follow = GetComponent<CinemachineFollow>();
            baseOffset = follow.FollowOffset;   // 원래(가만히 있을 때) 오프셋
            baseFov = vcam.Lens.FieldOfView;
            composer = GetComponent<CinemachineRotationComposer>();
            if (composer != null) baseScreenPos = composer.Composition.ScreenPosition;
        }

        void Update()
        {
            if (agent == null) AcquireAgent();   // 추격 대상(로컬 플레이어)이 스폰되면 잡는다

            bool moving = agent != null && agent.velocity.sqrMagnitude > moveThreshold * moveThreshold;

            // 이동=1 / 정지=0 로 부드럽게 보간 (시작/복귀 시간 분리)
            float target = moving ? 1f : 0f;
            zoom = Mathf.SmoothDamp(zoom, target, ref zoomVel, moving ? extendTime : returnTime);

            // 활성 존 시점(근처에 들어오면) — 부드럽게 보간해 이동줌과 합성
            ZoneCameraView zone = FindActiveZone();
            if (zone != null) curBlend = Mathf.Max(0.01f, zone.blendTime);
            Vector3 zTargetOff    = zone != null ? zone.extraFollowOffset : Vector3.zero;
            float   zTargetFov    = zone != null ? zone.extraFov : 0f;
            Vector2 zTargetScreen = zone != null ? zone.extraScreenPosition : Vector2.zero;
            zoneOffset = Vector3.SmoothDamp(zoneOffset, zTargetOff, ref zoneOffsetVel, curBlend);
            zoneFov    = Mathf.SmoothDamp(zoneFov, zTargetFov, ref zoneFovVel, curBlend);
            zoneScreen = Vector2.SmoothDamp(zoneScreen, zTargetScreen, ref zoneScreenVel, curBlend);

            // 합성: 존 오프셋을 먼저 더해 "존 시점의 기준 오프셋"을 만든 뒤, 이동줌을 그 위에 곱한다.
            // (이동줌을 baseOffset 에만 곱하면, 더해진 zoneOffset 비중이 줄어 존 각도가 희석됨 → 순서 중요)
            follow.FollowOffset = (baseOffset + zoneOffset) * (1f + zoom * zoomAmount);
            float fov = baseFov + zoom * fovBoost + zoneFov;
            if (!Mathf.Approximately(fov, vcam.Lens.FieldOfView))
            {
                LensSettings lens = vcam.Lens;
                lens.FieldOfView = fov;
                vcam.Lens = lens;
            }
            if (composer != null)
            {
                var comp = composer.Composition;
                comp.ScreenPosition = baseScreenPos + zoneScreen;
                composer.Composition = comp;
            }
        }

        // 플레이어(=Follow 대상) 위치 기준, 반경 안에서 가장 가까운 ZoneCameraView. 없으면 null.
        ZoneCameraView FindActiveZone()
        {
            Transform f = vcam.Follow;
            if (f == null || ZoneCameraView.All.Count == 0) return null;
            Vector3 p = f.position;
            ZoneCameraView best = null;
            float bestD = float.MaxValue;
            foreach (var z in ZoneCameraView.All)
            {
                if (z == null) continue;
                float d = z.HorizDistance(p);
                if (d <= z.cameraRadius && d < bestD) { bestD = d; best = z; }
            }
            return best;
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
