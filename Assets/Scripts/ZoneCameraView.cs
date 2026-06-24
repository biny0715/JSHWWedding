// ZoneCameraView.cs
// 특정 지점(예: 방명록 존)에 플레이어가 cameraRadius(수평) 안으로 들어오면 카메라 시점을 살짝 바꾼다.
// 'system'만 제공 — 실제 시점값(추가 FollowOffset/FOV)은 인스펙터에서 추후 조정.
// 카메라 적용은 CameraMoveZoom 이 이 컴포넌트들을 읽어 이동줌과 합성한다(단일 제어자 → 충돌 없음).
//
// 사용: 방명록 존(FlowerDecoZone) 등 원하는 오브젝트에 이 컴포넌트를 추가.
//   - cameraRadius 는 버튼이 뜨는 InteractionZone.activateRadius 보다 "크게"(더 멀리서부터 시점 변경).
using System.Collections.Generic;
using UnityEngine;

namespace JSHWWedding
{
    public class ZoneCameraView : MonoBehaviour
    {
        [Tooltip("이 반경(수평, m) 안에 플레이어가 오면 시점 변경 시작. 버튼 반경(InteractionZone.activateRadius)보다 크게.")]
        public float cameraRadius = 12f;
        [Tooltip("활성 시 카메라 FollowOffset(월드)에 더할 값. 추후 조정.")]
        public Vector3 extraFollowOffset = Vector3.zero;
        [Tooltip("활성 시 FOV(도)에 더할 값. 추후 조정.")]
        public float extraFov = 0f;
        [Tooltip("활성 시 타겟의 '화면상 위치'를 이만큼 이동(RotationComposer). y 음수 = 타겟이 화면 아래로(위쪽 배경 더 보임). 보통 -0.05~-0.2.")]
        public Vector2 extraScreenPosition = Vector2.zero;
        [Tooltip("들어가고 나갈 때 전환 부드러움(초). 작을수록 빠름.")]
        public float blendTime = 0.6f;

        [Tooltip("반경의 '중심'을 오브젝트 위치에서 이만큼 이동(월드). FlowerDecoZone origin이 시각적 중심과 다를 때 맞추는 용도.")]
        public Vector3 centerOffset = Vector3.zero;

        // 반경 판정/기즈모의 실제 중심
        public Vector3 Center => transform.position + centerOffset;

        // CameraMoveZoom 이 매 프레임 순회한다(활성 존 탐색). 등록/해제로 관리.
        public static readonly List<ZoneCameraView> All = new List<ZoneCameraView>();
        void OnEnable()  { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public float HorizDistance(Vector3 p)
        {
            Vector3 c = Center;
            float dx = c.x - p.x;
            float dz = c.z - p.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // 실제 트리거는 수평거리(XZ) 기준이므로 기즈모도 Center 에서 수평 원으로 그린다.
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.36f, 0.78f, 0.85f, 0.8f);
            Vector3 c = Center;
            const int seg = 48;
            Vector3 prev = c + new Vector3(cameraRadius, 0f, 0f);
            for (int i = 1; i <= seg; i++)
            {
                float a = (i / (float)seg) * Mathf.PI * 2f;
                Vector3 next = c + new Vector3(Mathf.Cos(a) * cameraRadius, 0f, Mathf.Sin(a) * cameraRadius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
            Gizmos.DrawLine(c + Vector3.up * 0.6f, c - Vector3.up * 0.6f);   // 중심 표시
        }
    }
}
