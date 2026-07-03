// PickupZone.cs
// 보물찾기 대상 지점. "빈 GameObject" 에 붙여 맵 곳곳에 숨겨 배치한다(눈에 안 보이는 탐색형).
// 퀘스트 수락 후, 플레이어가 activateRadius(인스펙터에서 조절) 안으로 오면 '줍기' 버튼이 뜨고
// 누르면 QuestManager 카운트가 올라간다. 진행도는 씬을 떠나면 리셋(메모리 유지).
// 버튼 UI 방식은 InteractionZone(월드 캔버스 빌보드 버튼)과 동일.
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun.Demo.PunBasics;

namespace JSHWWedding
{
    public class PickupZone : MonoBehaviour
    {
        [Header("표시")]
        [Tooltip("플레이어가 이 거리(m) 안에 들어오면 '줍기' 버튼 표시 — 탐색형이라 기존 존(4m)보다 가깝게")]
        public float activateRadius = 1.5f;
        [Tooltip("버튼 높이(m)")]
        public float buttonHeight = 1.2f;
        public string buttonLabel = "줍기";
        public Color buttonColor = new Color(0.357f, 0.624f, 0.702f, 1f); // sea-dk

        Transform player;
        Camera cam;
        GameObject ui;
        bool shown;
        bool picked;

        void Update()
        {
            if (picked) return;
            var qm = QuestManager.Instance;
            // 퀘스트 수락 전에는 아무것도 안 보임(탐색은 수락 후부터)
            if (qm == null || !qm.Accepted) { SetShown(false); return; }
            if (cam == null) cam = Camera.main;
            if (player == null) player = FindLocalPlayer();
            if (player == null || cam == null) { SetShown(false); return; }

            float dx = player.position.x - transform.position.x;
            float dz = player.position.z - transform.position.z;
            float horiz = Mathf.Sqrt(dx * dx + dz * dz);
            SetShown(horiz <= activateRadius && !UIInputLock.Locked && !NpcDialogCamera.Active);

            if (shown && ui != null)
            {
                ui.transform.position = transform.position + Vector3.up * buttonHeight;
                ui.transform.rotation = cam.transform.rotation;   // 빌보드(카메라 바라봄)
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
            if (picked) return;
            picked = true;
            SetShown(false);
            // GO 이름 = 아이템 키. 웹 config.js 의 questItems[키] 에서 표시 이름/그림을 찾아 획득 팝업을 띄운다.
            QuestManager.Instance?.OnPickedUp(name);
        }

        void BuildUI()
        {
            ui = new GameObject("PickupButton(" + name + ")",
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
            tmp.text = buttonLabel;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 36;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;

            ui.SetActive(false);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.35f, 0.75f, 0.9f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, activateRadius);
        }
    }
}
