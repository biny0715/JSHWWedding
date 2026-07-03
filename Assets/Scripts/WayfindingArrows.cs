// WayfindingArrows.cs
// 길 안내 — 화면 "밖"에 있는 목표(예식장/신부대기실/연회장)를 화면 가장자리 화살표 + 거리로 안내한다.
//  - 목표에 가까우면(nearHideDistance) 숨긴다. 화면 안/밖과 무관하게 표시(가까워질 때까지 유지).
//  - 목표는 씬의 "MinimapTxt" 하위 오브젝트(CeremonyHall/BrideRoom/BanquetHall)를 이름으로 자동 탐색.
//  - 웹 좌상단 "길 안내" 토글 → WebLobbyBridge.SetGuideArrows → WayfindingArrows.Enabled 로 On/Off.
//  - 씬 배치 불필요: Wedding 씬 로드 시 자동 생성(부트스트랩). UI/삼각형 스프라이트를 코드로 생성(MinimapSystem 패턴).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun.Demo.PunBasics;

namespace JSHWWedding
{
    public class WayfindingArrows : MonoBehaviour
    {
        /// <summary>웹 "길 안내" 토글로 제어(기본 On). WebLobbyBridge.SetGuideArrows 가 설정.</summary>
        public static bool Enabled = true;

        [Header("동작")]
        [Tooltip("이 거리(m, 수평) 안으로 들어오면 해당 안내를 숨긴다")]
        public float nearHideDistance = 6f;
        [Tooltip("화면 가장자리에서 안쪽으로 띄울 여백(px 기준). 화면 크기에 맞춰 보정됨")]
        public float edgeMargin = 120f;

        [Header("대상 (MinimapTxt 하위)")]
        public string parentName = "MinimapTxt";

        // 안내 대상: 자식 오브젝트 이름 → 화면 표시 라벨
        static readonly string[,] TARGETS = {
            { "CeremonyHall", "예식장" },
            { "BrideRoom",    "신부대기실" },
            { "BanquetHall",  "연회장" },
        };

        // 팔레트(MinimapSystem 과 동일 톤)
        static readonly Color Coral = new Color(0.913f, 0.639f, 0.612f, 1f);
        static readonly Color Ink   = new Color(0.29f, 0.27f, 0.24f, 1f);

        class Indicator
        {
            public Transform target;
            public string label;
            public RectTransform root;
            public RectTransform arrow;
            public TextMeshProUGUI text;
        }

        readonly List<Indicator> indicators = new List<Indicator>();
        Canvas canvas;
        Camera cam;
        Transform player;
        Sprite arrowSprite;
        TMP_FontAsset font;
        bool fontDone;
        float uiScale = 1f;
        WeddingCameraIntro intro;   // 인트로(섬 전체뷰) 카메라 — 끝날 때까지 네비 숨김
        bool introGate;             // 인트로 종료(또는 인트로 없음) 후 true

        // ===== 부트스트랩: Wedding 씬 로드 시 자동 생성(씬에 수동 배치 불필요) =====
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            SceneManager.sceneLoaded += (scene, mode) => { if (scene.name == "Wedding") Spawn(); };
            if (SceneManager.GetActiveScene().name == "Wedding") Spawn();
        }
        static void Spawn()
        {
            if (FindFirstObjectByType<WayfindingArrows>() != null) return;
            new GameObject("WayfindingArrows").AddComponent<WayfindingArrows>();
        }

