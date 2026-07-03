// WreathViewZone.cs
// 축하 화환(CelebrateFlowers 자식) 근접 '보기' 버튼. 누르면 웹 팝업(작성자/축하 문구)이 뜬다.
//  - 웹이 Firestore 화환 개수를 SendMessage("WebBridge","SetWreathCount","n") 로 보내면
//    ApplyCount 가 자식 0..n-1 활성화 + 이 컴포넌트 부착(슬롯 번호 지정)을 처리한다.
//  - 화환 슬롯 = CelebrateFlowers 자식 인덱스 = Firestore 작성순(0~14).
// 버튼 UI 방식은 InteractionZone(월드 캔버스 빌보드 버튼)과 동일.
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun.Demo.PunBasics;

namespace JSHWWedding
{
    public class WreathViewZone : MonoBehaviour
    {
        [Tooltip("이 화환의 슬롯 번호(0~14) — 웹 화환 목록 인덱스와 매칭")]
        public int slot;
        [Tooltip("플레이어가 이 거리(m) 안에 들어오면 '보기' 버튼 표시")]
        public float activateRadius = 3.5f;
        [Tooltip("오브젝트 기준 버튼 높이(m)")]
        public float buttonHeight = 2.4f;
        public Color buttonColor = new Color(0.913f, 0.639f, 0.612f, 1f); // coral

        Transform player;
        Camera cam;
        GameObject ui;
        bool shown;
        Bounds zoneBounds;
        bool boundsReady;

        /// <summary>웹 → 화환 개수 반영: CelebrateFlowers 자식 0..count-1 활성화(+존 부착), 나머지 비활성화.</summary>
        public static void ApplyCount(int count)
        {
            var root = GameObject.Find("CelebrateFlowers");
            if (root == null) return;   // Wedding 씬이 아직 아님 — WebLobbyBridge 가 씬 로드 후 재적용
            var t = root.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i).gameObject;
                bool on = i < count;
                if (child.activeSelf != on) child.SetActive(on);
                if (on && child.GetComponent<WreathViewZone>() == null)
                {
                    var z = child.AddComponent<WreathViewZone>();
                    z.slot = i;
                }
            }
            Debug.Log($"[WreathViewZone] 화환 {Mathf.Min(count, t.childCount)}/{t.childCount}개 활성화");
        }

        void EnsureBounds()
        {
            if (boundsReady) return;
            var rs = GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                zoneBounds = b;
            }
            else zoneBounds = new Bounds(transform.position, Vector3.zero);
            boundsReady = true;
        }

        void Update()
        {
            if (cam == null) cam = Camera.main;
            if (player == null) player = FindLocalPlayer();
            if (player == null || cam == null) { SetShown(false); return; }

            EnsureBounds();
            Vector3 c = zoneBounds.center;
            float dx = Mathf.Max(0f, Mathf.Abs(player.position.x - c.x) - zoneBounds.extents.x);
            float dz = Mathf.Max(0f, Mathf.Abs(player.position.z - c.z) - zoneBounds.extents.z);
            float horiz = Mathf.Sqrt(dx * dx + dz * dz);
            SetShown(horiz <= activateRadius && !UIInputLock.Locked && !NpcDialogCamera.Active);

            if (shown && ui != null)
            {
                ui.transform.position = new Vector3(c.x, zoneBounds.min.y + buttonHeight, c.z);
                ui.transform.rotation = cam.transform.rotation;   // 빌보드
            }
        }

        Transform FindLocalPlayer()
        {
            var moves = FindObjectsByType<PlayerClickToMove>(FindObjectsSortMode.None);
            foreach (var m in moves)
                if (m.photonView != null && m.photonView.IsMine) return m.transform;
            return null;
        }

        void SetShown(bool v)
        {
            if (v == shown) return;
            shown = v;
            if (v && ui == null) BuildUI();
            if (ui != null) ui.SetActive(v);
        }

        void OnClicked()
        {
            SetShown(false);
            VenueWeb.OpenWreath(slot);
        }

        void BuildUI()
        {
            ui = new GameObject("WreathButton(" + slot + ")",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = ui.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            var rt = (RectTransform)ui.transform;
            rt.sizeDelta = new Vector2(180, 76);
            rt.localScale = Vector3.one * 0.01f;

            var btnGO = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(ui.transform, false);
            var brt = (RectTransform)btnGO.transform;
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
            var img = btnGO.GetComponent<Image>();
            img.color = buttonColor;
            var btn = btnGO.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(OnClicked);

            var lblGO = new GameObject("Label", typeof(RectTransform));
            lblGO.transform.SetParent(btnGO.transform, false);
            var lrt = (RectTransform)lblGO.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "보기";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 36;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;

            ui.SetActive(false);
        }
    }
}
