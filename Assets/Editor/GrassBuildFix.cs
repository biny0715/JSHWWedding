// GrassBuildFix.cs (Editor-only)
// 증상: 잔디(터레인 디테일 메시)가 에디터엔 보이는데 WebGL 빌드에선 그림자만 보임.
// 원인: GPU 인스턴싱(procedural instancing) 셰이더 배리언트가 빌드에서 스트리핑됨.
// 해결: 디테일 프로토타입의 useInstancing=false → 항상 빌드에 포함되는 비인스턴싱 경로로 렌더.
//   (에디터 외형 동일, 빌드에서 렌더됨.)

using UnityEngine;
using UnityEditor;

public static class GrassBuildFix
{
    [MenuItem("Tools/Hanok/Fix Terrain Grass For Build (disable detail instancing)")]
    public static void Fix()
    {
        var go = GameObject.Find("MicroVerse/Terrain");
        var t = go ? go.GetComponent<Terrain>() : null;
        if (t == null || t.terrainData == null) { Debug.LogError("[GrassFix] terrain not found"); return; }
        var td = t.terrainData;

        var protos = td.detailPrototypes;
        int changed = 0, nullMesh = 0;
        for (int i = 0; i < protos.Length; i++)
        {
            if (protos[i].usePrototypeMesh && protos[i].prototype == null) { nullMesh++; }
            if (protos[i].useInstancing) { protos[i].useInstancing = false; changed++; }
        }

        try
        {
            td.detailPrototypes = protos;          // 배열 다시 설정
            EditorUtility.SetDirty(td);

            // 비인스턴싱은 더 무거우니 모바일 WebGL 용으로 디테일 거리 축소(250 -> 120).
            if (t.detailObjectDistance > 120f) t.detailObjectDistance = 120f;
            EditorUtility.SetDirty(t);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(t.gameObject.scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[GrassFix] useInstancing OFF on {changed} prototypes (nullMesh present: {nullMesh}). " +
                      $"detailObjectDistance={t.detailObjectDistance} density={t.detailObjectDensity}. Saved. Rebuild to verify.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GrassFix] setting detailPrototypes failed (likely the null-mesh prototype #): {e.Message}. " +
                           "Null 프로토타입에 메시를 지정하거나 제거 후 다시 시도.");
        }
    }
}
