using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Unity.Cinemachine;

namespace Photon.Pun.Demo.PunBasics
{
    public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable
    {
        public static GameObject LocalPlayerInstance;
        [Header("Player")]
        public float Health = 1f;

        [Header("UI")]
        [SerializeField] private GameObject playerUiPrefab;

        private bool leavingRoom;

#if UNITY_5_4_OR_NEWER
        void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
#endif

        void Awake()
        {
            if (photonView.IsMine)
            {
                LocalPlayerInstance = this.gameObject;
            }
        }
        void Start()
        {
            if (!photonView.IsMine)
                return;

            SetupCamera();
        }

        void SetupCamera()
        {
            CinemachineCamera cam = FindFirstObjectByType<CinemachineCamera>();

            if (cam != null)
            {
                cam.Follow = transform;
            }
        }
        
        private void OnDestroy()
        {
            if (photonView != null && photonView.IsMine)
            {
                if (LocalPlayerInstance == this.gameObject)
                    LocalPlayerInstance = null;
            }
        }

        public override void OnLeftRoom()
        {
            this.leavingRoom = false;
        }

#if UNITY_5_4_OR_NEWER
        void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode loadingMode)
        {
            CalledOnLevelWasLoaded(scene.buildIndex);
        }
#endif

        void CalledOnLevelWasLoaded(int level)
        {
            // 바닥이 없으면 중앙으로 이동(샘플 안전장치)
            if (!Physics.Raycast(transform.position, -Vector3.up, 5f))
            {
                transform.position = new Vector3(0f, 5f, 0f);
            }

            if (playerUiPrefab != null)
            {
                var uiGo = Instantiate(playerUiPrefab);
                uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
            }
        }

        // Health만 네트워크로 동기화
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(this.Health);
            }
            else
            {
                this.Health = (float)stream.ReceiveNext();
            }
        }
    }
}
