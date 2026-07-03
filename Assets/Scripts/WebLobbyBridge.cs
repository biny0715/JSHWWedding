// --------------------------------------------------------------------------------------------------------------------
// WebLobbyBridge.cs
// 로비 씬: 캐릭터 이름을 "웹(HTML/JS)" 에서 jslib 로 받고, "입장 신호" 를 기다렸다가 접속을 시작한다.
//  - WebGL 실행: 웹이 SendMessage 로 이름/입장을 전달 (Unity 자체 입력칸은 사용 안 함)
//  - 에디터 테스트: 씬의 Name InputField + Connect 버튼으로 동일하게 동작 (양쪽 모두 지원)
// 통신 규약은 같은 폴더의 Plugins/WebGL/WeddingBridge.jslib 및 웹의 venue.js 참고.
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using Photon.Realtime;
using JSHWWedding.Customization;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace JSHWWedding
{
    /// <summary>
    /// 웹 ↔ 유니티 로비 브리지.
    /// GameObject 이름을 반드시 "WebBridge" 로 두어야 웹의 SendMessage("WebBridge", ...) 가 도달한다.
    /// </summary>
    public class WebLobbyBridge : MonoBehaviourPunCallbacks
    {
        [Header("참조 (비워두면 런타임에 자동 탐색)")]
        [SerializeField] private Launcher launcher;
        [SerializeField] private TMP_InputField nameInputField;   // 에디터 테스트용 입력칸 (TMP)
        [SerializeField] private Button connectButton;            // 에디터 테스트용 버튼
        [Tooltip("WebGL 에서 숨길 유니티 자체 로비 UI(Canvas). 웹은 HTML 오버레이가 로비 담당")]
        [SerializeField] private GameObject lobbyUICanvas;

        [Header("설정")]
        [Tooltip("이름이 비어있을 때 사용할 기본 이름")]
        [SerializeField] private string defaultName = "하객";

        /// <summary>웹/에디터로부터 받은 현재 플레이어 이름</summary>
        public string PlayerName { get; private set; }

        private bool entered;   // 입장 신호 중복 방지
        private static WebLobbyBridge instance;  // 씬 전환 후 유지되는 단일 인스턴스
        public static WebLobbyBridge Active => instance;

        // 캐릭터 커스텀
        [System.NonSerialized] public LobbyPreviewController preview;  // 로비 프리뷰(런타임 자기등록)
        public static CharacterLook PendingLook;                       // 입장 시 Wedding 으로 넘길 룩(씬 전환 유지)

#if UNITY_WEBGL && !UNITY_EDITOR
        // 유니티 → 웹: "로비가 이름 받을 준비 됐다" 신호 (jslib)
        [DllImport("__Internal")] private static extern void WeddingLobbyReady();
        // 유니티 → 웹: "입장 처리 시작했다(접속 중)" 신호 (jslib)
        [DllImport("__Internal")] private static extern void WeddingEntering();
        // 유니티 → 웹: "Wedding(예식장) 3D 씬 로드 완료" 신호 (jslib)
        [DllImport("__Internal")] private static extern void WeddingSceneReady();
        // 유니티 → 웹: Photon 연결 끊김 + 사유 (jslib)
        [DllImport("__Internal")] private static extern void WeddingDisconnected(string cause);
        // 유니티 → 웹: 프리뷰 준비됨 + 카테고리별 부위 개수(JSON) (jslib)
        [DllImport("__Internal")] private static extern void PreviewReady(string countsJson);
#endif

        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // 유니티 WebGL이 페이지 전체 키보드 입력을 가로채면 HTML 입력칸(이름)에
            // 모바일 키보드로 타이핑이 안 됨(붙여넣기만 됨). 캔버스가 포커스됐을 때만
            // 캡처하도록 꺼서 HTML <input> 이 키 이벤트를 받게 한다.
            WebGLInput.captureAllKeyboardInput = false;
#endif

            // Wedding 씬 로드 완료를 웹에 알리려면 씬 전환 후에도 살아있어야 한다.
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            // 느린 기기에서 무거운 Wedding 비동기 로딩이 메인스레드를 길게 멈추면 그동안 ACK가 안 나가
            // Photon이 끊긴다(14 Pro 등). 로딩을 잘게 쪼개(프레임당 적게) 멈춤을 줄여 ACK가 흐르게 한다.
            Application.backgroundLoadingPriority = ThreadPriority.Low;

            // 참조 자동 보강 (인스펙터에서 비워두면 런타임 탐색)
            if (launcher == null) launcher = FindFirstObjectByType<Launcher>();
            if (nameInputField == null) nameInputField = FindFirstObjectByType<TMP_InputField>(FindObjectsInactive.Include);
            if (connectButton == null && nameInputField != null)
            {
                // 가장 가까운 버튼을 찾는다 (없으면 무시)
                connectButton = FindFirstObjectByType<Button>(FindObjectsInactive.Include);
            }
            if (lobbyUICanvas == null)
            {
                var c = GameObject.Find("Canvas");
                if (c != null) lobbyUICanvas = c;
            }
        }

        private void Start()
        {
            // 에디터 테스트: Connect 버튼을 누르면 입력칸 이름으로 입장
            if (connectButton != null)
            {
                connectButton.onClick.RemoveListener(OnClickConnect);
                connectButton.onClick.AddListener(OnClickConnect);
            }

            // WebGL: 유니티 자체 로비 UI 를 숨긴다(웹 HTML 오버레이가 로비 담당 → 이중 UI 방지)
            //        그리고 웹에 "준비 완료" 를 알려 이름/입장 SendMessage 를 받는다.
#if UNITY_WEBGL && !UNITY_EDITOR
            if (lobbyUICanvas != null) lobbyUICanvas.SetActive(false);
            WeddingLobbyReady();
#else
            Debug.Log("[WebLobbyBridge] 에디터 모드: Name InputField + Connect 버튼으로 테스트하세요.");
#endif
        }

        // 예식장(Wedding) 씬 로드 완료 → 웹에 알려 로딩/커튼을 걷게 한다.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Wedding")
            {
                // 씬 로드 전에 도착해 있던 화환 개수를 반영 (웹도 SceneReady 후 재전송하지만 이중 안전망)
                if (pendingWreathCount >= 0) WreathViewZone.ApplyCount(pendingWreathCount);
#if UNITY_WEBGL && !UNITY_EDITOR
                WeddingSceneReady();
#else
                Debug.Log("[WebLobbyBridge] Wedding 씬 로드됨 (에디터)");
#endif
            }
            else if (scene.name == "Lobby")
            {
                // Photon 끊김 등으로 GameManager가 로비로 되돌린 경우 → 웹에 다시 알려 이름 입력칸 재표시.
                // 재입장 가능하도록 입장 플래그도 리셋.
                entered = false;
#if UNITY_WEBGL && !UNITY_EDITOR
                WeddingLobbyReady();
#else
                Debug.Log("[WebLobbyBridge] Lobby 재진입 → 입력 재표시 신호 (에디터)");
#endif
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (instance == this) instance = null;
        }

        // 무거운 Wedding 씬 로딩(PhotonNetwork.LoadLevel) 동안 PUN 은 IsMessageQueueRunning=false 로
        // send/dispatch 를 모두 멈춘다 → ACK 도 안 나가 모바일 등 느린 기기에서 로딩이 기본 타임아웃(10s)을
        // 넘으면 ClientTimeout 으로 끊긴다. 큐가 멈춘 동안 ACK만 직접 보내 연결을 유지한다.
        private float ackTimer;
        private void Update()
        {
            if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMessageQueueRunning) return;
            ackTimer += Time.unscaledDeltaTime;
            if (ackTimer < 0.5f) return;
            ackTimer = 0f;
            var peer = PhotonNetwork.NetworkingClient?.LoadBalancingPeer;
            if (peer != null) peer.SendAcksOnly();
        }

        // ===== 웹(JS) → 유니티 : SendMessage 로 호출되는 진입점들 =====

        /// <summary>웹에서 입력한 캐릭터 이름만 먼저 전달받는다. (입장은 EnterVenue 로 별도 신호)</summary>
        public void SetPlayerName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            PlayerName = name.Trim();
            PhotonNetwork.NickName = PlayerName;
            if (nameInputField != null) nameInputField.text = PlayerName;
            Debug.Log($"[WebLobbyBridge] SetPlayerName: {PlayerName}");
        }

        /// <summary>웹의 "입장하기" 신호. 이름은 이미 SetPlayerName 으로 받았다고 가정.</summary>
        public void EnterVenue()
        {
            Enter(PlayerName);
        }

        /// <summary>이름 + 입장 신호를 한 번에 처리하고 싶을 때.</summary>
        public void EnterVenueWithName(string name)
        {
            SetPlayerName(name);
            Enter(PlayerName);
        }

        /// <summary>웹 오버레이(메뉴: 도움말/미니맵/방명록 등)가 열렸다 → 플레이어 이동 잠금.
        /// 존(InteractionZone)은 Unity가 직접 잠그지만, 웹 햄버거 메뉴에서 연 경우는 이 신호로 잠근다.
        /// 웹: unityInstance.SendMessage("WebBridge","OnVenueOverlayOpened")</summary>
        public void OnVenueOverlayOpened()
        {
            UIInputLock.Locked = true;
            Debug.Log("[WebLobbyBridge] 오버레이 열림 → 이동 잠금");
        }

        /// <summary>웹 오버레이(방명록/사진앨범/메뉴)가 닫혔다 → 플레이어 이동 잠금 해제.
        /// 웹: unityInstance.SendMessage("WebBridge","OnVenueOverlayClosed")</summary>
        public void OnVenueOverlayClosed()
        {
            UIInputLock.Locked = false;
            NpcDialogCamera.Unfocus();   // NPC 대화 카메라였다면 게임플레이 뷰로 복귀(아니면 무시)
            Debug.Log("[WebLobbyBridge] 오버레이 닫힘 → 이동 잠금 해제");
        }

        /// <summary>웹 좌상단 "길 안내" 토글 → 화면 밖 목표(예식장/신부대기실/연회장) 화살표 On/Off.
        /// 웹: unityInstance.SendMessage("WebBridge","SetGuideArrows","1"/"0")</summary>
        public void SetGuideArrows(string v)
        {
            WayfindingArrows.Enabled = (v == "1" || v == "true" || v == "on" || v == "On");
            Debug.Log("[WebLobbyBridge] 길 안내 화살표: " + (WayfindingArrows.Enabled ? "On" : "Off"));
        }

        // ===== 웹 → 유니티 : NPC(비니) 대화창 결과 =====
        // 이동 잠금 해제는 대화창 닫힘 시 웹이 보내는 OnVenueOverlayClosed 가 담당. 여기선 퀘스트 결과만 처리(추후 확장).
        /// <summary>웹 대화창에서 퀘스트 수락. 웹: SendMessage("WebBridge","OnNpcQuestAccepted")</summary>
        public void OnNpcQuestAccepted()
        {
            QuestManager.Instance?.Accept();
            Debug.Log("[WebLobbyBridge] NPC 퀘스트 수락");
        }
        /// <summary>웹 대화창에서 퀘스트 거절(다시 말걸면 처음부터 다시 제안). 웹: SendMessage("WebBridge","OnNpcQuestRejected")</summary>
        public void OnNpcQuestRejected() { Debug.Log("[WebLobbyBridge] NPC 퀘스트 거절"); }

        // ===== 웹 → 유니티 : 축하 화환(Firestore 실시간 구독) =====
        private static int pendingWreathCount = -1;   // Wedding 씬 로드 전에 도착한 개수(씬 로드 후 적용)

        /// <summary>웹: SendMessage("WebBridge","SetWreathCount","n") — CelebrateFlowers 자식 0..n-1 활성화.
        /// 웹이 씬 준비(WeddingSceneReady) 후와 화환 변경(onSnapshot) 때마다 보낸다.</summary>
        public void SetWreathCount(string countStr)
        {
            if (!int.TryParse(countStr, out int count) || count < 0) return;
            pendingWreathCount = count;
            WreathViewZone.ApplyCount(count);   // Wedding 씬이 아니면 내부에서 무시(로드 후 재적용)
        }

        // ===== 웹 → 유니티 : 캐릭터 커스텀 (로비) =====
        /// <summary>웹: SendMessage("WebBridge","SetPreviewGender","male"/"female")</summary>
        public void SetPreviewGender(string g)
        {
            if (preview != null) preview.ApplyGender(g == "male" ? 0 : 1);
        }

        /// <summary>웹: SendMessage("WebBridge","SetPreviewPart","&lt;catSlot&gt;:&lt;index&gt;") 예 "3:14", 없음 "7:-1"</summary>
        public void SetPreviewPart(string catAndIndex)
        {
            if (preview == null || string.IsNullOrEmpty(catAndIndex)) return;
            var p = catAndIndex.Split(':');
            if (p.Length == 2 && int.TryParse(p[0], out int cat) && int.TryParse(p[1], out int idx))
                preview.SetCategory(cat, idx);
        }

        /// <summary>웹: SendMessage("WebBridge","ApplyLook","g,skin,..,hats") — 전체 룩 적용 + 입장용으로 보관</summary>
        public void ApplyLook(string csv)
        {
            var look = CharacterLook.Parse(csv);
            PendingLook = look;
            if (preview != null) preview.ApplyLook(look);
        }

        /// <summary>프리뷰 준비 후 웹에 카테고리별 개수 전달(버튼 생성용).</summary>
        public void FirePreviewReady(string countsJson)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            PreviewReady(countsJson);
