// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Launcher.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Networking Demos
// </copyright>
// <summary>
//  Used in "PUN Basic tutorial" to handle typical game management requirements
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.SceneManagement;

using Photon.Realtime;

namespace Photon.Pun.Demo.PunBasics
{
	#pragma warning disable 649

	/// <summary>
	/// Game manager.
	/// Connects and watch Photon Status, Instantiate Player
	/// Deals with quiting the room and the game
	/// Deals with level loading (outside the in room synchronization)
	/// </summary>
	public class GameManager : MonoBehaviourPunCallbacks
    {

		#region Public Fields

		static public GameManager Instance;
		[Tooltip("고정 스폰 지점(입구). 접속 순서/ActorNumber 와 무관하게 모든 플레이어가 이 위치에서 생성된다.")]
		public Vector3 SpawnPosition;

		[Header("스폰")]
		[Tooltip("스폰 지점을 NavMesh 표면에 맞출 때 허용하는 최대 거리(m). 이보다 멀면 보정하지 않고 지정 좌표 그대로 사용.")]
		[SerializeField] private float navMeshSnapDistance = 0.5f;
		[Tooltip("스폰 진단 로그 출력")]
		[SerializeField] private bool spawnDebugLog = true;

		#endregion

		#region Private Fields

		private GameObject instance;

        [Tooltip("The prefab to use for representing the player (커스텀 미설정 시 폴백)")]
        [SerializeField]
        private GameObject playerPrefab;

        #endregion

        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        void Start()
		{
			Instance = this;

			// in case we started this demo with the wrong scene being active, simply load the menu scene
			if (!PhotonNetwork.IsConnected)
			{
				SceneManager.LoadScene("Lobby");
				return;
			}

			if (PhotonNetwork.InRoom && PlayerManager.LocalPlayerInstance == null)
			{
				SpawnPlayer(GetSpawnPosition());
			}
			else
			{
				Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
			}
		}

		/// <summary>
		/// MonoBehaviour method called on GameObject by Unity on every frame.
		/// </summary>
		void Update()
		{
			// "back" button of phone equals "Escape". quit app if that's pressed
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				QuitApplication();
			}
		}

        #endregion

        #region Photon Callbacks

        public override void OnJoinedRoom()
        {
	        if (PlayerManager.LocalPlayerInstance == null)
	        {
		        Debug.LogFormat("Spawning LocalPlayer from {0}", SceneManagerHelper.ActiveSceneName);
		        SpawnPlayer(GetSpawnPosition());
	        }
        }

        // WebLobbyBridge(Assembly-CSharp)가 입장 직전 채워준다. GameManager 는 별도 어셈블리(Demos asmdef)라
        // JSHWWedding.* 타입을 참조할 수 없어 '원시 타입'(string, object[])만 주고받는다.
        public static string CustomPrefabName;            // "MaleCharacter"/"FemaleCharacter" (없으면 폴백)
        public static object[] CustomInstantiationData;   // 부위 11개 int (없으면 커스텀 없음)

        // 성별 프리팹 + 커스텀 룩(instantiationData)으로 스폰. 모든 클라이언트가 InstantiationData 를 읽어 동일 조립.
        // 위치는 로컬 플레이어(소유자)만 한 번 계산해 Instantiate 로 보낸다 → 다른 클라이언트는 그 값을 그대로 받는다.
        private bool localSpawnRequested;   // Start/OnJoinedRoom 중복 스폰 방지(같은 GameManager 수명 안)
        private void SpawnPlayer(Vector3 spawnPos)
        {
            if (localSpawnRequested || PlayerManager.LocalPlayerInstance != null)
            {
                Debug.LogWarning("[Spawn] 로컬 플레이어가 이미 있어 스폰을 건너뜀");
                return;
            }
            localSpawnRequested = true;
            string prefabName = !string.IsNullOrEmpty(CustomPrefabName)
                ? CustomPrefabName
                : (playerPrefab != null ? playerPrefab.name : "FemaleCharacter");
            object[] data = CustomInstantiationData;
            PhotonNetwork.Instantiate(prefabName, spawnPos, Quaternion.identity, 0, data);
        }

        // 고정 스폰: 접속 순서/ActorNumber 와 무관하게 항상 SpawnPosition 한 곳(입퇴장 반복으로 ActorNumber 가 커져도 동일).
        // NavMesh 표면에 맞추는 보정만 하며, 보정 거리가 navMeshSnapDistance 를 넘으면 지정 좌표를 그대로 쓴다.
        private Vector3 GetSpawnPosition()
        {
	        var r = ComputeSpawnPosition(SpawnPosition, navMeshSnapDistance);
	        if (spawnDebugLog)
		        Debug.Log($"[Spawn] actor={PhotonNetwork.LocalPlayer?.ActorNumber} base={SpawnPosition:F2} " +
		                  $"navmesh={r.position:F2} snapDelta={(r.position - SpawnPosition).magnitude:F2}m{(r.fallback ? " FALLBACK(NavMesh 없음 → 지정 좌표 그대로)" : "")}");
	        return r.position;
        }

        public struct SpawnResult { public Vector3 position; public bool fallback; }

        /// <summary>스폰 위치 계산(순수 함수 — 에디터 검증에서도 호출). 입력은 기준 좌표뿐이라 누가 몇 번째로 들어와도 같은 결과.</summary>
        public static SpawnResult ComputeSpawnPosition(Vector3 basePos, float snapDistance)
        {
	        if (UnityEngine.AI.NavMesh.SamplePosition(basePos, out UnityEngine.AI.NavMeshHit hit, Mathf.Max(0.01f, snapDistance), UnityEngine.AI.NavMesh.AllAreas))
		        return new SpawnResult { position = hit.position };
	        return new SpawnResult { position = basePos, fallback = true };
        }
        /// <summary>
        /// Called when a Photon Player got connected. We need to then load a bigger scene.
        /// </summary>
        /// <param name="other">Other.</param>
        public override void OnPlayerEnteredRoom(Player other)
        {
	        Debug.Log("OnPlayerEnteredRoom() " + other.NickName);
	        // Room/scene resizing based on player count was removed.
        }

        public override void OnPlayerLeftRoom(Player other)
        {
	        Debug.Log("OnPlayerLeftRoom() " + other.NickName);
	        // Room/scene resizing based on player count was removed.
        }


		/// <summary>
		/// Called when the local player left the room. We need to load the launcher scene.
		/// </summary>
		public override void OnLeftRoom()
		{
			SceneManager.LoadScene("Lobby");
		}


		#endregion

		#region Public Methods

		public void LeaveRoom()
		{
			PhotonNetwork.LeaveRoom();
		}

		public void QuitApplication()
		{
			Application.Quit();
		}

		#endregion

		#region Private Methods

		void LoadArena()
		{
			if ( ! PhotonNetwork.IsMasterClient )
			{
				Debug.LogError( "PhotonNetwork : Trying to Load a level but we are not the master Client" );
				return;
			}

			Debug.LogFormat( "PhotonNetwork : Loading Level : {0}", PhotonNetwork.CurrentRoom.PlayerCount );

			PhotonNetwork.LoadLevel("PunBasics-Room for "+PhotonNetwork.CurrentRoom.PlayerCount);
		}

		#endregion

	}

}
