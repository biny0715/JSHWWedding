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

#if UNITY_WEBGL && !UNITY_EDITOR
        // 유니티 → 웹: "로비가 이름 받을 준비 됐다" 신호 (jslib)
        [DllImport("__Internal")] private static extern void WeddingLobbyReady();
        // 유니티 → 웹: "입장 처리 시작했다(접속 중)" 신호 (jslib)
        [DllImport("__Internal")] private static extern void WeddingEntering();
        // 유니티 → 웹: "Wedding(예식장) 3D 씬 로드 완료" 신호 (jslib)
        [DllImport("__Internal")] private static extern void WeddingSceneReady();
        // 유니티 → 웹: Photon 연결 끊김 + 사유 (jslib)
        [DllImport("__Internal")] private static extern void WeddingDisconnected(string cause);
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
            Debug.Log("[WebLobbyBridge] 오버레이 닫힘 → 이동 잠금 해제");
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
            Debug.Log($"[WebLobbyBridge] 입장 시작 - NickName: {name}");

#if UNITY_WEBGL && !UNITY_EDITOR
            WeddingEntering();
#endif
            launcher.Connect();   // Photon 접속 → 방 입장 → OnJoinedRoom 에서 Wedding 씬 로드
        }
    }
}
