// WeddingAdminSceneBuilder.cs
// Wedding 씬을 복제해 관리자(아이패드) 관전용 WeddingAdmin 씬을 만든다. 여러 번 실행해도 같은 결과(멱등) —
// Wedding 씬을 고친 뒤 다시 실행하면 관리자 씬도 최신 Wedding 기준으로 다시 만들어진다.
//  - 하객 전용 요소 제거: 내 캐릭터 생성(GameManager), 플레이어 추적/인트로 카메라, 미니맵, 상호작용·퀘스트 존 등
//  - Main Camera 는 고정 시점(스폰 지역을 내려다보는 위치), 루트 AdminViewer 에 관전 접속기만 (UI·조작 없음)
using System.Collections.Generic;
using System.Linq;
using JSHWWedding.Admin;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WeddingAdminSceneBuilder
{
    const string Src = "Assets/Stylized Water 3/Scenes/Wedding.unity";
    public const string Dst = "Assets/Stylized Water 3/Scenes/WeddingAdmin.unity";

    // 관리자 고정 카메라 시점 — 2026-11-06 에디터 플레이 중 직접 맞춘 위치
    static readonly Vector3 CamPosition = new Vector3(527.3058f, 58.4274f, 297.80646f);
    static readonly Vector3 CamEuler = new Vector3(24.5f, 14f, 0f);

    static readonly string[] RemoveRoots = { "Game Manager", "CinemachineCamera", "IntroCam", "MinimapSystem" };
    static readonly HashSet<string> RemoveTypes = new HashSet<string>
    {
        "InteractionZone", "PickupZone", "WreathViewZone", "WayfindingArrows", "NpcDialogCamera", "ZoneCameraView",
        "QuestManager", "CameraOcclusionFade", "CameraMoveZoom", "WeddingCameraIntro", "MinimapSystem",
        "WebLobbyBridge", "UIInputLock", "CinemachineBrain",
    };

    [MenuItem("Tools/Admin/Rebuild WeddingAdmin Scene")]
    public static void Build()
    {
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            Debug.LogError("[AdminScene] 열린 씬에 저장 안 된 변경이 있어 중단 — 먼저 저장하세요.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Dst) != null)
        {
            // 덮어쓰기 — 지우고 새로 만들면 .meta GUID 가 바뀌어 빌드 프로필 등의 씬 참조가 끊긴다
            if (EditorSceneManager.GetActiveScene().path == Dst) EditorSceneManager.OpenScene(Src, OpenSceneMode.Single);
            System.IO.File.Copy(Src, Dst, true);
            AssetDatabase.ImportAsset(Dst, ImportAssetOptions.ForceUpdate);
        }
        else if (!AssetDatabase.CopyAsset(Src, Dst)) { Debug.LogError("[AdminScene] 씬 복제 실패"); return; }
        var scene = EditorSceneManager.OpenScene(Dst, OpenSceneMode.Single);

        int removedRoots = 0, removed = 0, disabled = 0;
        foreach (var r in scene.GetRootGameObjects())
            if (RemoveRoots.Contains(r.name)) { Object.DestroyImmediate(r); removedRoots++; }

        foreach (var root in scene.GetRootGameObjects())
        foreach (var c in root.GetComponentsInChildren<Component>(true))
        {
            if (c == null || !RemoveTypes.Contains(c.GetType().Name)) continue;
            if (PrefabUtility.IsPartOfPrefabInstance(c))
            {
                if (c is Behaviour b) { b.enabled = false; disabled++; }   // 프리팹 인스턴스 안의 컴포넌트는 끄기만
            }
            else { Object.DestroyImmediate(c); removed++; }
        }

        var camGo = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Main Camera");
        if (camGo == null) { Debug.LogError("[AdminScene] Main Camera 없음"); return; }
        var cam = camGo.GetComponent<Camera>();
        cam.farClipPlane = Mathf.Max(cam.farClipPlane, 2000f);
        camGo.transform.SetPositionAndRotation(CamPosition, Quaternion.Euler(CamEuler));

        new GameObject("AdminViewer").AddComponent<AdminViewerNetwork>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[AdminScene] 완료: {Dst} | 루트 제거 {removedRoots} | 컴포넌트 제거 {removed} · 비활성 {disabled} | 카메라 고정 {CamPosition}");
    }
}
