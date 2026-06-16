// InteractionZone.cs
// FlowerDecoZone(방명록) / PictureZone(사진앨범) 같은 오브젝트에 붙인다.
// 플레이어가 activateRadius 안으로 오면 카메라를 바라보는 빌보드 버튼(월드 캔버스)을 띄우고,
// 클릭하면 웹 창(jslib)을 열면서 UIInputLock 으로 플레이어 이동을 잠근다.
// 웹 창을 닫으면 WebLobbyBridge.OnVenueOverlayClosed() 가 잠금을 푼다.
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Pun.Demo.PunBasics;

namespace JSHWWedding
{
    public class InteractionZone : MonoBehaviour
    {
        public enum ZoneAction { Guestbook, Album }

        [Header("동작")]
        public ZoneAction action = ZoneAction.Guestbook;
        public string buttonLabel = "방명록";

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

        void Update()
        {
            if (cam == null) cam = Camera.main;
            if (player == null) player = FindLocalPlayer();
            if (player == null || cam == null) { SetShown(false); return; }

            float dist = Vector3.Distance(player.position, transform.position);
            SetShown(dist <= activateRadius && !UIInputLock.Locked);

            if (shown && ui != null)
            {
                ui.transform.position = transform.position + Vector3.up * buttonHeight;
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
            UIInputLock.Locked = true;       // 창 닫힐 때까지 이동 잠금
            SetShown(false);
            string nick = string.IsNullOrEmpty(PhotonNetwork.NickName) ? "하객" : PhotonNetwork.NickName;
            if (action == ZoneAction.Guestbook) VenueWeb.OpenGuestbook(nick);
            else VenueWeb.OpenAlbum();
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
            Gizmos.color = new Color(0.95f, 0.6f, 0.7f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, activateRadius);
        }
    }
}
