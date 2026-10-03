using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using JSHWWedding;

// 보물찾기 선물상자: Assets/Models/GiftBox/GiftBox.fbx (원본 BlenderSource/giftbox.py)
//  - Setup: 재질(핑크 상자 + 흰 리본) 생성/리맵, Wedding 씬 MiniGame 의 PickupZone 마다
//    자식 "GiftBox" 를 바닥에 붙여 배치하고 PickupZone.visual 에 연결 후 씬 저장(멱등).
//  - Capture: 상자 위치별 확인 캡처 → Temp/giftbox/<이름>.png
public static class GiftBoxSetup
{
    const string Dir = "Assets/Models/GiftBox/";
    const string Fbx = Dir + "GiftBox.fbx";

    [MenuItem("Tools/JSHW/Setup Quest Gift Boxes")]
    public static void Setup()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "Wedding") throw new System.Exception("Wedding 씬을 열고 실행하세요");
        if (scene.isDirty) throw new System.Exception("씬에 저장 안 된 변경이 있어 중단 — 먼저 저장/확인하세요");

        var (box, ribbon) = Materials();

        var mi = (ModelImporter)AssetImporter.GetAtPath(Fbx);
        mi.animationType = ModelImporterAnimationType.None;
        mi.importAnimation = false; mi.importCameras = false; mi.importLights = false; mi.importBlendShapes = false;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "GiftBox_Box"), box);
        mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "GiftBox_Ribbon"), ribbon);
        mi.SaveAndReimport();

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        Physics.SyncTransforms();
        foreach (var zone in Zones())
        {
            var old = zone.transform.Find("GiftBox");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, zone.transform);
            go.name = "GiftBox";
            // 바닥에 붙이기: 존 바로 아래의 가장 높은 표면(지형·콜라이더 없는 메시·수면 포함)
            var p = zone.transform.position;
            var surf = SurfacesBelow(p);
            var ground = surf.Count > 0 ? new Vector3(p.x, surf[0].y, p.z) : p;
            go.transform.position = ground;
            // FBX 루트의 축 변환 회전(Blender Z-up)은 유지하고 Y 방향만 무작위로 돌린다
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * model.transform.rotation;
            go.transform.localScale = Vector3.one * (1f / Mathf.Max(0.0001f, zone.transform.lossyScale.y));   // 월드 크기 고정
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.receiveShadows = false;
            zone.visual = go;
            EditorUtility.SetDirty(zone);
            Debug.Log($"[GiftBoxSetup] {zone.name}: zone={p:F2} ground={(surf.Count > 0 ? surf[0].name : "(없음)")} at {ground:F2}");
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    // 존 아래로 수직 레이를 쏴서 맞는 모든 표면(콜라이더 + 콜라이더 없는 메시)을 높이순으로 반환
    static System.Collections.Generic.List<(float y, string name)> SurfacesBelow(Vector3 p, float up = 0.3f, float down = 5f)
    {
        var res = new System.Collections.Generic.List<(float, string)>();
        var ray = new Ray(p + Vector3.up * up, Vector3.down);
        foreach (var h in Physics.RaycastAll(ray, up + down, ~0, QueryTriggerInteraction.Ignore))
            res.Add((h.point.y, "col:" + h.collider.name));
        var intersect = typeof(HandleUtility).GetMethod("IntersectRayMesh", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            var r = mf.GetComponent<MeshRenderer>();
            if (r == null || !r.enabled || mf.sharedMesh == null || mf.GetComponentInParent<PickupZone>() != null) continue;
            var b = r.bounds;
            if (p.x < b.min.x || p.x > b.max.x || p.z < b.min.z || p.z > b.max.z || b.min.y > p.y + up || b.max.y < p.y - down) continue;
            var args = new object[] { ray, mf.sharedMesh, mf.transform.localToWorldMatrix, null };
            if ((bool)intersect.Invoke(null, args)) { var h = (RaycastHit)args[3]; if (h.distance <= up + down) res.Add((h.point.y, "mesh:" + mf.name)); }
        }
        res.Sort((a, c) => c.Item1.CompareTo(a.Item1));
        return res;
    }

    public static void Probe()
    {
        foreach (var z in Zones())
            Debug.Log($"[GiftBoxSetup] probe {z.name} y={z.transform.position.y:F2}: " +
                      string.Join(", ", SurfacesBelow(z.transform.position).Select(s => $"{s.name}@{s.y:F2}")));
    }

    static PickupZone[] Zones() => Object.FindObjectsByType<PickupZone>(FindObjectsSortMode.None).OrderBy(z => z.transform.GetSiblingIndex()).ToArray();

    // 상자 핑크 + 흰 리본. 색만 바꿀 땐 UpdateMaterials 만 실행(배치/씬은 그대로).
    public static void UpdateMaterials() { Materials(); AssetDatabase.SaveAssets(); }

    const string BoxColor = "#EC7FA0";   // 햇빛에서 밝게 날아가지 않도록 진한 핑크(이전 #F2C0BF 는 거의 흰색으로 보였음)

    static (Material box, Material ribbon) Materials()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        ColorUtility.TryParseHtmlString(BoxColor, out var pink);
        var box = Mat(Dir + "GiftBox_Box.mat", lit, pink, Color.black);
        var ribbon = Mat(Dir + "GiftBox_Ribbon.mat", lit, Color.white, Color.black);
        return (box, ribbon);
    }

    static Material Mat(string path, Shader s, Color c, Color emission)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(s); AssetDatabase.CreateAsset(m, path); }
        m.shader = s;
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0.45f);
        m.SetFloat("_Metallic", 0f);
        m.SetColor("_EmissionColor", emission); m.EnableKeyword("_EMISSION");   // 그늘에서도 색이 죽지 않게 약하게
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    public static void Capture()
    {
        Directory.CreateDirectory("Temp/giftbox");
        var main = Camera.main;
        var go = new GameObject("~GiftCam") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>();
        if (main != null) cam.CopyFrom(main);
        var data = cam.GetUniversalAdditionalCameraData();
        if (main != null) { var md = main.GetUniversalAdditionalCameraData(); data.renderPostProcessing = md.renderPostProcessing; data.volumeLayerMask = md.volumeLayerMask; }
        cam.fieldOfView = 50f;
        var rt = new RenderTexture(800, 800, 24);
        var prev = RenderTexture.active;
        try
        {
            cam.targetTexture = rt;
            foreach (var zone in Zones())
            {
                if (zone.visual == null) continue;
                var c = zone.visual.transform.position + Vector3.up * 0.2f;
                // 가장 트인 방향을 찾기: 8방향 중 카메라~상자 사이에 막힘이 없는 첫 방향
                Vector3 pos = c + new Vector3(0, 1.6f, -2.6f);
                for (int i = 0; i < 8; i++)
                {
                    var d = Quaternion.Euler(0, i * 45f, 0) * new Vector3(0, 1.6f, -2.6f);
                    if (!Physics.Linecast(c + d, c + Vector3.up * 0.1f, ~0, QueryTriggerInteraction.Ignore)) { pos = c + d; break; }
                }
                cam.transform.position = pos; cam.transform.LookAt(c);
                VolumeManager.instance.Update(cam.transform, data.volumeLayerMask);
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(800, 800, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 800, 800), 0, 0); tex.Apply();
                File.WriteAllBytes($"Temp/giftbox/{zone.name}.png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
        }
        finally { RenderTexture.active = prev; cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(go); }
        Debug.Log("[GiftBoxSetup] captured");
    }
}
