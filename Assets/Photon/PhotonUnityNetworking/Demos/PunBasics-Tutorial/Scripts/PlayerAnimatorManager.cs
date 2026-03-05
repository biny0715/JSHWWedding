using UnityEngine;
using Photon.Pun;

namespace Photon.Pun.Demo.PunBasics
{
    public class PlayerAnimatorManager : MonoBehaviourPun
    {
        [SerializeField] private float directionDampTime = 0.25f;

        Animator animator;
        Vector3 lastPosition;

        void Start()
        {
            animator = GetComponent<Animator>();
            lastPosition = transform.position;
        }

        void Update()
        {
            float speed = (transform.position - lastPosition).magnitude / Time.deltaTime;
            animator.SetFloat("Speed", speed);

            lastPosition = transform.position;
        }
    }
}