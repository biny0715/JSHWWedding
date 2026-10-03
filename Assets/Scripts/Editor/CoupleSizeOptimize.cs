using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 신랑/신부 Tripo 모델 WebGL 용량 최적화 보조:
//  - 신랑 BaseColor WebGL 압축(신부와 동일 설정)
//  - 프리팹의 비활성 *_PreTripo_Backup 제거(빌드 포함 방지)
//  - 전후 비교 캡처(씬은 바꾸지 않음) → Temp/opt/<tag>_<npc>_<view>.png
public static class CoupleSizeOptimize
{
    const string Prefabs = "Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Prefabs/";

    public static void CompressGroomTexture()
    {
        var bride = (TextureImporter)AssetImporter.GetAtPath("Assets/Models/WeddingBrideTripo/Bride_Tripo_BaseColor.png");
        var groom = (TextureImporter)AssetImporter.GetAtPath("Assets/Models/WeddingGroomTripo/Groom_Tripo_BaseColor.png");
        var s = bride.GetPlatformTextureSettings("WebGL");
        s.name = "WebGL";
        groom.SetPlatformTextureSettings(s);
        groom.SaveAndReimport();
        var t = groom.GetPlatformTextureSettings("WebGL");
        Debug.Log($"[CoupleSizeOptimize] groom WebGL override={t.overridden} max={t.maxTextureSize} format={t.format}");
    }

    public static void CompressMeshes()
    {
        foreach (var p in new[] { "Assets/Models/WeddingGroomTripo/Groom_Tripo.fbx", "Assets/Models/WeddingBrideTripo/Bride_Tripo.fbx" })
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(p);
            mi.meshCompression = ModelImporterMeshCompression.Low;   // UV 정밀도(얼굴 텍스처) 보존 위해 Low
            mi.SaveAndReimport();
        }
        Debug.Log("[CoupleSizeOptimize] mesh compression = Low");
    }

    public static void RemoveBackups()
    {
        foreach (var name in new[] { "HwNPC", "BrideNPC" })
        {
            var path = Prefabs + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var backups = root.GetComponentsInChildren<Transform>(true).Where(t => t.name.EndsWith("_PreTripo_Backup")).ToArray();
                foreach (var b in backups) Object.DestroyImmediate(b.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[CoupleSizeOptimize] {name}: 백업 {backups.Length}개 제거");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }

    public static void CaptureBefore() => Capture("before");
    public static void CaptureAfter() => Capture("after");

    static void Capture(string tag)
    {
        Directory.CreateDirectory("Temp/opt");
        foreach (var (npc, model) in new[] { ("HwNPC", "GroomModel"), ("BrideNPC", "BrideModel") })
        {
            var root = GameObject.Find(npc);
            var m = root.GetComponentsInChildren<Transform>().First(t => t.name == model).gameObject;
            var rs = m.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
            int tris = m.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3);
            Debug.Log($"[CoupleSizeOptimize] {tag} {npc} tris={tris} bounds center={b.center:F4} size={b.size:F4}");

            var go = new GameObject("~OptCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            var main = Camera.main;
            if (main != null) cam.CopyFrom(main);
            var data = cam.GetUniversalAdditionalCameraData();
            if (main != null) { var md = main.GetUniversalAdditionalCameraData(); data.renderPostProcessing = md.renderPostProcessing; data.volumeLayerMask = md.volumeLayerMask; data.antialiasing = md.antialiasing; }
            var rt = new RenderTexture(900, 1200, 24);
            var prev = RenderTexture.active;
            try
            {
                cam.targetTexture = rt; cam.nearClipPlane = 0.01f; cam.farClipPlane = 100f; cam.fieldOfView = 35f;
                // (뷰, yaw, 초점 높이 비율, 거리 배수)
                foreach (var (v, yaw, hk, dk) in new[] { ("front", 0f, 0.5f, 2.1f), ("q34", 35f, 0.5f, 2.1f), ("face", 0f, 0.79f, 0.85f), ("faceQ", 35f, 0.79f, 0.85f) })
                {
                    var target = new Vector3(b.center.x, b.min.y + b.size.y * hk, b.center.z);
                    cam.transform.position = target + Quaternion.AngleAxis(yaw, Vector3.up) * root.transform.forward * (b.size.y * dk);
                    cam.transform.LookAt(target);
                    VolumeManager.instance.Update(cam.transform, data.volumeLayerMask);
                    cam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(900, 1200, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 900, 1200), 0, 0); tex.Apply();
                    File.WriteAllBytes($"Temp/opt/{tag}_{npc}_{v}.png", tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                }
            }
            finally { RenderTexture.active = prev; cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(go); }
        }
    }
}
