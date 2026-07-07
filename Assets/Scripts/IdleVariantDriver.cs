// IdleVariantDriver.cs
// 멀티 파트 캐릭터(바디/눈/눈썹/머리 등 Animator 여러 개)의 아이들 변형을 캐릭터 단위로 지휘한다.
//  - 파트별 Animator 가 각자 랜덤을 굴리면 부위마다 다른 idle 이 재생되므로,
//    루트에서 한 번만 굴려 모든 파트에 같은 변형을 같은 프레임에 CrossFade → 동작·위상까지 동기화.
//  - 변형 종료 후 Idle_Breathing 복귀는 컨트롤러의 Exit Time 전이가 처리(동시 진입 → 동시 복귀).
//  - 부위 교체 시 CharacterAssembler 가 SendMessage("RefreshAnimators")를 보내므로 자동 재수집.
//  - 부착은 IdleVariantSetup(Tools > JSHW > Setup Idle Variants) 이 캐릭터 프리팹/씬 캐릭터 루트에 수행.
using System.Collections.Generic;
using UnityEngine;

namespace JSHWWedding
{
    public class IdleVariantDriver : MonoBehaviour
    {
        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly string[] Variants = { "Idle_Look_Around", "Idle_Relaxed" };
        const string ControllerName = "Character_Movement";
        const string IdleStateName = "Idle_Breathing";

        [Tooltip("변형 재생 간격(초) 랜덤 범위")]
        public Vector2 interval = new Vector2(8f, 16f);

        readonly List<Animator> parts = new List<Animator>();
        float nextAt;
        bool dirty = true;

        void OnEnable() { dirty = true; Reschedule(); }

        /// <summary>부위 교체 후 재수집 (CharacterAssembler 의 SendMessage("RefreshAnimators") 수신)</summary>
        public void RefreshAnimators() { dirty = true; }

        void Update()
        {
            if (Time.time < nextAt) return;

            if (dirty) Rescan();
            if (parts.Count == 0) { nextAt = Time.time + 5f; return; }
            if (!AllIdle()) { nextAt = Time.time + 1f; return; }   // 이동/변형 재생 중이면 잠시 후 재시도

            string variant = Variants[Random.Range(0, Variants.Length)];
            foreach (var a in parts)
                if (a != null && a.isActiveAndEnabled)
                    a.CrossFadeInFixedTime(variant, 0.25f, 0, 0f);
            Reschedule();
        }

        void Reschedule() { nextAt = Time.time + Random.Range(interval.x, interval.y); }

        void Rescan()
        {
            dirty = false;
            parts.Clear();
            foreach (var a in GetComponentsInChildren<Animator>())
                if (a.runtimeAnimatorController != null && a.runtimeAnimatorController.name == ControllerName)
                    parts.Add(a);
        }

        bool AllIdle()
        {
            foreach (var a in parts)
            {
                if (a == null || !a.isActiveAndEnabled) return false;
                if (a.GetFloat(SpeedHash) > 0.1f) return false;
                if (a.IsInTransition(0)) return false;
                if (!a.GetCurrentAnimatorStateInfo(0).IsName(IdleStateName)) return false;
            }
            return true;
        }
    }
}
