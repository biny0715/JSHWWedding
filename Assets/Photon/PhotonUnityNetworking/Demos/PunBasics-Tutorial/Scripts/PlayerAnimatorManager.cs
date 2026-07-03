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
        private float baseSpeed = 3.5f;   // NavMeshAgent 기본 속도 — 달리기 가속 시 애니 배속 기준

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent != null && agent.speed > 0.01f) baseSpeed = agent.speed;
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

            // 달리기 가속(기본 속도 초과) 비율만큼 재생을 배속해 발이 미끄러지지 않게 한다.
            // 원격 플레이어도 syncedSpeed(velocity)로 같은 값을 계산하므로 별도 동기화 불필요.
            float animSpeed = Mathf.Max(1f, speed / baseSpeed);

            foreach (Animator anim in animators)
            {
                if (anim != null)   // 파괴된 부위(null) 건너뜀 → 루프 중단 방지
                {
                    anim.SetFloat("Speed", speed);
                    anim.speed = animSpeed;
                }
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