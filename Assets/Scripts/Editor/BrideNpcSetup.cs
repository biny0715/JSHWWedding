// BrideNpcSetup.cs (Editor)
// Tools/JSHW/Setup Bride NPC — Jisu 플레이어 프리팹 기반의 "정적 Idle NPC"를 BrideNPC 위치에 생성.
//  - Wedding 씬을 연 상태에서 실행. 씬에 'BrideNPC' 오브젝트가 있어야 함(그 위치/방향에 NPC 배치).
//  - Jisu 인스턴스에서 플레이어/네트워킹 컴포넌트(PhotonView·이동·NavMeshAgent·네임태그·커스텀적용)를 제거 →
//    GASTRO 부위(자식)의 Animator만 남아 런타임에 Idle 상태로 서 있음(Speed 미설정 → Idle 블렌드).
//  - 부위 Animator를 비니/지니(CharacterAssembler)와 동일하게 정규화(루트 Animator 제거 + 모든 부위
//    같은 컨트롤러 + 루트모션 off)해 Idle을 동기화. (Jisu 플레이어 프리팹은 부위별 설정이 제각각이라
//    바디만 따로 노는 문제가 있었음)
//  - 재실행하면 기존 Jisu_NPC를 갈아끼움.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JSHWWedding.Customization.EditorTools
{
    public static class BrideNpcSetup
    {
        const string JisuPath = "Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Resources/Jisu.prefab";
        const string BrideName = "신부\n박지수";   // 머리 위 이름표(2줄: 역할 + 이름)

        // 제거 순서: RequireComponent 의존(PhotonView 를 요구하는 것들)을 먼저, PhotonView 는 마지막.
        static readonly string[] StripOrder = {
            "PlayerCustomizationApply", "PlayerNameTag", "PlayerClickToMove",
            "PlayerManager", "PlayerAnimatorManager",
            "NavMeshAgent", "CharacterController", "Rigidbody",
            "PhotonTransformView", "PhotonAnimatorView",   // 네트워크 동기화 관측 컴포넌트
            "PhotonView",
        };

        [MenuItem("Tools/JSHW/Setup Bride NPC")]
        public static void Setup()
        {
            var bride = GameObject.Find("BrideNPC");
            if (bride == null) { Debug.LogError("[BrideNPC] 활성 씬에 'BrideNPC' 오브젝트가 없습니다. Wedding 씬을 열고 실행하세요."); return; }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(JisuPath);
            if (prefab == null) { Debug.LogError("[BrideNPC] Jisu 프리팹을 찾을 수 없음: " + JisuPath); return; }

            var old = bride.transform.Find("Jisu_NPC");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, bride.scene);
            PrefabUtility.UnpackPrefabInstance(npc, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            npc.name = "Jisu_NPC";
            npc.transform.SetParent(bride.transform, false);
            npc.transform.localPosition = Vector3.zero;
            npc.transform.localRotation = Quaternion.identity;

            int removed = 0;
            foreach (var typeName in StripOrder) removed += StripByTypeName(npc, typeName);

            // 부위 Animator 정규화 — 비니/지니(CharacterAssembler)와 동일하게 통일한다:
            //  · 루트 Animator(플레이어 프리팹의 전신 컨트롤러)는 제거 → 부위별 Animator만 남김(비니/지니엔 루트 Animator 없음)
            //  · 모든 부위 Animator 를 같은 컨트롤러(Character_Movement) + 루트모션 off 로 통일 → 전 부위 Idle 동기화
            // (Jisu 플레이어 프리팹은 부위별 Animator 설정이 제각각이라 바디만 따로 움직이던 문제 해결)
            var manifest = AssetDatabase.LoadAssetAtPath<GastroPartManifest>("Assets/Customization/GastroPartManifest.asset");
            var ctrl = manifest != null ? manifest.characterController : null;
            if (ctrl == null) Debug.LogWarning("[BrideNPC] GastroPartManifest.characterController 를 못 찾음 — 부위 컨트롤러 통일 생략(동기화 안 될 수 있음)");
            int animNormalized = 0, rootAnimRemoved = 0;
            foreach (var anim in npc.GetComponentsInChildren<Animator>(true))
            {
                if (anim.gameObject == npc) { Object.DestroyImmediate(anim); rootAnimRemoved++; continue; }   // 루트 Animator 제거
                if (ctrl != null) anim.runtimeAnimatorController = ctrl;
                anim.applyRootMotion = false;
                animNormalized++;
            }

            // 머리 위 이름표(고정 이름) — PhotonView 없이도 overrideName 으로 표시
            var nameTag = npc.AddComponent<PlayerNameTag>();
            nameTag.overrideName = BrideName;

            string left = "";
            foreach (var c in npc.GetComponents<Component>()) left += c.GetType().Name + " ";

            EditorSceneManager.MarkSceneDirty(npc.scene);
            Selection.activeGameObject = npc;
            Debug.Log($"[BrideNPC] 'Jisu_NPC' 생성 완료(BrideNPC 자식). 제거된 컴포넌트 {removed}개, " +
                $"루트 Animator 제거 {rootAnimRemoved}개, 부위 Animator 정규화 {animNormalized}개.\n" +
                $"루트 남은 컴포넌트: {left}\n→ 런타임에 Idle 자세로 서 있습니다. 씬 저장하세요.");
        }

        static int StripByTypeName(GameObject go, string typeName)
        {
            int n = 0;
            foreach (var c in go.GetComponents<Component>())
                if (c != null && c.GetType().Name == typeName) { Object.DestroyImmediate(c); n++; }
            return n;
        }
    }
}
