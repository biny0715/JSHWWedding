// CustomizationDevTools.cs (Editor)
// MCP component_set 이 ScriptableObject 자산 참조를 못 넣으므로, 매니페스트 연결/테스트는 Editor 스크립트로 한다.
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace JSHWWedding.Customization.EditorTools
{
    public static class CustomizationDevTools
    {
        const string ManifestPath = "Assets/Customization/GastroPartManifest.asset";
        const string MalePrefab = "Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Resources/MaleCharacter.prefab";
        const string FemalePrefab = "Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Resources/FemaleCharacter.prefab";

        static GastroPartManifest Manifest =>
            AssetDatabase.LoadAssetAtPath<GastroPartManifest>(ManifestPath);

        // Stage 1 검증: 빈 루트에 여성/남성 기본 룩을 조립해 본다(에디트 모드).
        [MenuItem("Tools/JSHW/Test Assemble Female")]
        public static void TestAssembleFemale() => TestAssemble(1);
        [MenuItem("Tools/JSHW/Test Assemble Male")]
        public static void TestAssembleMale() => TestAssemble(0);

        static void TestAssemble(int gender)
        {
            var manifest = Manifest;
            if (manifest == null) { Debug.LogError("[TestAssemble] manifest 없음 — Build Gastro Manifest 먼저"); return; }

            var go = GameObject.Find("AssemblerTest");
            if (go == null) go = new GameObject("AssemblerTest");
            var asm = go.GetComponent<CharacterAssembler>();
            if (asm == null) asm = go.AddComponent<CharacterAssembler>();
            asm.manifest = manifest;
            asm.ApplyFull(manifest.DefaultFor(gender));

            int n = go.transform.childCount;
            string names = "";
            foreach (Transform c in go.transform) names += c.name + " ";
            Debug.Log($"[TestAssemble] gender={gender} AssemblerTest childCount={n} | parts: {names}");

            Selection.activeGameObject = go;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
        }

        [MenuItem("Tools/JSHW/Test Assemble - Cleanup")]
        public static void Cleanup()
        {
            var go = GameObject.Find("AssemblerTest");
            if (go != null) Object.DestroyImmediate(go);
            Debug.Log("[TestAssemble] AssemblerTest 정리됨");
        }

        // ===== Stage 2 와이어링: Lobby 씬에 프리뷰 컨트롤러 추가 (Lobby 씬을 연 상태로 실행) =====
        [MenuItem("Tools/JSHW/Setup Lobby Preview")]
        public static void SetupLobbyPreview()
        {
            var manifest = Manifest;
            if (manifest == null) { Debug.LogError("[Setup] manifest 없음 — Build Gastro Manifest 먼저"); return; }
            var scene = SceneManager.GetActiveScene();
            if (!scene.name.Contains("Lobby"))
            { Debug.LogWarning("[Setup] Lobby 씬을 열고 실행하세요. 현재: " + scene.name); return; }

            var go = GameObject.Find("LobbyPreview");
            if (go == null) go = new GameObject("LobbyPreview");
            var c = go.GetComponent<LobbyPreviewController>();
            if (c == null) c = go.AddComponent<LobbyPreviewController>();
            c.manifest = manifest;

            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Setup] LobbyPreview 추가/갱신 + manifest 연결 + 씬 저장 (scene=" + scene.name + ")");
        }

        // ===== Stage 4 와이어링: Male/FemaleCharacter 프리팹에 PlayerCustomizationApply 연결 =====
        [MenuItem("Tools/JSHW/Wire Player Prefabs")]
        public static void WirePlayerPrefabs()
        {
            var manifest = Manifest;
            if (manifest == null) { Debug.LogError("[Setup] manifest 없음"); return; }
            WirePrefab(MalePrefab, 0, manifest);
            WirePrefab(FemalePrefab, 1, manifest);
            AssetDatabase.SaveAssets();
        }

        static void WirePrefab(string path, int gender, GastroPartManifest manifest)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) { Debug.LogError("[Setup] 프리팹 로드 실패: " + path); return; }
            var apply = root.GetComponent<PlayerCustomizationApply>();
            if (apply == null) apply = root.AddComponent<PlayerCustomizationApply>();
            apply.gender = gender;
            apply.manifest = manifest;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[Setup] wired " + System.IO.Path.GetFileName(path) + " (gender=" + gender + ")");
        }
    }
}
