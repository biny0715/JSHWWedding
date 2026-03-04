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
		public Vector3 SpawnPosition;

		#endregion

		#region Private Fields

		private GameObject instance;

        [Tooltip("The prefab to use for representing the player")]
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


			if (playerPrefab == null) { // #Tip Never assume public properties of Components are filled up properly, always check and inform the developer of it.

				Debug.LogError("<Color=Red><b>Missing</b></Color> playerPrefab Reference. Please set it up in GameObject 'Game Manager'", this);
			} else {


				if (PhotonNetwork.InRoom && PlayerManager.LocalPlayerInstance == null)
				{
					Vector3 spawnPos = GetSpawnPosition();

					PhotonNetwork.Instantiate(
						this.playerPrefab.name,
						spawnPos,
						Quaternion.identity,
						0
					);
				}
				else{

					Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
				}


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

		        Vector3 spawnPos = GetSpawnPosition();

		        PhotonNetwork.Instantiate(
			        this.playerPrefab.name,
			        spawnPos,
			        Quaternion.identity,
			        0
		        );
	        }
        }
        private Vector3 GetSpawnPosition()
        {
	        Vector3 basePos = SpawnPosition;

	        // 플레이어 수에 따라 살짝 분산
	        float offset = PhotonNetwork.LocalPlayer.ActorNumber * 1.5f;

	        Vector3 spawnPos = basePos + new Vector3(offset, 0f, offset);

	        // NavMesh 위로 스냅
	        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
	        {
		        return hit.position;
	        }

	        return basePos;
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