        void Start()
        {
            cam = Camera.main;
            intro = FindFirstObjectByType<WeddingCameraIntro>();   // 인트로 진행 중이면 그게 끝날 때까지 대기
            introGate = (intro == null);                           // 인트로가 없으면 바로 표시 허용
            uiScale = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) / 900f, 0.8f, 1.8f);
            arrowSprite = MakeArrowSprite();
            BuildCanvas();
            ResolveTargets();
            BuildIndicators();
            TryApplyFont();
        }

        // ===== 대상 탐색 =====
        void ResolveTargets()
        {
            Transform parent = null;
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == parentName) { parent = t; break; }
            if (parent == null) { Debug.LogWarning($"[Wayfinding] '{parentName}' 오브젝트를 씬에서 못 찾음"); return; }

            for (int i = 0; i < TARGETS.GetLength(0); i++)
            {
                string child = TARGETS[i, 0], label = TARGETS[i, 1];
                Transform t = FindDeep(parent, child);
                if (t == null) { Debug.LogWarning($"[Wayfinding] '{parentName}/{child}' 못 찾음 → '{label}' 안내 생략"); continue; }
                indicators.Add(new Indicator { target = t, label = label });
            }
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (string.Equals(root.name, name, StringComparison.OrdinalIgnoreCase)) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        // ===== 매 프레임: 화면 밖 목표만 가장자리 화살표로 =====
        void Update()
        {
            if (cam == null) cam = Camera.main;
            if (player == null) player = FindLocalPlayer();
            if (!fontDone) TryApplyFont();
            // 인트로 카메라(섬 전체뷰)가 스스로 비활성화되면(=연출 종료) 그때부터 네비 표시
            if (!introGate && (intro == null || !intro.gameObject.activeInHierarchy)) introGate = true;

            // 인트로 진행 중 / 전역 Off / 카메라·플레이어 없음 / 웹 오버레이 열림(이동 잠금) / NPC 대화 카메라 → 전부 숨김
            bool on = introGate && Enabled && cam != null && player != null && !UIInputLock.Locked && !NpcDialogCamera.Active;

            // 좌우 "양 끝" 하단에 도킹(중앙 캐릭터를 가리지 않게). 같은 쪽 여러 개는 위로 쌓는다.
            float halfW = Screen.width * 0.5f, halfH = Screen.height * 0.5f;
            float marginX = Mathf.Min(edgeMargin * uiScale, halfW * 0.45f);
            float ex = halfW - marginX;      // 좌우 끝에서 안쪽으로 들어온 X(px, 중심 기준)
            float baseY = -halfH * 0.5f;     // 화면 아래쪽(중심에서 아래로 50%)
            float stepY = 96f * uiScale;     // 같은 쪽 겹침 방지 간격
            int leftN = 0, rightN = 0;

            foreach (var ind in indicators)
            {
                if (!on || ind.target == null) { Show(ind, false); continue; }

                // 수평 거리(XZ)로 가까움 판정
                Vector3 pp = player.position, tp = ind.target.position;
                float dist = Vector2.Distance(new Vector2(pp.x, pp.z), new Vector2(tp.x, tp.z));
                if (dist < nearHideDistance) { Show(ind, false); continue; }

                // 숨김은 "가까우면(nearHideDistance)"으로만 판단 → 화면에 보여도 가까워질 때까지 계속 표시
                Vector3 vp = cam.WorldToViewportPoint(tp);
                bool inFront = vp.z > 0f;

                // 화면 중심 기준 방향(카메라 뒤쪽이면 반사) — 화살표 회전 + 좌/우 판정에 사용
                Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
                if (!inFront) dir = -dir;
                if (dir.sqrMagnitude < 1e-6f) dir = new Vector2(0f, -1f);
                dir.Normalize();

                // 목표가 오른쪽이면 오른쪽 끝, 왼쪽이면 왼쪽 끝. 아래쪽에서 위로 쌓기.
                bool right = dir.x >= 0f;
                float x = right ? ex : -ex;
                int idx = right ? rightN++ : leftN++;
                float y = Mathf.Min(baseY + idx * stepY, halfH * 0.2f);

                Show(ind, true);
                ind.root.anchoredPosition = new Vector2(x, y);
                float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                ind.arrow.localEulerAngles = new Vector3(0f, 0f, ang - 90f);   // 삼각형 스프라이트는 위(+Y)를 가리킴
                ind.text.text = $"{ind.label} {Mathf.RoundToInt(dist)}m";
            }
        }

        static void Show(Indicator ind, bool v)
        {
            if (ind.root != null && ind.root.gameObject.activeSelf != v) ind.root.gameObject.SetActive(v);
        }

        Transform FindLocalPlayer()
        {
            var moves = FindObjectsByType<PlayerClickToMove>(FindObjectsSortMode.None);
            foreach (var m in moves)
                if (m.photonView != null && m.photonView.IsMine) return m.transform;
            return null;
        }

        // ===== UI 생성 =====
        void BuildCanvas()
        {
            var go = new GameObject("GuideCanvas", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;   // 미니맵 캔버스(90)보다 아래

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;   // 좌표=화면 px (중심 기준 배치 단순화)
            scaler.scaleFactor = 1f;
        }

        void BuildIndicators()
        {
            foreach (var ind in indicators)
            {
                RectTransform root = NewRect("Guide_" + ind.label, canvas.transform);
                root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
                root.pivot = new Vector2(0.5f, 0.5f);
                root.sizeDelta = new Vector2(200f, 130f) * uiScale;

                // 화살표(삼각형) — 위를 가리키는 상태로 두고 방향에 맞춰 회전
                RectTransform arrow = NewRect("Arrow", root);
                arrow.anchorMin = arrow.anchorMax = new Vector2(0.5f, 0.5f);
                arrow.pivot = new Vector2(0.5f, 0.5f);
                arrow.sizeDelta = new Vector2(52f, 66f) * uiScale;   // 세로로 길게 → 방향 구분이 쉬움
                arrow.anchoredPosition = new Vector2(0f, 34f * uiScale);
                Image ai = arrow.gameObject.AddComponent<Image>();
                ai.sprite = arrowSprite; ai.color = Color.white; ai.raycastTarget = false;   // 색은 스프라이트에 구움

                // 라벨(회전 안 함) — 배경 박스 없이 텍스트만
                RectTransform pill = NewRect("Pill", root);
                pill.anchorMin = pill.anchorMax = new Vector2(0.5f, 0.5f);
                pill.pivot = new Vector2(0.5f, 0.5f);
                pill.sizeDelta = new Vector2(190f, 44f) * uiScale;
                pill.anchoredPosition = new Vector2(0f, -24f * uiScale);

                RectTransform txt = NewRect("Text", pill);
                Stretch(txt, 8f);
                TextMeshProUGUI t = txt.gameObject.AddComponent<TextMeshProUGUI>();
                t.text = ind.label;
                t.color = Ink;
                t.alignment = TextAlignmentOptions.Center;
                t.fontStyle = FontStyles.Bold;
                t.raycastTarget = false;
                t.enableAutoSizing = true;                 // 긴 라벨(신부대기실 120m)도 알아서 축소
                t.fontSizeMin = 12f; t.fontSizeMax = 22f * uiScale;
                if (font != null) t.font = font;

                ind.root = root; ind.arrow = arrow; ind.text = t;
                root.gameObject.SetActive(false);
            }
        }

        // 한글 폰트 자동 탐색(이름표/미니맵과 동일 — 로드된 Maplestory 폰트 재사용)
        void TryApplyFont()
        {
            if (fontDone) return;
            if (font == null)
                foreach (var f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                    if (f != null && f.name.IndexOf("Maplestory", StringComparison.OrdinalIgnoreCase) >= 0) { font = f; break; }
            if (font != null)
            {
                foreach (var ind in indicators) if (ind.text != null) ind.text.font = font;
                fontDone = true;
            }
        }

        // 위(+Y)를 가리키는 "화살표"(뾰족한 머리 + 얇은 자루 + 어두운 외곽선) 스프라이트를 런타임 생성.
        // 자루와 어깨(머리-자루 경계), 외곽선 덕분에 어느 쪽이 앞인지 한눈에 구분된다. 스프라이트 에셋 의존 없음.
        static Sprite MakeArrowSprite()
        {
            const int s = 128;                 // 외곽선까지 또렷하게 담기 위해 넉넉한 해상도
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color line  = new Color(0.12f, 0.10f, 0.09f, 1f);   // 어두운 외곽선
            Color fill  = Coral;                                // 화살표 몸통(코랄)
            float cx = s * 0.5f;
            float b  = s * 0.06f;              // 외곽선 두께(px)
            float headBaseY = s * 0.46f;       // 머리 밑변 y (0=하단, s-1=상단 apex)
            float headHalf  = s * 0.46f;       // 머리 밑변 반너비(가장 넓은 곳)
            float shaftHalf = s * 0.15f;       // 자루 반너비

            // (x,y)가 화살표 내부인지 — inset 만큼 안쪽으로 줄이면 채움 영역, 0이면 외곽선 포함 전체
            bool Inside(int x, int y, float inset)
            {
                if (y < inset || y > (s - 1) - inset) return false;
                float hw = (y >= headBaseY)
                    ? headHalf * (1f - (y - headBaseY) / ((s - 1) - headBaseY))   // 머리: apex로 갈수록 뾰족
                    : shaftHalf;                                                  // 자루
                hw -= inset;
                return hw > 0f && Mathf.Abs(x + 0.5f - cx) <= hw;
            }

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    if (Inside(x, y, b)) tex.SetPixel(x, y, fill);        // 안쪽 채움
                    else if (Inside(x, y, 0f)) tex.SetPixel(x, y, line);  // 가장자리 외곽선
                    else tex.SetPixel(x, y, clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        void OnDestroy()
        {
            if (arrowSprite != null)
            {
                if (arrowSprite.texture != null) Destroy(arrowSprite.texture);
                Destroy(arrowSprite);
            }
        }

        // ===== 헬퍼(MinimapSystem 과 동일) =====
        static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rt, float margin)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
        }
    }
}
