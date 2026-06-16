// TerrainDetailInspector.cs (Editor-only) - 터레인 디테일(잔디) 프로토타입 진단용.
using UnityEngine;
using UnityEditor;

public static class TerrainDetailInspector
{
    [MenuItem("Tools/Hanok/Inspect Terrain Details")]
    public static void Inspect()
    {
        var go = GameObject.Find("MicroVerse/Terrain");
        var t = go ? go.GetComponent<Terrain>() : null;
        if (t == null || t.terrainData == null) { Debug.LogError("[TD] terrain/terrainData not found"); return; }
        var td = t.terrainData;

        Debug.Log($"[TD] detailObjectDistance={t.detailObjectDistance} density={t.detailObjectDensity} " +
                  $"drawInstanced={t.drawInstanced} detailScatterMode={td.detailScatterMode} prototypes={td.detailPrototypes.Length} " +
                  $"detailResolution={td.detailResolution} perPatch={td.detailResolutionPerPatch}");

        int i = 0;
        foreach (var p in td.detailPrototypes)
        {
            string kind = p.usePrototypeMesh
                ? $"MESH:{(p.prototype ? p.prototype.name : "null")}"
                : $"TEX:{(p.prototypeTexture ? p.prototypeTexture.name : "null")}";
            string mat = "-";
            if (p.usePrototypeMesh && p.prototype)
            {
                var r = p.prototype.GetComponentInChildren<MeshRenderer>();
                mat = (r && r.sharedMaterial) ? (r.sharedMaterial.shader ? r.sharedMaterial.shader.name : "NULL shader") : "no MeshRenderer/mat";
            }
            Debug.Log($"[TD] #{i}: {kind} renderMode={p.renderMode} useInstancing={p.useInstancing} " +
                      $"healthy={p.healthyColor} dry={p.dryColor} W=[{p.minWidth}-{p.maxWidth}] H=[{p.minHeight}-{p.maxHeight}] mat=[{mat}]");
            i++;
        }

        // detail layer 별 칠해진 양(0이면 안 칠해진 것)
        for (int d = 0; d < td.detailPrototypes.Length; d++)
        {
            var map = td.GetDetailLayer(0, 0, td.detailWidth, td.detailHeight, d);
            long sum = 0; foreach (var v in map) sum += v;
            Debug.Log($"[TD] layer #{d} painted total density = {sum}");
        }
    }
}
