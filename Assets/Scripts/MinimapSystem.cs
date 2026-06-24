// MinimapSystem.cs
// 예식장 미니맵 — 상공에서 똑바로 내려다보는 Orthographic 카메라를 RenderTexture로 찍어 화면 UI에 보여준다.
// 모든 UI/카메라를 코드로 생성하므로 씬의 빈 GameObject 하나에 이 컴포넌트만 붙이면 동작한다.
//
//  - 우측 상단에 작은 미니맵 썸네일이 "항상" 떠있다(라이브). 탭하면 큰 미니맵 창이 열린다.
//  - 큰 창이 떠있어도 이동은 가능(이동 잠금 없음). 닫기(✕)로 닫는다.
//  - 미니맵 위쪽 = 메인 카메라가 보는 방향(매 프레임 정렬). 내 위치는 마커로 표시.
//  - 성능을 위해 미니맵 카메라는 매 프레임이 아니라 renderInterval 간격으로 갱신.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Photon.Pun.Demo.PunBasics;

namespace JSHWWedding
{
    public class MinimapSystem : MonoBehaviour
    {
        [Header("미니맵 카메라")]
        [Tooltip("렌더 텍스처 해상도(정사각)")]
        public int textureSize = 768;
        [Tooltip("자동맞춤 끌 때: 중심(이 오브젝트) 위로 카메라를 띄울 높이(m)")]
        public float cameraHeight = 120f;
        [Tooltip("미니맵에 그릴 레이어 (기본: UI 레이어 제외)")]
        public LayerMask renderLayers = ~(1 << 5);
        [Tooltip("지형 밖(빈 영역) 배경색")]
        public Color background = new Color(0.82f, 0.90f, 0.92f, 1f);
        [Tooltip("미니맵 갱신 간격(초). 작을수록 부드럽지만 성능 부담↑")]
        public float renderInterval = 0.1f;

        [Header("촬영 영역")]
        [Tooltip("켜면 Terrain 전체 자동 프레이밍. 끄면 이 오브젝트 위치를 중심으로 잡는다(씬 뷰 기즈모로 영역 표시)")]
        public bool autoFitToTerrain = true;
        [Tooltip("자동맞춤 끌 때: 촬영 영역 절반 크기(m)")]
        public float areaHalfSize = 80f;
        [Tooltip("미니맵 확대 배율(클수록 더 확대). 플레이 중 조절 가능")]
        public float magnification = 3f;

        [Header("스타일")]
        public Color markerColor = new Color(0.913f, 0.639f, 0.612f, 1f);   // coral
        public Color frameColor  = new Color(0.357f, 0.624f, 0.702f, 1f);   // sea-dk 테두리
        public Color panelColor  = new Color(0.984f, 0.969f, 0.937f, 1f);   // paper 카드
        [Tooltip("한글 폰트(TMP). 비우면 런타임에 'Maplestory' 폰트를 자동 탐색")]
        public TMP_FontAsset font;

        [Header("지역명 라벨 (큰 미니맵에 표시)")]
        [Tooltip("빈 GameObject를 해당 지역 위치에 두고 드래그(anchor). text=표시할 이름(예: 예식홀)")]
        public RegionLabel[] regions;
        public float regionFontSize = 26f;
        public Color regionColor = new Color(0.40f, 0.22f, 0.30f, 1f);   // plum

        [Serializable]
        public class RegionLabel
        {
            public string text;
            public Transform anchor;
        }

        // 팔레트
        static readonly Color Coral = new Color(0.913f, 0.639f, 0.612f, 1f);
        static readonly Color Ink   = new Color(0.29f, 0.27f, 0.24f, 1f);
        static readonly Color Plum  = new Color(0.54f, 0.30f, 0.39f, 1f);

        RenderTexture rt;
        Camera cam;
        GameObject bigPanel;                 // 큰 미니맵 창(카드)
        RectTransform thumbMapRect, thumbMarker;
        RectTransform bigMapRect, bigMarker;
        Transform player;
        Vector3 areaCenter;
        float viewHalfSize;
        float renderTimer;

        readonly List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();
        readonly List<TextMeshProUGUI> regionLabelUI = new List<TextMeshProUGUI>();
        bool fontDone;

        void Start()
        {
            BuildCamera();
            EnsureEventSystem();
            BuildUI();
            ApplyFraming();
            cam.Render();        // 초기 1프레임(빈 썸네일 방지)
            TryApplyFont();
            SetOpen(false);      // 큰 창은 숨김(썸네일은 항상 표시)
        }

