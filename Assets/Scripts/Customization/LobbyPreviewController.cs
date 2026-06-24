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
        public float lookHeight = -0.08f;
        public float fov = 30f;
        [Tooltip("캐릭터가 카메라를 바라보게 하는 yaw. 등이 보이면 0↔180 바꿔")]
        public float charYaw = 0f;
        public float turnSpeed = 0f;

        RenderTexture rt;
        Camera cam;
        Transform charRoot;
        CharacterAssembler assembler;
        CharacterLook look;
        int gender = 1;

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
        }

        // ===== 웹 → 유니티 (WebLobbyBridge 가 호출) =====
        public void ApplyGender(int g)
        {
            gender = g;
            if (manifest == null) return;
            look = manifest.DefaultFor(g);
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
            assembler.ApplyFull(look);
            ApplyPreviewLayer();
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
