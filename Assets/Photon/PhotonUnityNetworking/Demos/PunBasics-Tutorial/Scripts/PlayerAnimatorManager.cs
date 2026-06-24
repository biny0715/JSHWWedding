using UnityEngine;
using Photon.Pun;
using UnityEngine.AI;
using System.Collections.Generic;

namespace Photon.Pun.Demo.PunBasics
{
    public class PlayerAnimatorManager : MonoBehaviourPun, IPunObservable
    {
        private List<Animator> animators = new List<Animator>();
        private NavMeshAgent agent;

        private float syncedSpeed;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animators.AddRange(GetComponentsInChildren<Animator>());
        }

        // 커스텀으로 부위(Animator)가 교체된 뒤 다시 수집한다. CharacterAssembler 가 SendMessage 로 호출.
        public void RefreshAnimators()
        {
            animators.Clear();
            animators.AddRange(GetComponentsInChildren<Animator>());
        }

        void Update()
        {
            float speed;

            if (photonView.IsMine)
            {
                speed = agent.velocity.magnitude;
                syncedSpeed = speed;
            }
            else
            {
                speed = syncedSpeed;
            }

            foreach (Animator anim in animators)
            {
                if (anim != null) anim.SetFloat("Speed", speed);   // 파괴된 부위(null) 건너뜀 → 루프 중단 방지
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(syncedSpeed);
            }
            else
            {
                syncedSpeed = (float)stream.ReceiveNext();
            }
        }
    }
}