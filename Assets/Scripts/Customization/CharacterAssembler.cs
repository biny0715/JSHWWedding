// CharacterAssembler.cs
// GastroPartManifest 기반으로 캐릭터 루트에 부위(Meshes FBX)를 인스턴스화/스왑한다.
// 프리뷰(빈 루트)와 인게임(베이크된 부위를 가진 Male/FemaleCharacter 프리팹) 양쪽에서 사용.
//  - 각 부위 인스턴스에 Character_Movement 컨트롤러를 자동 부여(없으면 Animator 추가).
//  - 스왑/재조립 후 PlayerAnimatorManager 같은 구동기에 SendMessage("RefreshAnimators")로 알림(있을 때만).
using System.Collections.Generic;
using UnityEngine;

namespace JSHWWedding.Customization
{
    public class CharacterAssembler : MonoBehaviour
    {
        public GastroPartManifest manifest;

        [Tooltip("프리뷰용: 부위 애니메이터를 정지(speed=0)해 정자세로 고정. 스왑 시 부위(특히 눈)가 따로 움직여 어긋나는 것 방지. 인게임은 false(정상 애니).")]
        public bool freezeAnimators = false;

        readonly Dictionary<int, GameObject> equipped = new Dictionary<int, GameObject>();
        CharacterLook current;

        public CharacterLook CurrentLook => current != null ? current.Clone() : null;

        // 전체 룩 적용: 기존/베이크 부위를 모두 제거 후 룩대로 재조립
        public void ApplyFull(CharacterLook look)
        {
            if (look == null) return;
            ClearAllParts();
            current = look.Clone();
            for (int cat = 1; cat <= CharacterLook.CategoryCount; cat++)
                Equip(cat, current.Get(cat));
            NotifyAnimatorRefresh();
        }

        // 카테고리 1개만 스왑 (index < 0 = '없음' = 제거)
        public void SetPart(int catSlot, int index)
        {
            if (current == null) current = new CharacterLook();
            current.Set(catSlot, index);
            Equip(catSlot, index);
            NotifyAnimatorRefresh();
        }

        void Equip(int catSlot, int index)
        {
            Remove(catSlot);
            if (index < 0 || manifest == null) return;
            GameObject prefab = manifest.Prefab(catSlot, index);
            if (prefab == null) return;

            GameObject inst = Instantiate(prefab, transform, false);   // 부위 원본 로컬 트랜스폼(=identity) 유지
            inst.name = prefab.name;

            var anim = inst.GetComponent<Animator>();
            if (anim == null) anim = inst.AddComponent<Animator>();    // 컨트롤러 자동 부여
            if (manifest.characterController != null) anim.runtimeAnimatorController = manifest.characterController;
            anim.applyRootMotion = false;
            if (freezeAnimators) anim.speed = 0f;   // 정자세 고정: 모든 부위가 기본상태 frame0에서 멈춰 어긋남 없음

            equipped[catSlot] = inst;
        }

        void Remove(int catSlot)
        {
            if (equipped.TryGetValue(catSlot, out var go) && go != null) DestroyPart(go);
            equipped.Remove(catSlot);
        }

        // equipped + 베이크된 GASTRO 부위(자식에 Main_Rig 보유)를 모두 제거
        void ClearAllParts()
        {
            foreach (var kv in equipped) if (kv.Value != null) DestroyPart(kv.Value);
            equipped.Clear();

            var toKill = new List<GameObject>();
            foreach (Transform child in transform)
                if (child.Find("Main_Rig") != null) toKill.Add(child.gameObject);
            foreach (var go in toKill) DestroyPart(go);
        }

        void DestroyPart(GameObject go)
        {
            if (Application.isPlaying)
            {
                // Destroy는 프레임 끝에 처리됨 → 그 사이 RefreshAnimators가 죽을 부위의 Animator를
                // 다시 수집하지 않도록 즉시 비활성화(GetComponentsInChildren 기본값이 비활성 제외).
                go.SetActive(false);
                Destroy(go);
            }
            else DestroyImmediate(go);
        }

        // 부위 Animator들을 구동하는 컴포넌트(PlayerAnimatorManager.RefreshAnimators)가 같은 오브젝트에 있으면 갱신.
        // 하드 의존 없이 SendMessage 로 호출(없으면 무시). 메서드명을 RefreshAnimators 로 두면
        // SendMessage 가 이 메서드 자신도 호출해 무한 재귀가 되므로 이름을 다르게 둔다.
        void NotifyAnimatorRefresh()
        {
            SendMessage("RefreshAnimators", SendMessageOptions.DontRequireReceiver);
        }

        // ===== 에디터 검증용 =====
        [ContextMenu("Test: Apply Female Default")]
        public void TestApplyFemale() { if (manifest != null) ApplyFull(manifest.DefaultFor(1)); }
        [ContextMenu("Test: Apply Male Default")]
        public void TestApplyMale() { if (manifest != null) ApplyFull(manifest.DefaultFor(0)); }
    }
}