        // ===== 촬영 영역 / 카메라 =====
        void ComputeFraming(out Vector3 center, out float camY, out float baseHalf)
        {
            if (autoFitToTerrain && Terrain.activeTerrain != null)
            {
                Terrain t = Terrain.activeTerrain;
                Vector3 s = t.terrainData.size;
                Vector3 p = t.transform.position;
                center = p + new Vector3(s.x * 0.5f, 0f, s.z * 0.5f);
                camY = p.y + s.y + 30f;
                baseHalf = Mathf.Max(s.x, s.z) * 0.5f;
            }
            else
            {
                center = transform.position;
                camY = transform.position.y + cameraHeight;
                baseHalf = areaHalfSize;
            }
        }

        void ApplyFraming()
        {
            ComputeFraming(out Vector3 center, out float camY, out float baseHalf);
            areaCenter = center;
            viewHalfSize = baseHalf / Mathf.Max(0.01f, magnification);
            if (cam != null)
            {
                cam.transform.position = new Vector3(center.x, camY, center.z);
                cam.orthographicSize = viewHalfSize;
                cam.farClipPlane = (camY - center.y) + 200f;

                // 미니맵 위쪽 = 메인 카메라가 보는 방향
                Vector3 fwd = Vector3.forward;
                Camera main = Camera.main;
                if (main != null) { fwd = main.transform.forward; fwd.y = 0f; }
                if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
                cam.transform.rotation = Quaternion.LookRotation(Vector3.down, fwd.normalized);
            }
        }

        void BuildCamera()
        {
            rt = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32) { name = "MinimapRT" };

            GameObject go = new GameObject("MinimapCamera");
            go.transform.SetParent(transform, false);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.aspect = 1f;
            cam.cullingMask = renderLayers;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.nearClipPlane = 0.3f;
            cam.targetTexture = rt;
            cam.enabled = false;   // 매 프레임 자동 렌더 대신, renderInterval 로 수동 렌더
        }

