// LobbyPreviewController.cs
// 로비 라이브 캐릭터 프리뷰. 자체 카메라 + RenderTexture + 전체화면 RawImage 를 런타임 생성한다(MinimapSystem 방식).
// 로비 씬과 격리하기 위해 캐릭터를 먼 위치 + 전용 레이어에 두고, 프리뷰 카메라는 그 레이어만 렌더.
// 웹(WebBridge)이 성별/부위 변경을 SendMessage 로 보내면 프리뷰를 갱신한다.
// 웹은 step2(꾸미기)에서 커튼을 투명하게 하면 이 전체화면 프리뷰가 드러난다.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace JSHWWedding.Customization
{
    public class LobbyPreviewController : MonoBehaviour
    {
        [Header("필수")]
        public GastroPartManifest manifest;

        [Header("프리뷰 렌더")]
        public Color backdrop = new Color(0.97f, 0.94f, 0.90f, 1f);
        public Vector3 farOrigin = new Vector3(5000f, 0f, 0f);  // 로비 씬과 격리
        [Range(8, 31)] public int previewLayer = 31;            // 프리뷰 전용 레이어
        public int rtWidth = 720, rtHeight = 1280;

        [Header("카메라 프레이밍")]
        // 캐릭터 키 ≈ 1.74m(헤어번 포함, 발 Y=-0.156~머리 Y=1.587). 하단 시트(기기별 ~38~45%)가
        // 신발을 가리지 않도록 줌아웃해서 전신을 프레임 ~48%(발)~92%(머리)에 배치(상하 여유).
        // 기기 화면 비와 무관하게 세로 매핑은 보존되므로 % 프레이밍은 견고. 인스펙터에서 미세조정 가능.
        public float camDistance = 7.4f;
        public float camHeight = 0.5f;
        public float lookHeight = -0.18f;   // 캐릭터를 프레임에서 살짝 위로(신발이 하단 시트에 가리지 않게)
        public float fov = 30f;
        [Tooltip("캐릭터가 카메라를 바라보게 하는 yaw. 등이 보이면 0↔180 바꿔")]
        public float charYaw = 0f;
        public float turnSpeed = 0f;
        [Tooltip("드래그 회전 감도: 화면 가로 폭만큼 드래그했을 때 도는 각도(도). 0이면 비활성")]
        public float dragTurnDegrees = 360f;

        RenderTexture rt;
        Camera cam;
        Transform charRoot;
        CharacterAssembler assembler;
        CharacterLook look;
        int gender = 1;
        bool dragging;
        float lastPointerX;

        void Start()
        {
            BuildRig();
            if (WebLobbyBridge.Active != null) WebLobbyBridge.Active.preview = this;
            ApplyGender(gender);
            if (WebLobbyBridge.Active != null && manifest != null)
                WebLobbyBridge.Active.FirePreviewReady(manifest.PreviewJson());
        }

        void BuildRig()
        {
            charRoot = new GameObject("PreviewChar").transform;
            charRoot.SetParent(transform, false);
            charRoot.position = farOrigin;
            charRoot.rotation = Quaternion.Euler(0f, charYaw, 0f);
            assembler = charRoot.gameObject.AddComponent<CharacterAssembler>();
            assembler.manifest = manifest;
            assembler.freezeAnimators = true;   // 프리뷰는 정자세 고정(움직임/스왑 어긋남 방지)

            rt = new RenderTexture(rtWidth, rtHeight, 16, RenderTextureFormat.ARGB32) { name = "PreviewRT" };

            var camGO = new GameObject("PreviewCamera");
            camGO.transform.SetParent(transform, false);
            cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backdrop;
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 50f;
            cam.cullingMask = 1 << previewLayer;     // 캐릭터(전용 레이어)만 렌더
            cam.targetTexture = rt;
            cam.transform.position = farOrigin + new Vector3(0f, camHeight, camDistance);
            cam.transform.LookAt(farOrigin + Vector3.up * lookHeight);

            var lightGO = new GameObject("PreviewLight");
            lightGO.transform.SetParent(transform, false);
            lightGO.transform.position = farOrigin + new Vector3(0f, 3f, 3f);
            lightGO.transform.rotation = Quaternion.Euler(35f, 160f, 0f);
            var l = lightGO.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.15f;
            l.cullingMask = 1 << previewLayer;

            EnsureEventSystem();
            var canvasGO = new GameObject("PreviewCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var rawGO = new GameObject("PreviewImage", typeof(RectTransform));
            rawGO.transform.SetParent(canvasGO.transform, false);
            var rrt = (RectTransform)rawGO.transform;
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var raw = rawGO.AddComponent<RawImage>();
            raw.texture = rt;
            raw.raycastTarget = false;
        }

        void Update()
        {
            if (turnSpeed != 0f && charRoot != null) charRoot.Rotate(0f, turnSpeed * Time.deltaTime, 0f);
            UpdateDragRotate();
        }

        // 꾸미기(step2)에서 화면을 좌우로 드래그하면 캐릭터가 회전한다(턴테이블 방식).
        // step1/로딩 중엔 HTML 커튼(pointer-events:auto)이 캔버스 입력을 막아 자연히 동작하지 않고,
        // step2 하단 시트 위의 드래그도 시트(.custom-sheet)가 이벤트를 가져가 캔버스에 닿지 않는다.
        void UpdateDragRotate()
        {
            if (dragTurnDegrees == 0f || charRoot == null) return;

            bool held; float x;
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                held = t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
                x = t.position.x;
            }
            else
            {
                held = Input.GetMouseButton(0);
                x = Input.mousePosition.x;
            }

            if (!held) { dragging = false; return; }
            if (!dragging) { dragging = true; lastPointerX = x; return; }

            float deltaX = x - lastPointerX;
            lastPointerX = x;
            // 카메라 쪽(앞면)이 손가락을 따라오는 방향: 오른쪽 드래그 → -Y 회전
            charRoot.Rotate(0f, -deltaX * dragTurnDegrees / Mathf.Max(1f, Screen.width), 0f);
        }

        // ===== 웹 → 유니티 (WebLobbyBridge 가 호출) =====
        public void ApplyGender(int g)
        {
            gender = g;
            if (manifest == null) return;
            look = manifest.DefaultFor(g);
            ResetYaw();
            assembler.ApplyFull(look);
            ApplyPreviewLayer();
        }

        public void SetCategory(int catSlot, int index)
        {
            if (look == null) look = new CharacterLook { gender = gender };
            look.Set(catSlot, index);
            assembler.SetPart(catSlot, index);
            ApplyPreviewLayer();
        }

        public void ApplyLook(CharacterLook l)
        {
            if (l == null) return;
            look = l.Clone();
            gender = l.gender;
            ResetYaw();
            assembler.ApplyFull(look);
            ApplyPreviewLayer();
        }

        // 전신 룩을 새로 적용할 때(꾸미기 진입/성별 변경)는 정면으로 되돌린다.
        // 부위 하나 스왑(SetCategory)은 보던 각도를 유지.
        void ResetYaw()
        {
            dragging = false;
            if (charRoot != null) charRoot.rotation = Quaternion.Euler(0f, charYaw, 0f);
        }

        public CharacterLook CurrentLook =>
            look != null ? look.Clone() : (manifest != null ? manifest.DefaultFor(gender) : null);

        void ApplyPreviewLayer()
        {
            if (charRoot != null) SetLayerRecursive(charRoot.gameObject, previewLayer);
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform c in go.transform) SetLayerRecursive(c.gameObject, layer);
        }

        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        void OnDestroy()
        {
            if (cam != null) cam.targetTexture = null;
            if (rt != null) { rt.Release(); Destroy(rt); }
        }
    }
}
