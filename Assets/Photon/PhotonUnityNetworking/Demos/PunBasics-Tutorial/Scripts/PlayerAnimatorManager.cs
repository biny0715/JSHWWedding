using UnityEngine;
using Photon.Pun;
using UnityEngine.AI;
using System.Collections.Generic;

namespace Photon.Pun.Demo.PunBasics
{
    public class PlayerAnimatorManager : MonoBehaviourPun
    {
        private List<Animator> animators = new List<Animator>();
        private NavMeshAgent agent;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();

            // 모든 자식 Animator 가져오기
            Animator[] foundAnimators = GetComponentsInChildren<Animator>();

            animators.AddRange(foundAnimators);
        }

        void Update()
        {
            if (!photonView.IsMine) return;

            float speed = agent.velocity.magnitude;
            float normalizedSpeed = Mathf.Clamp01(speed / agent.speed);

            // 모든 Animator에 Speed 전달
            foreach (Animator anim in animators)
            {
                anim.SetFloat("Speed", normalizedSpeed);
            }
        }
    }
}