// BuildingColliderBaker.cs  (Editor-only)
// WeddingEnviroments/House 의 각 건물(자식: Center/Left/Right ...)에 건물-로컬 기준 BoxCollider 1개씩 추가.
// 목적: (1) 카메라 Deoccluder가 건물을 피하게 (콜라이더 필요), (2) 플레이어가 벽 통과 못하게.
// 콜라이더 GameObject 레이어를 Ground 로 둬서 카메라 CollideAgainst(=Ground)에 포함, 플레이어(Default)는 제외 → 떨림 없음.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class BuildingColliderBaker
{
    const string HousePath = "WeddingEnviroments/House";

    [MenuItem("Tools/Hanok/Add Building Box Colliders")]
    public static void AddColliders()
    {
        var house = GameObject.Find(HousePath);
        if (house == null) { Debug.LogError($"[Collider] '{HousePath}' not found"); return; }
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0) { Debug.LogError("[Collider] 'Ground' layer missing"); return; }

        int n = 0;
        foreach (Transform child in house.transform)
        {
            var mfs = child.GetComponentsInChildren<MeshFilter>(true);
            bool init = false;
            Bounds local = new Bounds();
            foreach (var mf in mfs)
            {
                if (mf.sharedMesh == null) continue;
                Bounds mb = mf.sharedMesh.bounds;       // mesh-local
                Vector3[] c = {
                    new Vector3(mb.min.x,mb.min.y,mb.min.z), new Vector3(mb.max.x,mb.min.y,mb.min.z),
                    new Vector3(mb.min.x,mb.max.y,mb.min.z), new Vector3(mb.max.x,mb.max.y,mb.min.z),
                    new Vector3(mb.min.x,mb.min.y,mb.max.z), new Vector3(mb.max.x,mb.min.y,mb.max.z),
                    new Vector3(mb.min.x,mb.max.y,mb.max.z), new Vector3(mb.max.x,mb.max.y,mb.max.z),
                };
                foreach (var corner in c)
                {
                    Vector3 world = mf.transform.TransformPoint(corner);
                    Vector3 lp = child.InverseTransformPoint(world);   // building-local
                    if (!init) { local = new Bounds(lp, Vector3.zero); init = true; }
                    else local.Encapsulate(lp);
                }
            }
            if (!init) continue;

            var box = child.GetComponent<BoxCollider>();
            if (box == null) box = child.gameObject.AddComponent<BoxCollider>();
            box.center = local.center;
            box.size = local.size;
            child.gameObject.layer = groundLayer;     // 콜라이더 보유 오브젝트만 Ground 로
            n++;
            Debug.Log($"[Collider] {child.name}: size={local.size} (local) -> BoxCollider on Ground");
        }

        EditorSceneManager.MarkSceneDirty(house.scene);
        EditorSceneManager.SaveScene(house.scene);
        Debug.Log($"[Collider] DONE. {n} building box colliders added/updated + scene saved. (CollideAgainst already=Ground)");
    }
}
