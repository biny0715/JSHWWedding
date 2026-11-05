// AdminViewerNetwork.cs
// 관리자(아이패드) 뷰 전용 Photon 접속기 — 아바타를 만들지 않고 하객들이 있는 방에 '관전자'로만 들어간다.
//  - 방을 새로 만들지 않는다: 방을 만든 쪽(방장)이 Wedding 씬 동기화(curScn)를 정하므로,
//    관리자가 방을 만들면 뒤에 들어온 하객이 Wedding 씬을 로드하지 못한다. 하객이 없으면 기다렸다가 다시 시도.
//  - AutomaticallySyncScene=false: 방의 Wedding 씬을 따라 로드하지 않고 지금 씬(WeddingAdmin)에 머문다.
using System.Collections;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace JSHWWedding.Admin
{
    public class AdminViewerNetwork : MonoBehaviourPunCallbacks
    {
        const string GameVersion = "1";        // Launcher.gameVersion 과 같아야 하객과 같은 방을 찾는다
        public const string AdminPropKey = "adm"; // 관리자 표식(다른 관리자 기기를 하객 목록에서 빼는 용도)

        [SerializeField] float retrySeconds = 5f;

        public static string Status { get; private set; } = "준비 중";

        static void SetStatus(string s)
        {
            if (Status == s) return;
            Status = s;
            Debug.Log("[AdminViewer] " + s);   // 화면 UI 가 없으므로 상태는 로그로 확인(Xcode 콘솔)
        }

        void Awake()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;   // 아이패드를 켜둔 채 관전
            Application.targetFrameRate = 60;
            PhotonNetwork.AutomaticallySyncScene = false;
        }

        void Start() => Connect();

        void Connect()
        {
            if (PhotonNetwork.IsConnected)
            {
                if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom) Join();
                return;
            }
            SetStatus("서버 연결 중…");
            PhotonNetwork.NickName = "관리자";
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { AdminPropKey, true } });
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = GameVersion;   // 하객 Launcher 와 같은 순서로 설정
        }

        void Join()
        {
            if (!Status.Contains("기다리는 중")) SetStatus("예식장 찾는 중…");   // 대기 중 5초마다 재시도할 땐 로그 반복 안 함
            PhotonNetwork.JoinRandomRoom();
        }

        public override void OnConnectedToMaster() => Join();

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            SetStatus("아직 입장한 하객이 없어요 — 기다리는 중");
            StartCoroutine(RetryJoin());
        }

        IEnumerator RetryJoin()
        {
            yield return new WaitForSeconds(retrySeconds);
            if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom) Join();
        }

        public override void OnJoinedRoom() => SetStatus("관전 중");

        public override void OnDisconnected(DisconnectCause cause)
        {
            SetStatus($"연결 끊김({cause}) — 다시 연결 중");
            StartCoroutine(RetryConnect());
        }

        IEnumerator RetryConnect()
        {
            yield return new WaitForSeconds(retrySeconds);
            if (!PhotonNetwork.IsConnected) Connect();
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused && !PhotonNetwork.IsConnected) Connect();
        }

        public static bool IsAdmin(Player p) =>
            p != null && p.CustomProperties.TryGetValue(AdminPropKey, out var v) && v is bool b && b;
    }
}
