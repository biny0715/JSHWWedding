// AdminOverlay.cs
// 관리자 뷰 화면 UI (IMGUI) — 접속 상태·하객 수, 하객 목록(누르면 따라가기), 하객 머리 위 이름표.
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace JSHWWedding.Admin
{
    public class AdminOverlay : MonoBehaviour
    {
        [SerializeField] AdminCameraController cam;
        [SerializeField] float nameHeight = 2.3f;   // 아바타 기준 이름표 높이(m)

        static readonly List<Rect> uiRects = new List<Rect>();   // 화면(픽셀, GUI 좌표) 기준 UI 영역 — 카메라 조작 제외용
        readonly List<(Player player, Transform avatar)> guests = new List<(Player, Transform)>();
        float nextRefresh;
        Vector2 listScroll;
        float scale = 1f;
        GUIStyle box, label, button, buttonOn, nameTag;

        /// <summary>Input 좌표(왼쪽 아래 원점)가 오버레이 UI 위인지.</summary>
        public static bool IsOverUI(Vector2 inputPos)
        {
            var guiPos = new Vector2(inputPos.x, Screen.height - inputPos.y);
            foreach (var r in uiRects) if (r.Contains(guiPos)) return true;
            return false;
        }

        void Awake()
        {
            if (cam == null) cam = FindFirstObjectByType<AdminCameraController>();
        }

        void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.5f;
            RefreshGuests();
        }

        void RefreshGuests()
        {
            guests.Clear();
            if (!PhotonNetwork.InRoom) return;

            var avatarByActor = new Dictionary<int, Transform>();
            foreach (var v in PhotonNetwork.PhotonViewCollection)
            {
                if (v == null || v.IsRoomView || v.GetComponent<PlayerNameTag>() == null) continue;
                avatarByActor[v.OwnerActorNr] = v.transform;
            }
            foreach (var p in PhotonNetwork.PlayerListOthers)
            {
                if (AdminViewerNetwork.IsAdmin(p)) continue;
                avatarByActor.TryGetValue(p.ActorNumber, out var t);
                guests.Add((p, t));
            }
            guests.Sort((a, b) => string.CompareOrdinal(a.player.NickName, b.player.NickName));
        }

        void EnsureStyles()
        {
            float s = Screen.dpi > 0 ? Screen.dpi / 160f : 1f;
            s = Mathf.Clamp(s * (Application.isMobilePlatform ? 0.85f : 1f), 1f, 2.5f);
            if (box != null && Mathf.Approximately(s, scale)) return;
            scale = s;

            var bg = MakeTex(new Color(0.12f, 0.1f, 0.12f, 0.78f));
            box = new GUIStyle(GUI.skin.box) { normal = { background = bg }, padding = new RectOffset(12, 12, 10, 10) };
            label = new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = Color.white }, wordWrap = true };
            button = new GUIStyle(GUI.skin.button) { fontSize = 15, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 10, 6, 6) };
            buttonOn = new GUIStyle(button) { normal = { background = MakeTex(new Color(0.85f, 0.5f, 0.6f, 1f)), textColor = Color.white } };
            nameTag = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(6, 6, 2, 2),
                normal = { background = MakeTex(new Color(0f, 0f, 0f, 0.55f)), textColor = Color.white },
            };
        }

        void OnGUI()
        {
            EnsureStyles();
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale, h = Screen.height / scale;
            uiRects.Clear();

            DrawNameTags();

            // 왼쪽 위: 상태 + 보기 버튼
            var info = new Rect(12, 12, 260, 150);
            Panel(info);
            GUILayout.BeginArea(info, box);
            GUILayout.Label("모바일 예식장 · 관리자 뷰", label);
            GUILayout.Label($"{AdminViewerNetwork.Status}   하객 {guests.Count}명", label);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("처음 위치", button)) cam.ResetView();
            if (GUILayout.Button(cam.IsTopDown ? "비스듬히" : "위에서", button)) cam.SetTopDown(!cam.IsTopDown);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // 오른쪽: 하객 목록 (누르면 따라가기)
            var list = new Rect(w - 232, 12, 220, Mathf.Min(h - 24, 60 + guests.Count * 36));
            Panel(list);
            GUILayout.BeginArea(list, box);
            GUILayout.Label("하객 목록 (누르면 따라가기)", label);
            listScroll = GUILayout.BeginScrollView(listScroll);
            foreach (var g in guests)
            {
                bool on = g.avatar != null && cam.Following == g.avatar;
                string text = (g.avatar == null ? "· " : "") + Name(g.player);
                if (GUILayout.Button(text, on ? buttonOn : button) && g.avatar != null)
                    cam.Follow(on ? null : g.avatar);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawNameTags()
        {
            var c = Camera.main;
            if (c == null) return;
            foreach (var g in guests)
            {
                if (g.avatar == null) continue;
                Vector3 sp = c.WorldToScreenPoint(g.avatar.position + Vector3.up * nameHeight);
                if (sp.z <= 0f) continue;
                var content = new GUIContent(Name(g.player));
                Vector2 size = nameTag.CalcSize(content);
                var pos = new Vector2(sp.x / scale, (Screen.height - sp.y) / scale);
                GUI.Label(new Rect(pos.x - size.x * 0.5f, pos.y - size.y, size.x, size.y), content, nameTag);
            }
        }

        void Panel(Rect r) => uiRects.Add(new Rect(r.x * scale, r.y * scale, r.width * scale, r.height * scale));

        static string Name(Player p) => string.IsNullOrEmpty(p.NickName) ? $"하객 {p.ActorNumber}" : p.NickName;

        static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c); t.Apply();
            return t;
        }
    }
}
