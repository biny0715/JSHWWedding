// IdleVariantSetup.cs
// Character_Movement 컨트롤러의 아이들 변형(Idle_Look_Around / Idle_Relaxed) 구성 + IdleVariantDriver 부착 도구.
//  - 컨트롤러: 변형 상태 2개(비루프 클립, 한 번 재생 후 Idle_Breathing 복귀, 이동 시 Run_Forward 인터럽트)를 보장.
//    구버전이 넣던 Breathing→변형 IdleIndex 전이/RandomIdleBehaviour/파라미터는 파트별 desync 원인이라 제거한다.
//  - 재생 트리거는 캐릭터 루트의 IdleVariantDriver 가 모든 파트 Animator 에 동시 CrossFade 로 수행.
//  - 여러 번 실행해도 안전(있으면 건너뜀).
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JSHWWedding.EditorTools
{
    public static class IdleVariantSetup
    {
        const string ControllerPath = "Assets/yaro.team/GASTRO_Character_Collection/Animations/Animation_Controllers/Character_Movement.controller";
        const string AnimDir = "Assets/yaro.team/GASTRO_Character_Collection/Animations/Other_Animations";
        const string ParamName = "IdleIndex";
        static readonly string[] VariantNames = { "Idle_Look_Around", "Idle_Relaxed" };
        static readonly string[] CharacterPrefabs =
        {
            "Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Resources/MaleCharacter.prefab",
            "Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Resources/FemaleCharacter.prefab",
            "Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Resources/Jisu.prefab",
            "Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Resources/Player.prefab",
            "Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Prefabs/BrideNPC.prefab",
            "Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Prefabs/GMNPC.prefab",
            "Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Prefabs/JinyNPC.prefab",
        };

        [MenuItem("Tools/JSHW/Setup Idle Variants")]
        public static void Apply()
        {
            if (!SetupController()) return;

            int prefabCount = 0;
            foreach (var path in CharacterPrefabs)
                if (AttachDriverToPrefab(path)) prefabCount++;

            int sceneCount = AttachDriverToSceneCharacters();

            AssetDatabase.SaveAssets();
            Debug.Log($"[IdleVariantSetup] 완료 — 컨트롤러 구성 + 드라이버 부착(프리팹 {prefabCount}건, 씬 {sceneCount}건)");
        }

        // ===== 컨트롤러 =====

        static bool SetupController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) { Debug.LogError($"[IdleVariantSetup] 컨트롤러 없음: {ControllerPath}"); return false; }

            var sm = controller.layers[0].stateMachine;
            var breathing = FindState(sm, "Idle_Breathing");
            var run = FindState(sm, "Run_Forward");
            if (breathing == null || run == null) { Debug.LogError("[IdleVariantSetup] Idle_Breathing/Run_Forward 상태를 찾지 못함"); return false; }

            EnsureVariant(sm, breathing, run, "Idle_Look_Around", new Vector3(560, 10, 0));
            EnsureVariant(sm, breathing, run, "Idle_Relaxed", new Vector3(560, 120, 0));

            // --- 구버전(SMB 랜덤) 잔재 제거 ---
            foreach (var t in breathing.transitions)
                if (t.destinationState != null && System.Array.IndexOf(VariantNames, t.destinationState.name) >= 0)
                    breathing.RemoveTransition(t);

            var keep = new List<StateMachineBehaviour>();
            bool changedSmb = false;
            foreach (var b in breathing.behaviours)
            {
                // 타입명 문자열 비교: RandomIdleBehaviour 클래스 삭제 후에도 이 코드가 컴파일되도록
                if (b == null || b.GetType().Name == "RandomIdleBehaviour")
                {
                    if (b != null) Object.DestroyImmediate(b, true);
                    changedSmb = true;
                }
                else keep.Add(b);
            }
            if (changedSmb) breathing.behaviours = keep.ToArray();

            for (int i = controller.parameters.Length - 1; i >= 0; i--)
                if (controller.parameters[i].name == ParamName)
                    controller.RemoveParameter(i);

            EditorUtility.SetDirty(controller);
            return true;
        }

        static AnimatorState FindState(AnimatorStateMachine sm, string name)
        {
            foreach (var cs in sm.states) if (cs.state.name == name) return cs.state;
            return null;
        }

        // 변형 상태 보장: 비루프 클립 + 재생 후 Breathing 복귀 + 이동 시 Run 인터럽트
        static void EnsureVariant(AnimatorStateMachine sm, AnimatorState breathing, AnimatorState run,
                                  string name, Vector3 pos)
        {
            var state = FindState(sm, name);
            if (state == null)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimDir}/{name}.anim");
                if (clip == null) { Debug.LogError($"[IdleVariantSetup] 클립 없음: {AnimDir}/{name}.anim"); return; }
                state = sm.AddState(name, pos);
                state.motion = clip;
            }

            if (!HasTransitionTo(state, run))
            {
                var toRun = state.AddTransition(run);
                toRun.hasExitTime = false;
                toRun.duration = 0.25f;
                toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            }

            if (!HasTransitionTo(state, breathing))
            {
                var back = state.AddTransition(breathing);
                back.hasExitTime = true;
                back.exitTime = 0.95f;
                back.duration = 0.25f;
            }
        }

        static bool HasTransitionTo(AnimatorState from, AnimatorState to)
        {
            foreach (var t in from.transitions) if (t.destinationState == to) return true;
            return false;
        }

        // ===== IdleVariantDriver 부착 =====

        static bool AttachDriverToPrefab(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning($"[IdleVariantSetup] 프리팹 없음(건너뜀): {path}");
                return false;
            }
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<IdleVariantDriver>() != null) return false;
                root.AddComponent<IdleVariantDriver>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // 열린 씬에서 프리팹 밖(또는 목록 밖) 캐릭터에도 부착
        static int AttachDriverToSceneCharacters()
        {
            int added = 0;
            foreach (var anim in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var rc = anim.runtimeAnimatorController;
                if (rc == null || rc.name != "Character_Movement") continue;
                if (anim.GetComponentInParent<IdleVariantDriver>(true) != null) continue;

                var host = FindCharacterRoot(anim);
                if (host == null)
                {
                    Debug.LogWarning($"[IdleVariantSetup] 캐릭터 루트를 특정하지 못해 건너뜀: {anim.transform.name}", anim);
                    continue;
                }
                if (host.GetComponent<IdleVariantDriver>() == null)
                {
                    host.AddComponent<IdleVariantDriver>();
                    added++;
                    EditorSceneManager.MarkSceneDirty(host.scene);
                }
            }
            if (added > 0) EditorSceneManager.SaveOpenScenes();
            return added;
        }

        static GameObject FindCharacterRoot(Animator anim)
        {
            // 1순위: 파트들을 구동하는 PlayerAnimatorManager 가 있는 조상 (문자열 검색 — 데모 어셈블리 하드 의존 회피)
            for (var t = anim.transform; t != null; t = t.parent)
                if (t.GetComponent("PlayerAnimatorManager") != null) return t.gameObject;
            // 2순위: 프리팹 인스턴스 루트
            return PrefabUtility.GetNearestPrefabInstanceRoot(anim.gameObject);
        }
    }
}
