// WeddingCameraIntro.cs
// 예식장(Wedding) 입장 인트로 연출: 처음엔 섬 전체가 보이는 먼 카메라(IntroCam)를 보여주다가,
// 플레이어 스폰 후 잠깐 머문 뒤 게임플레이 카메라로 부드럽게 블렌드(GTA 캐릭터 전환 느낌).
// CinemachineBrain 의 DefaultBlend(EaseInOut) 시간 동안 IntroCam -> 게임플레이캠으로 자연스럽게 줌인.
// 이 스크립트는 IntroCam(CinemachineCamera) 에 붙인다.
using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

namespace JSHWWedding
{
    public class WeddingCameraIntro : MonoBehaviour
    {
        [Tooltip("게임플레이 카메라 오브젝트 이름")]
        public string gameplayCamName = "CinemachineCamera";
        [Tooltip("플레이어 스폰 후 먼 시점에서 머무는 시간(초)")]
        public float holdSeconds = 1.2f;
        [Tooltip("Brain DefaultBlend 시간과 맞춰 인트로캠을 정리할 여유(초)")]
        public float blendSeconds = 3.5f;

        [Header("우선순위")]
        public int introPriority = 20;     // 인트로 동안 가장 높게
        public int gameplayPriority = 10;
        public int idlePriority = 0;       // 블렌드 시작 시 인트로를 낮춤

        CinemachineCamera introCam;
        CinemachineCamera gameplayCam;

        void Awake()
        {
            introCam = GetComponent<CinemachineCamera>();
            foreach (var c in FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None))
                if (c != introCam && c.name == gameplayCamName) { gameplayCam = c; break; }

            if (introCam != null) introCam.Priority = introPriority;       // 처음엔 인트로캠이 활성
            if (gameplayCam != null) gameplayCam.Priority = gameplayPriority;
        }

        IEnumerator Start()
        {
            if (introCam == null || gameplayCam == null) yield break;

            // 플레이어 스폰(=게임플레이캠 Follow 할당) 대기
            float t = 0f;
            while (gameplayCam.Follow == null && t < 15f) { t += Time.deltaTime; yield return null; }

            yield return new WaitForSeconds(holdSeconds);

            introCam.Priority = idlePriority;   // gameplay(10) > intro(0) → Brain 이 게임플레이캠으로 블렌드(줌인)
            yield return new WaitForSeconds(blendSeconds + 0.3f);

            gameObject.SetActive(false);        // 인트로캠 정리(다음 입장 시 씬 재로드로 재생성됨)
        }
    }
}