        // ===== UI =====
        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        void BuildUI()
        {
            GameObject canvasGO = new GameObject("MinimapCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            BuildThumbnail(canvasGO.transform);
            BuildBigWindow(canvasGO.transform);
        }

        // 우측 상단: 항상 떠있는 작은 미니맵(=큰 창 여는 버튼)
        void BuildThumbnail(Transform parent)
        {
            RectTransform thumb = NewRect("MinimapThumb", parent);
            thumb.anchorMin = thumb.anchorMax = new Vector2(1f, 1f);
            thumb.pivot = new Vector2(1f, 1f);
            thumb.anchoredPosition = new Vector2(-22f, -22f);
            thumb.sizeDelta = new Vector2(210f, 210f);
            Image frame = thumb.gameObject.AddComponent<Image>();   // 테두리
            frame.color = frameColor;
            Button btn = thumb.gameObject.AddComponent<Button>();
            btn.targetGraphic = frame;
            btn.onClick.AddListener(() => SetOpen(true));

            thumbMapRect = NewRect("Map", thumb);
            Stretch(thumbMapRect, 7f);
            RawImage img = thumbMapRect.gameObject.AddComponent<RawImage>();
            img.texture = rt;
            img.raycastTarget = false;

            thumbMarker = MakeMarker(thumbMapRect, 16f);
        }

        // 큰 미니맵 창(중앙 카드, 백드롭 없음 → 카드 밖 탭으로 이동 가능)
        void BuildBigWindow(Transform parent)
        {
            RectTransform card = NewRect("MinimapBig", parent);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(860f, 940f);   // 타이틀96 + 갭20 + 맵788 + 하단여백36
            card.anchoredPosition = new Vector2(0f, 40f);   // 타이틀이 위를 먹어 지도가 40px 내려가므로 카드를 올려 지도를 화면 중앙에
            Image cardBg = card.gameObject.AddComponent<Image>();   // raycastTarget=true → 카드 위 탭은 게임으로 안 샘
            cardBg.color = panelColor;
            bigPanel = card.gameObject;

            // 타이틀 바
            RectTransform titleBar = NewRect("TitleBar", card);
            titleBar.anchorMin = new Vector2(0f, 1f); titleBar.anchorMax = new Vector2(1f, 1f);
            titleBar.pivot = new Vector2(0.5f, 1f);
            titleBar.sizeDelta = new Vector2(0f, 96f);
            titleBar.anchoredPosition = Vector2.zero;
            titleBar.gameObject.AddComponent<Image>().color = Coral;
            AddLabel(titleBar, "미니맵", 46f, Color.white, true);

            // 지도 프레임 + 지도
            RectTransform frame = NewRect("MapFrame", card);
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 1f);
            frame.pivot = new Vector2(0.5f, 1f);
            frame.anchoredPosition = new Vector2(0f, -116f);
            frame.sizeDelta = new Vector2(788f, 788f);
            Image frameImg = frame.gameObject.AddComponent<Image>();
            frameImg.color = frameColor;
            frameImg.raycastTarget = false;

            bigMapRect = NewRect("Map", frame);
            Stretch(bigMapRect, 9f);
            RawImage bigImg = bigMapRect.gameObject.AddComponent<RawImage>();
            bigImg.texture = rt;
            bigImg.raycastTarget = false;

            bigMarker = MakeMarker(bigMapRect, 28f);
            BuildRegionLabels(bigMapRect);

            // 닫기 ✕ (두 막대)
            RectTransform close = NewRect("Close", card);
            close.anchorMin = close.anchorMax = new Vector2(1f, 1f);
            close.pivot = new Vector2(1f, 1f);
            close.anchoredPosition = new Vector2(-16f, -16f);
            close.sizeDelta = new Vector2(64f, 64f);
            Image closeBg = close.gameObject.AddComponent<Image>();
            closeBg.color = new Color(1f, 1f, 1f, 0.22f);
            Button closeBtn = close.gameObject.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => SetOpen(false));
            AddBar(close, 45f, Color.white);
            AddBar(close, -45f, Color.white);
        }

        // ===== 동작 =====
        void Update()
        {
            if (player == null) player = FindLocalPlayer();
            ApplyFraming();   // 카메라 추적/정렬

            renderTimer += Time.deltaTime;
            if (renderTimer >= renderInterval)
            {
                renderTimer = 0f;
                if (cam != null) cam.Render();   // 스로틀 렌더(썸네일/큰창 공용)
            }

            UpdateMarkers();
        }

        public void OpenMinimap() { SetOpen(true); }
        public void CloseMinimap() { SetOpen(false); }

        void SetOpen(bool v)
        {
            if (bigPanel != null) bigPanel.SetActive(v);
            // 이동 잠금 없음 — 미니맵을 보면서도 이동 가능
            if (v)
            {
                player = FindLocalPlayer();
                TryApplyFont();
                UpdateMarkers();
            }
        }

        void UpdateMarkers()
        {
            bool has = player != null && cam != null;
            if (thumbMarker != null) thumbMarker.gameObject.SetActive(has);
            bool bigOn = bigPanel != null && bigPanel.activeSelf;
            if (bigOn) UpdateRegionLabels();   // 지역명은 플레이어 유무와 무관
            if (bigMarker != null) bigMarker.gameObject.SetActive(has && bigOn);
            if (!has) return;

            if (viewHalfSize <= 0f) ApplyFraming();
            Vector3 off = player.position - areaCenter;
            float u = Mathf.Clamp(Vector3.Dot(off, cam.transform.right) / (2f * viewHalfSize), -0.5f, 0.5f);
            float v = Mathf.Clamp(Vector3.Dot(off, cam.transform.up)    / (2f * viewHalfSize), -0.5f, 0.5f);

            if (thumbMarker != null)
            {
                Vector2 s = thumbMapRect.rect.size;
                thumbMarker.anchoredPosition = new Vector2(u * s.x, v * s.y);
            }
            if (bigMarker != null && bigOn)
            {
                Vector2 s = bigMapRect.rect.size;
                bigMarker.anchoredPosition = new Vector2(u * s.x, v * s.y);
            }
        }

        // 지역명 라벨을 큰 미니맵에 생성(빈 GameObject anchor 마다 1개). 위치는 매 프레임 UpdateRegionLabels 에서.
        void BuildRegionLabels(RectTransform map)
        {
            regionLabelUI.Clear();
            if (regions == null) return;
            foreach (var r in regions)
            {
                if (r == null || string.IsNullOrEmpty(r.text)) { regionLabelUI.Add(null); continue; }
                TextMeshProUGUI lbl = AddLabel(map, r.text, regionFontSize, regionColor, true);
                RectTransform rt = lbl.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);   // 중심 기준 배치(AddLabel의 Stretch 덮어쓰기)
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(240f, 44f);
                lbl.outlineWidth = 0.22f;          // 지도 위 대비용 흰 외곽선
                lbl.outlineColor = Color.white;
                regionLabelUI.Add(lbl);
            }
        }

        // 각 지역 anchor 의 월드좌표를 미니맵 좌표로 변환해 라벨 배치(화면 밖이면 숨김). 마커와 동일한 매핑.
        void UpdateRegionLabels()
        {
            if (regions == null || cam == null || bigMapRect == null) return;
            if (viewHalfSize <= 0f) ApplyFraming();
            Vector2 sz = bigMapRect.rect.size;
            for (int i = 0; i < regions.Length && i < regionLabelUI.Count; i++)
            {
                TextMeshProUGUI lbl = regionLabelUI[i];
                if (lbl == null) continue;
                RegionLabel r = regions[i];
                if (r == null || r.anchor == null) { lbl.gameObject.SetActive(false); continue; }

                Vector3 off = r.anchor.position - areaCenter;
                float u = Vector3.Dot(off, cam.transform.right) / (2f * viewHalfSize);
                float v = Vector3.Dot(off, cam.transform.up)    / (2f * viewHalfSize);
                bool inside = Mathf.Abs(u) <= 0.5f && Mathf.Abs(v) <= 0.5f;
                lbl.gameObject.SetActive(inside);
                if (inside) lbl.rectTransform.anchoredPosition = new Vector2(u * sz.x, v * sz.y);
            }
        }

        Transform FindLocalPlayer()
        {
            var moves = FindObjectsByType<PlayerClickToMove>(FindObjectsSortMode.None);
            foreach (var m in moves)
                if (m.photonView != null && m.photonView.IsMine) return m.transform;
            return null;
        }

        // 한글 폰트 자동 탐색(이름표 등에서 로드된 Maplestory 폰트를 찾아 라벨에 적용)
        void TryApplyFont()
        {
            if (fontDone) return;
            if (font == null)
            {
                foreach (var f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                    if (f != null && f.name.IndexOf("Maplestory", StringComparison.OrdinalIgnoreCase) >= 0) { font = f; break; }
            }
            if (font != null)
            {
                foreach (var t in labels) if (t != null) t.font = font;
                fontDone = true;
            }
        }

        void OnDrawGizmosSelected()
        {
            ComputeFraming(out Vector3 center, out float camY, out float baseHalf);
            float half = baseHalf / Mathf.Max(0.01f, magnification);
            Gizmos.color = new Color(0.36f, 0.78f, 0.85f, 1f);
            Gizmos.DrawWireCube(center, new Vector3(half * 2f, 0.05f, half * 2f));
            Vector3 camPos = new Vector3(center.x, camY, center.z);
            Gizmos.color = Coral;
            Gizmos.DrawWireSphere(camPos, Mathf.Max(0.5f, half * 0.04f));
            Gizmos.DrawLine(camPos, center);
        }

        void OnDestroy()
        {
            if (cam != null) cam.targetTexture = null;
            if (rt != null) { rt.Release(); Destroy(rt); }
        }

        // ===== UI 헬퍼 =====
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

        // 흰 테두리 다이아몬드 + 안쪽 코랄 점 (어떤 지도 위에서도 잘 보이는 내 위치 마커)
        RectTransform MakeMarker(RectTransform parent, float size)
        {
            RectTransform m = NewRect("Marker", parent);
            m.anchorMin = m.anchorMax = new Vector2(0.5f, 0.5f);
            m.pivot = new Vector2(0.5f, 0.5f);
            m.sizeDelta = new Vector2(size, size);
            m.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image outline = m.gameObject.AddComponent<Image>();
            outline.color = Color.white;
            outline.raycastTarget = false;

            RectTransform inner = NewRect("Dot", m);
            Stretch(inner, size * 0.2f);
            Image dot = inner.gameObject.AddComponent<Image>();
            dot.color = markerColor;
            dot.raycastTarget = false;
            return m;
        }

        TextMeshProUGUI AddLabel(RectTransform parent, string text, float fontSize, Color color, bool bold)
        {
            RectTransform rt = NewRect("Label", parent);
            Stretch(rt, 0f);
            TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            if (bold) t.fontStyle = FontStyles.Bold;
            if (font != null) t.font = font;
            t.raycastTarget = false;
            labels.Add(t);
            return t;
        }

        static void AddBar(RectTransform parent, float angle, Color color)
        {
            RectTransform b = NewRect("Bar", parent);
            b.anchorMin = b.anchorMax = new Vector2(0.5f, 0.5f);
            b.pivot = new Vector2(0.5f, 0.5f);
            b.sizeDelta = new Vector2(30f, 5f);
            b.localRotation = Quaternion.Euler(0f, 0f, angle);
            Image img = b.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }
    }
}
