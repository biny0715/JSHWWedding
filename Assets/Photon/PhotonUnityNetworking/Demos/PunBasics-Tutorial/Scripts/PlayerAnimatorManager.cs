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
                anim.SetFloat("Speed", speed);
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