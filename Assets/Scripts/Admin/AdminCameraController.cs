// AdminCameraController.cs
// 관리자 뷰 카메라 — 드론/CCTV 처럼 예식장 위를 자유롭게 이동.
//  아이패드: 한 손가락 드래그 = 이동, 두 손가락 벌리기/모으기 = 줌, 두 손가락 돌리기 = 회전, 두 손가락 위아래 = 기울기
//  에디터: 왼쪽 드래그 = 이동, 오른쪽 드래그 = 회전/기울기, 휠 = 줌
//  하객을 선택하면(Follow) 그 하객을 따라가고, 화면을 끌면 따라가기가 풀린다.
using UnityEngine;

namespace JSHWWedding.Admin
{
    [RequireComponent(typeof(Camera))]
    public class AdminCameraController : MonoBehaviour
    {
        [Header("처음 보기 — 하객 스폰 지역을 가까이")]
        public Vector3 homePivot;
        public float homeDistance = 22f;
        public float homeYaw = 45f;      // 하객 카메라(FollowOffset -5.94,4,-5.94)와 같은 방향에서 바라봄
        public float homePitch = 45f;

        [Header("제한")]
        public float minDistance = 5f;
        public float maxDistance = 60f;
        public float minPitch = 20f;
        public float maxPitch = 89f;
        public float boundsRadius = 80f;    // homePivot(스폰) 기준 이동 가능 반경(m) — 섬 전체가 아니라 예식 공간 위주

        [Header("감도")]
        public float panSpeed = 1.2f;
        public float rotateSpeed = 0.25f;
        public float followSharpness = 5f;

        Vector3 pivot;
        float distance, yaw, pitch;
        Transform follow;
        Vector2 lastMouse;

        public Transform Following => follow;

        void Start() => ResetView();

        public void ResetView()
        {
            follow = null;
            pivot = homePivot; distance = homeDistance; yaw = homeYaw; pitch = homePitch;
            Apply();
        }

        public void SetTopDown(bool topDown) => pitch = topDown ? maxPitch : homePitch;
        public bool IsTopDown => pitch > maxPitch - 1f;

        public void Follow(Transform target)
        {
            follow = target;
            if (target != null) distance = Mathf.Min(distance, 14f);   // 따라갈 땐 더 가까이
        }

        void LateUpdate()
        {
            if (Input.touchCount > 0) HandleTouch();
            else HandleMouse();

            if (follow != null)
                pivot = Vector3.Lerp(pivot, follow.position, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));

            Apply();
        }

        void HandleTouch()
        {
            if (Input.touchCount == 1)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Moved && !AdminOverlay.IsOverUI(t.position - t.deltaPosition))
                    Pan(t.deltaPosition);
            }
            else if (Input.touchCount >= 2)
            {
                var a = Input.GetTouch(0); var b = Input.GetTouch(1);
                if (AdminOverlay.IsOverUI(a.position) || AdminOverlay.IsOverUI(b.position)) return;
                Vector2 pa = a.position - a.deltaPosition, pb = b.position - b.deltaPosition;

                float prev = (pa - pb).magnitude, cur = (a.position - b.position).magnitude;
                if (prev > 1f && cur > 1f) distance *= prev / cur;

                yaw -= Vector2.SignedAngle(pa - pb, a.position - b.position);

                float midDy = ((a.deltaPosition + b.deltaPosition) * 0.5f).y;
                pitch -= midDy * rotateSpeed;
            }
        }

        void HandleMouse()
        {
            Vector2 m = Input.mousePosition;
            Vector2 d = m - lastMouse;
            lastMouse = m;
            if (AdminOverlay.IsOverUI(m)) return;

            if (Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0)) Pan(d);
            if (Input.GetMouseButton(1) && !Input.GetMouseButtonDown(1))
            {
                yaw += d.x * rotateSpeed;
                pitch -= d.y * rotateSpeed;
            }
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f) distance *= Mathf.Pow(0.9f, wheel);
        }

        void Pan(Vector2 screenDelta)
        {
            if (screenDelta.sqrMagnitude < 0.01f) return;
            follow = null;   // 직접 움직이면 따라가기 해제
            float worldPerPixel = 2f * distance * Mathf.Tan(GetComponent<Camera>().fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;
            var flatRot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 right = flatRot * Vector3.right, fwd = flatRot * Vector3.forward;
            pivot -= (right * screenDelta.x + fwd * screenDelta.y) * worldPerPixel * panSpeed;
        }

        void Apply()
        {
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            Vector3 off = pivot - homePivot; off.y = 0f;
            if (off.magnitude > boundsRadius) pivot = homePivot + off.normalized * boundsRadius + Vector3.up * (pivot.y - homePivot.y);

            var rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(pivot - rot * Vector3.forward * distance, rot);
        }
    }
}
