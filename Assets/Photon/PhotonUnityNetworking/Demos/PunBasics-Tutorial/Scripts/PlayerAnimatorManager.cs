using UnityEngine;
using Photon.Pun;

namespace Photon.Pun.Demo.PunBasics
{
    public class PlayerAnimatorManager : MonoBehaviourPun
    {
        [SerializeField] private float directionDampTime = 0.25f;

        private Animator animator;
        private CharacterController characterController;

        void Start()
        {
            animator = GetComponent<Animator>();
            characterController = GetComponent<CharacterController>();
        }

        void Update()
        {
            // 로컬 플레이어만 애니메이션 입력 갱신
            if (photonView.IsMine == false && PhotonNetwork.IsConnected == true) return;
            if (!animator) return;

            // 점프 트리거는 유지(원하면 나중에 모바일 버튼으로 바꿔도 됨)
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Base Layer.Run"))
            {
                if (Input.GetButtonDown("Fire2"))
                    animator.SetTrigger("Jump");
            }

            // ✅ 핵심: 입력축이 아니라 "실제 이동"으로 Speed/Direction 계산
            Vector3 velocity = Vector3.zero;

            if (characterController != null)
                velocity = characterController.velocity;
            else
            {
                var rb = GetComponent<Rigidbody>();
                if (rb != null) velocity = rb.linearVelocity;
            }

            // y(점프/중력) 제외한 평면 속도
            velocity.y = 0f;

            float speed01 = Mathf.Clamp01(velocity.magnitude); // 필요하면 나중에 /maxSpeed로 정규화
            animator.SetFloat("Speed", speed01 * speed01);

            // Direction은 로컬 공간 x 성분으로 대체(좌/우 회전 블렌딩용)
            Vector3 localVel = transform.InverseTransformDirection(velocity);
            animator.SetFloat("Direction", localVel.x, directionDampTime, Time.deltaTime);
        }
    }
}