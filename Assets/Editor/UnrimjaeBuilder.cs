// UnrimjaeBuilder.cs  (Editor-only, one-off scene authoring helper)
// 운림제 3-동(중앙 본채 + 좌/우 동) 정리 스크립트.
// House(=조립된 자경전, 1288 flat 조각) 에서 담장/문/굴뚝(SM_JGJ_*, *Wall*, SkySphere)을 제거해
// 깨끗한 본채 1동만 남기고, 좌/우 Wing 도 동일하게 정리한 뒤 ㄷ자 위치로 재배치한다.
// 라이브 MCP 조작 대신 한 번의 배치 작업으로 GPU 부하를 줄이는 것이 목적.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class UnrimjaeBuilder
{
    const float WingScale = 0.6f;

    // 각 동의 (월드 XZ) 중심 목표 — 사용자가 배치했던 Left/Right 큐브 위치 기준.
    static readonly Vector2 LeftTargetXZ  = new Vector2(544.87f, 376.80f);
    static readonly Vector2 RightTargetXZ = new Vector2(585.99f, 338.04f);

    [MenuItem("Tools/Hanok/Rebuild Unrimjae 3 Buildings")]
    public static void Rebuild()
    {
        var center = GameObject.Find("House");
        if (center == null) { Debug.LogError("[Unrimjae] 'House' not found."); return; }

        // 1) 중앙 본채의 SkySphere 는 보존 위해 루트로 빼낸다(없으면 무시).
        var sky = center.GetComponentsInChildren<Transform>(true)
                        .FirstOrDefault(t => t.name.Contains("SkySphere"));
        if (sky != null) sky.SetParent(null, true);

        // 2) 중앙 본채 담장/문/굴뚝 제거 → 깨끗한 본채.
        int strippedC = StripPerimeter(center.transform);
        Bounds cb = CalcBounds(center.transform);
        float groundY = cb.min.y;
        Debug.Log($"[Unrimjae] Center stripped={strippedC}, bounds c={cb.center} size={cb.size} groundY={groundY:F2}");

        // 3) 좌/우 동: 있으면 정리+재배치, 없으면 중앙 복제.
        FixOrMakeWing(center, "Wing_Left", 133.2f, LeftTargetXZ, groundY);
        FixOrMakeWing(center, "Wing_Right", 313.2f, RightTargetXZ, groundY);

        EditorSceneManager.MarkSceneDirty(center.scene);
        EditorSceneManager.SaveScene(center.scene);
        Debug.Log("[Unrimjae] DONE + scene saved.");
    }

    static void FixOrMakeWing(GameObject center, string name, float rotY, Vector2 targetXZ, float groundY)
    {
        var wing = GameObject.Find(name);
        if (wing == null)
        {
            wing = Object.Instantiate(center);
            wing.name = name;
            wing.transform.SetParent(null, true);
        }

        int stripped = StripPerimeter(wing.transform);   // 담장/문/굴뚝 + 복제된 SkySphere 제거
        wing.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        wing.transform.localScale = Vector3.one * WingScale;

        Bounds b = CalcBounds(wing.transform);
        wing.transform.position += new Vector3(targetXZ.x - b.center.x,
                                               groundY - b.min.y,
                                               targetXZ.y - b.center.z);

        Bounds f = CalcBounds(wing.transform);
        Debug.Log($"[Unrimjae] {name}: stripped={stripped} center={f.center} size={f.size} baseY={f.min.y:F2}");
    }

    // SM_JGJ_*(담장/문/굴뚝/비석), *Wall*(BottomWall/Wallframe/Wall), SkySphere 제거.
    // 본채 조각은 SM_JGJM_* 이며 "Wall" 을 포함하지 않으므로 보존된다.
    static int StripPerimeter(Transform root)
    {
        var kill = new List<GameObject>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == root) continue;
            string n = t.name;
            if (n.StartsWith("SM_JGJ_") || n.Contains("Wall") || n.Contains("SkySphere"))
                kill.Add(t.gameObject);
        }
        foreach (var go in kill) if (go != null) Object.DestroyImmediate(go);
        return kill.Count;
    }

    static Bounds CalcBounds(Transform root)
    {
        var rends = root.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return new Bounds(root.position, Vector3.zero);
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b;
    }

    // 향후 일반 실행(Hub 더블클릭/자동 재시작)에서도 에디터가 D3D11 을 쓰도록 영구 설정.
    // WebGL 빌드 타깃과 무관(스탠드얼론 Windows 에디터 API만 변경).
    [MenuItem("Tools/Hanok/Force D3D11 Editor (Standalone, permanent)")]
    public static void ForceD3D11Permanent()
    {
        var t = BuildTarget.StandaloneWindows64;
        PlayerSettings.SetUseDefaultGraphicsAPIs(t, false);
        PlayerSettings.SetGraphicsAPIs(t, new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });
        AssetDatabase.SaveAssets();
        Debug.Log("[Unrimjae] Standalone Windows editor Graphics API => Direct3D11 (permanent). Applies on next launch.");
    }
}
