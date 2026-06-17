// BuildingMeshOccluderBaker.cs (Editor-only)
// WeddingEnviroments/House 각 건물의 "큰" 메시 조각에 MeshCollider 추가 (가림 fade 정확 판정용).
// - 레이어는 그대로(Default) 두어 렌더링/네비메시 영향 없음.
// - CameraOcclusionFade 가 카메라->캐릭터 레이를 이 콜라이더에 쏴 실제 가림만 판정.
// - 클릭 이동은 PlayerClickToMove 가 Default 레이어를 제외하므로 이 콜라이더에 막히지 않음.
// 작은 장식(맥스 치수 < minSize)은 건너뛰어 모바일 부담을 줄임.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class BuildingMeshOccluderBaker
{
    const string HousePath = "WeddingEnviroments/House";
    const float MinSize = 0.6f;   // 월드 바운즈 최대 치수가 이 값 미만이면 콜라이더 생략

    [MenuItem("Tools/Hanok/Add Building Mesh Occluders")]
    public static void AddOccluders()
    {
        var house = GameObject.Find(HousePath);
        if (house == null) { Debug.LogError($"[MeshOccluder] '{HousePath}' not found"); return; }

        int added = 0, skipped = 0, buildings = 0;
        foreach (Transform child in house.transform)
        {
            buildings++;
            var rends = child.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var rend in rends)
            {
                var mf = rend.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                Vector3 s = rend.bounds.size;
                if (Mathf.Max(s.x, Mathf.Max(s.y, s.z)) < MinSize) { skipped++; continue; }

                var mc = rend.GetComponent<MeshCollider>();
                if (mc == null) mc = rend.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                // convex+trigger: 물리 충돌(플레이어 끼임) 없음, 레이캐스트는 맞음, 모바일에 가벼움(볼록헐 ≤255삼각형)
                mc.convex = true;
                mc.isTrigger = true;
                added++;
            }
        }

        EditorSceneManager.MarkSceneDirty(house.scene);
        EditorSceneManager.SaveScene(house.scene);
        Debug.Log($"[MeshOccluder] DONE. buildings={buildings}, colliders added/updated={added}, skipped(small)={skipped} + scene saved.");
    }

    [MenuItem("Tools/Hanok/Remove Building Mesh Occluders")]
    public static void RemoveOccluders()
    {
        var house = GameObject.Find(HousePath);
        if (house == null) { Debug.LogError($"[MeshOccluder] '{HousePath}' not found"); return; }
        int removed = 0;
        foreach (var mc in house.GetComponentsInChildren<MeshCollider>(true))
        {
            Object.DestroyImmediate(mc);
            removed++;
        }
        EditorSceneManager.MarkSceneDirty(house.scene);
        EditorSceneManager.SaveScene(house.scene);
        Debug.Log($"[MeshOccluder] removed {removed} MeshColliders + scene saved.");
    }
}
