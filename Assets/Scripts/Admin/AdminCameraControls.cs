// AdminCameraControls.cs
// 관리자 뷰(아이패드) 카메라 이동 UI — 실행 시 코드로 UI 를 만든다(별도 프리팹·폰트 불필요).
//  - 오른쪽 위 작은 원형 아이콘: 누를 때마다 조작 UI 켜기/끄기 (기본 꺼짐 → 평소엔 고정 화면)
//  - 왼쪽 아래 이동 조이스틱: 카메라를 앞뒤·좌우로 이동(카메라가 보는 방향 기준, 높이·각도는 유지)
//  - 오른쪽 아래 회전 조이스틱: 좌우 = 방향 돌리기, 위아래 = 고개 들기/숙이기
//  - 회전 조이스틱 왼쪽 ▲/▼ 버튼: 누르고 있는 동안 카메라를 위/아래로 이동
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JSHWWedding.Admin
{
    public class AdminCameraControls : MonoBehaviour
    {
        [SerializeField] Transform cam;
        [SerializeField] float moveSpeed = 10f;       // 조이스틱 끝까지 밀었을 때 m/s
        [SerializeField] float verticalSpeed = 6f;    // ▲▼ 누르고 있을 때 m/s
        [SerializeField] float minHeightOffset = -20f; // 시작 높이 기준 내려갈 수 있는 한계(m) — 땅 아래로 안 가게
        [SerializeField] float maxHeightOffset = 80f;
        [SerializeField] float maxDistance = 150f;     // 시작 위치에서 수평으로 벗어날 수 있는 거리(m)
        [SerializeField] float yawSpeed = 70f;         // 회전 조이스틱 끝까지 밀었을 때 °/s
        [SerializeField] float pitchSpeed = 45f;
        [SerializeField] float minPitch = 5f;          // 거의 수평
        [SerializeField] float maxPitch = 85f;         // 거의 수직으로 내려다봄

        static readonly Color Dark = new Color(0f, 0f, 0f, 0.35f);
        static readonly Color Light = new Color(1f, 1f, 1f, 0.85f);
        static readonly Color Accent = new Color(0.85f, 0.5f, 0.6f, 0.9f);

        Vector3 startPos;
        float yaw, pitch;
        GameObject controls;
        Image toggleBg;
        AdminJoystick joystick, lookStick;
        AdminHoldButton upButton, downButton;
        RectTransform safeArea;
        Rect lastSafe;

        void Start()
        {
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            if (cam == null) { Debug.LogError("[AdminCameraControls] 카메라 없음"); enabled = false; return; }
            startPos = cam.position;
            yaw = cam.eulerAngles.y;
            pitch = Mathf.DeltaAngle(0f, cam.eulerAngles.x);
            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            BuildUI();
        }

        void Update()
        {
            if (Screen.safeArea != lastSafe) ApplySafeArea();
            if (!controls.activeSelf) return;

            Vector2 look = lookStick.Value;
            if (look.sqrMagnitude > 0.0001f)
            {
                yaw += look.x * yawSpeed * Time.deltaTime;
                pitch = Mathf.Clamp(pitch - look.y * pitchSpeed * Time.deltaTime, minPitch, maxPitch);   // 위로 밀면 고개 들기
                cam.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            Vector2 j = joystick.Value;
            float v = (upButton.Pressed ? 1f : 0f) - (downButton.Pressed ? 1f : 0f);
            if (j.sqrMagnitude < 0.0001f && v == 0f) return;

            Vector3 fwd = cam.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = cam.right; right.y = 0f; right.Normalize();
            Vector3 p = cam.position + (right * j.x + fwd * j.y) * moveSpeed * Time.deltaTime
                                     + Vector3.up * v * verticalSpeed * Time.deltaTime;

            Vector3 off = p - startPos; off.y = 0f;
            if (off.magnitude > maxDistance) { off = off.normalized * maxDistance; p.x = startPos.x + off.x; p.z = startPos.z + off.z; }
            p.y = Mathf.Clamp(p.y, startPos.y + minHeightOffset, startPos.y + maxHeightOffset);
            cam.position = p;
        }

        public void Toggle()
        {
            controls.SetActive(!controls.activeSelf);
            toggleBg.color = controls.activeSelf ? Accent : Dark;
        }

        // ===== UI 생성 =====
        void BuildUI()
        {
            var canvasGo = new GameObject("AdminControlsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 768);   // 아이패드 가로 기준
            scaler.matchWidthOrHeight = 0.5f;

            safeArea = NewRect("SafeArea", canvasGo.transform);
            safeArea.anchorMin = Vector2.zero; safeArea.anchorMax = Vector2.one;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            ApplySafeArea();

            Sprite circle = MakeCircle(), tri = MakeTriangle();

            // 오른쪽 위 토글 아이콘 (원 + 가운데 십자 화살표 느낌의 작은 원 4개)
            var toggle = NewImage("Toggle", safeArea, circle, Dark, new Vector2(1, 1), new Vector2(-20, -20), 52);
            toggleBg = toggle.GetComponent<Image>();
            NewImage("Dot", toggle, circle, Light, new Vector2(0.5f, 0.5f), Vector2.zero, 14);
            foreach (var d in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
                NewImage("Arrow", toggle, circle, Light, new Vector2(0.5f, 0.5f), d * 13f, 7);
            toggle.gameObject.AddComponent<Button>().onClick.AddListener(Toggle);

            controls = NewRect("Controls", safeArea).gameObject;
            var cr = (RectTransform)controls.transform;
            cr.anchorMin = Vector2.zero; cr.anchorMax = Vector2.one; cr.offsetMin = cr.offsetMax = Vector2.zero;

            // 왼쪽 아래 이동 조이스틱 / 오른쪽 아래 회전 조이스틱(손잡이 색으로 구분)
            joystick = Joystick("MoveStick", cr, circle, new Vector2(0, 0), new Vector2(150, 150), Light);
            lookStick = Joystick("LookStick", cr, circle, new Vector2(1, 0), new Vector2(-150, 150), Accent);

            // 회전 조이스틱 왼쪽 ▲ / ▼
            upButton = HoldButton("Up", cr, circle, tri, new Vector2(-330, 210), false);
            downButton = HoldButton("Down", cr, circle, tri, new Vector2(-330, 90), true);

            controls.SetActive(false);   // 기본은 꺼둠 — 아이콘을 눌러야 보임
        }

        AdminJoystick Joystick(string name, RectTransform parent, Sprite circle, Vector2 anchor, Vector2 pos, Color handleColor)
        {
            var baseRt = NewImage(name, parent, circle, Dark, anchor, pos, 200);
            var handle = NewImage("Handle", baseRt, circle, handleColor, new Vector2(0.5f, 0.5f), Vector2.zero, 84);
            handle.GetComponent<Image>().raycastTarget = false;
            var js = baseRt.gameObject.AddComponent<AdminJoystick>();
            js.handle = handle;
            js.radius = 100f;
            return js;
        }

        AdminHoldButton HoldButton(string name, RectTransform parent, Sprite circle, Sprite tri, Vector2 pos, bool flip)
        {
            var bg = NewImage(name, parent, circle, Dark, new Vector2(1, 0), pos, 100);
            var icon = NewImage("Icon", bg, tri, Light, new Vector2(0.5f, 0.5f), Vector2.zero, 44);
            icon.GetComponent<Image>().raycastTarget = false;
            if (flip) icon.localScale = new Vector3(1, -1, 1);
            var hb = bg.gameObject.AddComponent<AdminHoldButton>();
            hb.target = bg.GetComponent<Image>();
            hb.normal = Dark; hb.pressed = Accent;
            return hb;
        }

        void ApplySafeArea()
        {
            if (safeArea == null) return;
            lastSafe = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            safeArea.anchorMin = new Vector2(lastSafe.xMin / Screen.width, lastSafe.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(lastSafe.xMax / Screen.width, lastSafe.yMax / Screen.height);
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>anchor 지점에 pos 만큼 떨어진 곳(중심)에 size 크기 정사각 이미지.</summary>
        static RectTransform NewImage(string name, Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, float size)
        {
            var rt = NewRect(name, parent);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = color;
            return rt;
        }

        static Sprite MakeCircle()
        {
            const int n = 128; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = n * 0.5f - 1f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f));
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d + 0.5f)));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeTriangle()
        {
            const int n = 128; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
            {
                float half = (1f - (y + 0.5f) / n) * n * 0.5f;   // 아래가 넓고 위가 뾰족한 ▲
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - n * 0.5f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(half - dx + 0.5f)));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }
    }

    /// <summary>원형 조이스틱 — Value: (-1..1, -1..1).</summary>
    public class AdminJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform handle;
        public float radius = 100f;
        public Vector2 Value { get; private set; }

        public void OnPointerDown(PointerEventData e) => OnDrag(e);

        public void OnDrag(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out var local);
            local = Vector2.ClampMagnitude(local, radius);
            handle.anchoredPosition = local;
            Value = local / radius;
        }

        public void OnPointerUp(PointerEventData e)
        {
            handle.anchoredPosition = Vector2.zero;
            Value = Vector2.zero;
        }

        void OnDisable() { if (handle != null) handle.anchoredPosition = Vector2.zero; Value = Vector2.zero; }
    }

    /// <summary>누르고 있는 동안 Pressed=true 인 버튼.</summary>
    public class AdminHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Image target;
        public Color normal, pressed;
        public bool Pressed { get; private set; }

        public void OnPointerDown(PointerEventData e) => Set(true);
        public void OnPointerUp(PointerEventData e) => Set(false);
        public void OnPointerExit(PointerEventData e) => Set(false);
        void OnDisable() => Set(false);

        void Set(bool on)
        {
            Pressed = on;
            if (target != null) target.color = on ? pressed : normal;
        }
    }
}
