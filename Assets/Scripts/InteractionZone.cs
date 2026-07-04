// InteractionZone.cs
// FlowerDecoZone(방명록) / PictureZone(사진앨범) 같은 오브젝트에 붙인다.
// 플레이어가 activateRadius 안으로 오면 카메라를 바라보는 빌보드 버튼(월드 캔버스)을 띄우고,
// 클릭하면 웹 창(jslib)을 연다. 이동 잠금은 Unity가 직접 걸지 않고, 웹이 창을 "실제로" 열었을 때
// OnVenueOverlayOpened 로 건다(WebLobbyBridge). 창을 닫으면 OnVenueOverlayClosed 가 푼다.
// → 에디터/모바일 로컬테스트처럼 창이 안 뜨는 경우엔 잠기지 않아 그대로 이동 가능.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using Photon.Pun.Demo.PunBasics;

namespace JSHWWedding
{
    public class InteractionZone : MonoBehaviour
    {
        public enum ZoneAction { Guestbook, Album, Talk }

        [Header("동작")]
        public ZoneAction action = ZoneAction.Guestbook;
        public string buttonLabel = "방명록";
        [Tooltip("Talk 액션일 때 말 거는 NPC(대화창 화자) 이름")]
        public string talkName = "비니";
        [Tooltip("Talk 대화 종류: \"npc\"(비니 퀘스트) / \"help\"(지니 도움말)")]
        public string talkMode = "npc";

        [Header("표시")]
        [Tooltip("플레이어가 이 거리 안에 들어오면 버튼 표시")]
        public float activateRadius = 4f;
        [Tooltip("오브젝트 기준 버튼 높이(m)")]
        public float buttonHeight = 2.2f;
        public Color buttonColor = new Color(0.913f, 0.639f, 0.612f, 1f); // coral

        Transform player;
        Camera cam;
        GameObject ui;
        bool shown;
        Bounds zoneBounds;     // 자식 장식들의 합산 bounds(실제 위치). pivot이 장식과 어긋나도 정확.
        bool boundsReady;

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
            // 장식 footprint(XZ)까지의 수평 거리(안에 있으면 0). 높이(Y)는 무시.
            float dx = Mathf.Max(0f, Mathf.Abs(player.position.x - c.x) - zoneBounds.extents.x);
            float dz = Mathf.Max(0f, Mathf.Abs(player.position.z - c.z) - zoneBounds.extents.z);
            float horiz = Mathf.Sqrt(dx * dx + dz * dz);
            // NPC 대화 카메라가 켜져 있는 동안엔 버튼 숨김(대화 종료 시 Active=false → 다시 표시)
            SetShown(horiz <= activateRadius && !UIInputLock.Locked && !NpcDialogCamera.Active);

            if (shown && ui != null)
            {
                ui.transform.position = new Vector3(c.x, zoneBounds.min.y + buttonHeight, c.z);
                ui.transform.rotation = cam.transform.rotation; // 빌보드(카메라 바라봄)
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
            // 이동 잠금은 "웹이 창을 실제로 열었을 때"만 건다(웹의 OnOpenGuestbook/Album → lockMove → OnVenueOverlayOpened).
            // 여기서 낙관적으로 잠그면, 에디터/모바일 로컬테스트처럼 창이 안 뜨는 경우 잠금이 안 풀려 못 움직이게 됨.
            SetShown(false);
            string nick = string.IsNullOrEmpty(PhotonNetwork.NickName) ? "하객" : PhotonNetwork.NickName;
            if (action == ZoneAction.Guestbook) VenueWeb.OpenNpcDialog(nick, "celebrate");   // 축하 감사 대화 → 방명록
            else if (action == ZoneAction.Album) VenueWeb.OpenAlbum();
            else { NpcDialogCamera.Focus(transform); VenueWeb.OpenNpcDialog(talkName, talkMode); }   // Talk: NPC 정면 즉시컷 + 대화창
        }

        void BuildUI()
        {
            ui = new GameObject("ZoneButton(" + buttonLabel + ")",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = ui.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            var rt = (RectTransform)ui.transform;
            rt.sizeDelta = new Vector2(220, 84);
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
            tmp.text = buttonLabel;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 40;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;

            ui.SetActive(false);
        }

        void OnDrawGizmosSelected()
        {
            var rs = GetComponentsInChildren<Renderer>();
            Vector3 c = transform.position;
            if (rs.Length > 0)
            {
                var b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                c = b.center;
                Gizmos.color = new Color(0.95f, 0.6f, 0.7f, 0.3f);
                Gizmos.DrawWireCube(b.center, b.size);
            }
            Gizmos.color = new Color(0.95f, 0.6f, 0.7f, 0.6f);
            Gizmos.DrawWireSphere(new Vector3(c.x, c.y, c.z), activateRadius);
        }

        // ===== NPC에 상호작용 존을 런타임 자동 부착 (씬 수동 배치 불필요) =====
        //  - 비니: "말걸기"(npc 퀘스트) / 지니: "도움말"(help 안내)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void BootstrapNpcTalk()
        {
            SceneManager.sceneLoaded += (scene, mode) => { if (scene.name == "Wedding") AttachAllNpcs(); };
            if (SceneManager.GetActiveScene().name == "Wedding") AttachAllNpcs();
        }

        static void AttachAllNpcs()
        {
            AttachTalkTo("비니", "말걸기", "npc");
            AttachTalkTo("지니", "도움말", "help");
        }

        static void AttachTalkTo(string npcName, string label, string mode)
        {
            // 이미 부착돼 있으면 스킵
            foreach (var z in FindObjectsByType<InteractionZone>(FindObjectsSortMode.None))
                if (z.action == ZoneAction.Talk && z.talkName == npcName) return;

            // PlayerNameTag.overrideName 으로 NPC(GM_Char / Jiny_Char) 찾기
            Transform target = null;
            foreach (var tag in FindObjectsByType<PlayerNameTag>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (tag != null && tag.overrideName == npcName) { target = tag.transform; break; }
            if (target == null) { Debug.LogWarning($"[InteractionZone] NPC '{npcName}' 못 찾음 → '{label}' 버튼 생략"); return; }

            var zone = target.gameObject.AddComponent<InteractionZone>();
            zone.action = ZoneAction.Talk;
            zone.buttonLabel = label;
            zone.talkName = npcName;
            zone.talkMode = mode;
            zone.activateRadius = 4f;
            zone.buttonHeight = 4f;   // NPC 머리 위. 필요시 조절
        }
    }
}
