// NpcDialogCamera.cs
// NPC 대화 시 해당 NPC를 바라보는 카메라로 "즉시 컷" 전환한다.
//  - 방식: CinemachineBrain 을 잠깐 끄고 메인 카메라를 NPC 정면에 직접 배치 → 블렌드 없이 즉시 전환.
//    대화창이 닫히면(웹의 OnVenueOverlayClosed → Unfocus) 브레인을 다시 켜 게임플레이 뷰로 복귀.
//  - 씬 배치 불필요: Wedding 씬 로드 시 자동 생성(부트스트랩). InteractionZone(Talk) 이 Focus 를 호출.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using Photon.Pun.Demo.PunBasics;   // PlayerClickToMove(플레이어 식별용)

namespace JSHWWedding
{
    public class NpcDialogCamera : MonoBehaviour
    {
        // 구도(정면 클로즈업) — NPC 스케일 0.5 기준. 보고 어긋나면 이 값만 조절하면 된다.
        const float HeadHeight = 1.0f;   // NPC 발밑(transform) 기준 얼굴 높이(m)
        const float Distance   = 4f;     // NPC 정면에서 떨어질 거리(m) — 더 뒤에서
        const float CamRaise   = 0.3f;   // 카메라를 얼굴보다 살짝 위로(m)

        static NpcDialogCamera instance;
        /// <summary>NPC 대화 카메라가 활성인 동안 true (네비게이션 등 숨김 판단용).</summary>
        public static bool Active { get; private set; }
        Camera cam;
        CinemachineBrain brain;
        bool focused;
        int focusFrame;
        Vector3 lockPos;
        Quaternion lockRot;
        // 대화 중 숨긴 플레이어 렌더러(내 캐릭터+다른 하객, 로컬 화면만) — 종료 시 복원
        readonly List<Renderer> hiddenRenderers = new List<Renderer>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            SceneManager.sceneLoaded += (s, m) => { if (s.name == "Wedding") Spawn(); };
            if (SceneManager.GetActiveScene().name == "Wedding") Spawn();
        }
        static void Spawn()
        {
            if (instance != null) return;
            instance = new GameObject("NpcDialogCamera").AddComponent<NpcDialogCamera>();
        }

        void Awake() { instance = this; Active = false; }
        void OnDestroy() { if (instance == this) instance = null; }

        /// <summary>해당 NPC 정면을 바라보는 뷰로 즉시 컷 전환. headHeight 로 NPC별 바라볼 높이(m) 지정.</summary>
        public static void Focus(Transform npc, float headHeight = HeadHeight) { if (instance != null) instance.DoFocus(npc, headHeight); }
        /// <summary>게임플레이 카메라로 복귀(대화 중이 아니면 무시).</summary>
        public static void Unfocus() { if (instance != null) instance.DoUnfocus(); }

        void DoFocus(Transform npc, float headHeight)
        {
            if (npc == null) return;
            cam = Camera.main;
            if (cam == null) return;
            brain = cam.GetComponent<CinemachineBrain>();

            // NPC 정면(그가 바라보는 방향 앞쪽)에서 얼굴을 바라본다.
            Vector3 head = npc.position + Vector3.up * headHeight;
            Vector3 fwd = npc.forward; fwd.y = 0f;
            fwd = (fwd.sqrMagnitude < 1e-4f) ? Vector3.forward : fwd.normalized;
            lockPos = head + fwd * Distance + Vector3.up * CamRaise;
            lockRot = Quaternion.LookRotation((head - lockPos).normalized, Vector3.up);

            if (brain != null) brain.enabled = false;   // 브레인 정지 → 블렌드 없이 즉시 컷
            cam.transform.SetPositionAndRotation(lockPos, lockRot);
            if (focused) ShowPlayers();                 // 연속 Focus 방어(이전 숨김분 복원 후 다시 숨김)
            HidePlayers();                              // 카메라를 가리는 캐릭터들 숨김
            focused = true;
            Active = true;
            focusFrame = Time.frameCount;
        }

        void DoUnfocus()
        {
            if (!focused) return;
            focused = false;
            Active = false;
            ShowPlayers();
            if (brain != null) brain.enabled = true;     // 게임플레이 카메라로 복귀
        }

        // 플레이어 캐릭터(내 캐릭터 + 다른 하객)의 렌더러를 꺼서 NPC 뷰가 가려지지 않게 한다.
        // 렌더링만 로컬로 끄는 것이라 다른 사람 화면에는 영향 없음. NPC(PlayerClickToMove 없음)는 대상 아님.
        // 이름표(월드 TMP)도 MeshRenderer 라 함께 숨겨진다.
        void HidePlayers()
        {
            hiddenRenderers.Clear();
            foreach (var move in FindObjectsByType<PlayerClickToMove>(FindObjectsSortMode.None))
                foreach (var r in move.GetComponentsInChildren<Renderer>())
                    if (r != null && r.enabled) { r.enabled = false; hiddenRenderers.Add(r); }
        }

        void ShowPlayers()
        {
            foreach (var r in hiddenRenderers)
                if (r != null) r.enabled = true;   // 대화 중 파괴(퇴장)된 렌더러는 건너뜀
            hiddenRenderers.Clear();
        }

        void LateUpdate()
        {
            // 대화 중엔 카메라를 NPC 뷰에 고정(다른 요소가 건드려도 유지)
            if (focused && cam != null) cam.transform.SetPositionAndRotation(lockPos, lockRot);

#if UNITY_EDITOR
            // 에디터 테스트: 웹 닫힘 신호가 없으니 클릭(이동) 또는 Esc 로 원래 카메라 복귀.
            // 말걸기를 누른 그 프레임은 제외(즉시 풀리지 않게).
            if (focused && Time.frameCount != focusFrame &&
                (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0)))
                DoUnfocus();
#endif
        }
    }
}
