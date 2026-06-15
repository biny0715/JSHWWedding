// --------------------------------------------------------------------------------------------------------------------
// WebLobbyBridge.cs
// 로비 씬: 캐릭터 이름을 "웹(HTML/JS)" 에서 jslib 로 받고, "입장 신호" 를 기다렸다가 접속을 시작한다.
//  - WebGL 실행: 웹이 SendMessage 로 이름/입장을 전달 (Unity 자체 입력칸은 사용 안 함)
//  - 에디터 테스트: 씬의 Name InputField + Connect 버튼으로 동일하게 동작 (양쪽 모두 지원)
// 통신 규약은 같은 폴더의 Plugins/WebGL/WeddingBridge.jslib 및 웹의 venue.js 참고.
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Pun.Demo.PunBasics;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace JSHWWedding
{
    /// <summary>
    /// 웹 ↔ 유니티 로비 브리지.
    /// GameObject 이름을 반드시 "WebBridge" 로 두어야 웹의 SendMessage("WebBridge", ...) 가 도달한다.
    /// </summary>
    public class WebLobbyBridge : MonoBehaviour
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

#if UNITY_WEBGL && !UNITY_EDITOR
        // 유니티 → 웹: "로비가 이름 받을 준비 됐다" 신호 (jslib)
        [DllImport("__Internal")] private static extern void WeddingLobbyReady();
        // 유니티 → 웹: "입장 처리 시작했다(접속 중)" 신호 (jslib)
        [DllImport("__Internal")] private static extern void WeddingEntering();
#endif

        private void Awake()
        {
            // 참조 자동 보강 (인스펙터에서 비워둬도 동작하도록)
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