#else
            Debug.Log("[WebLobbyBridge] (에디터) PreviewReady: " + countsJson);
#endif
        }

        // Photon 연결이 끊기면(어느 씬이든) 사유를 웹에 전달 → 진단/안내.
        // 끊기면 GameManager가 로비로 되돌리고, OnSceneLoaded(Lobby)에서 입력칸을 재표시한다.
        public override void OnDisconnected(DisconnectCause cause)
        {
            entered = false;
            Debug.LogWarning($"[WebLobbyBridge] Photon 끊김: {cause}");
#if UNITY_WEBGL && !UNITY_EDITOR
            WeddingDisconnected(cause.ToString());
#endif
        }

        // ===== 에디터 테스트용 : Connect 버튼 =====
        public void OnClickConnect()
        {
            string n = (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
                ? nameInputField.text.Trim()
                : PlayerName;
            Enter(n);
        }

        // ===== 공통 입장 처리 =====
        private void Enter(string name)
        {
            if (entered) return;

            if (string.IsNullOrWhiteSpace(name)) name = defaultName;
            PlayerName = name;
            PhotonNetwork.NickName = name;

            if (launcher == null)
            {
                Debug.LogError("[WebLobbyBridge] Launcher 를 찾지 못했습니다. 입장 불가.");
                return;
            }

            entered = true;
            // 커스텀 룩 확정: 프리뷰가 있으면 그 현재 룩을, 없으면 웹이 ApplyLook 으로 보낸 PendingLook 유지
            if (preview != null) PendingLook = preview.CurrentLook;
            // GameManager(별도 어셈블리)로는 원시 타입만 전달 (asmdef → Assembly-CSharp 참조 불가하므로)
            if (PendingLook != null)
            {
                GameManager.CustomPrefabName = (PendingLook.gender == 0) ? "MaleCharacter" : "FemaleCharacter";
                GameManager.CustomInstantiationData = PendingLook.ToInstantiationData();
            }
            Debug.Log($"[WebLobbyBridge] 입장 시작 - NickName: {name}");

#if UNITY_WEBGL && !UNITY_EDITOR
            WeddingEntering();
#endif
            // 모바일에서 무거운 Wedding 로딩이 길어져도 버티도록 연결 타임아웃을 넉넉히(기본 10s → 60s).
            var peer = PhotonNetwork.NetworkingClient?.LoadBalancingPeer;
            if (peer != null) peer.DisconnectTimeout = 60000;

            launcher.Connect();   // Photon 접속 → 방 입장 → OnJoinedRoom 에서 Wedding 씬 로드
        }
    }
}
