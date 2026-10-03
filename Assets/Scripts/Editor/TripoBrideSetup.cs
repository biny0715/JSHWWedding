using UnityEditor;
using UnityEngine;

// Tripo 기반 신부(Assets/Models/WeddingBrideTripo) 임포트 설정 + 씬을 바꾸지 않는 확인용 캡처.
// 원본: BlenderSource/WeddingCouple_v10.blend (tb_step*.py, tb_export.py)
public static class TripoBrideSetup
{
    const string Dir = "Assets/Models/WeddingBrideTripo/";
    const string Fbx = Dir + "Bride_Tripo.fbx";

    [MenuItem("Tools/JSHW/Setup Tripo Bride Assets")]
    public static void Setup()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        SetTex(Dir + "Bride_Tripo_BaseColor.png", true, false);
        SetTex(Dir + "Bride_Tripo_Normal.png", false, true);
        SetTex(Dir + "Bride_VeilLace.png", true, false);

        var body = LoadOrCreate(Dir + "Bride_Tripo_Body.mat", lit);
        body.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Bride_Tripo_BaseColor.png"));
        body.SetColor("_BaseColor", Color.white);
        body.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Bride_Tripo_Normal.png"));
        body.EnableKeyword("_NORMALMAP");
        body.SetFloat("_Metallic", 0f);
        body.SetFloat("_Smoothness", 0.3f);
        body.SetFloat("_ReceiveShadows", 0f); body.EnableKeyword("_RECEIVE_SHADOWS_OFF");
        EditorUtility.SetDirty(body);

        var veil = LoadOrCreate(Dir + "Bride_Tripo_Veil.mat", lit);
        veil.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Bride_VeilLace.png"));
        veil.SetColor("_BaseColor", Color.white);
        veil.SetFloat("_Smoothness", 0.2f);
        veil.SetFloat("_Surface", 1f); veil.SetFloat("_Blend", 0f); veil.SetFloat("_Cull", 0f); veil.SetFloat("_ZWrite", 0f);
        veil.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        veil.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        veil.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        veil.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        veil.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        veil.SetOverrideTag("RenderType", "Transparent");
        veil.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        veil.SetShaderPassEnabled("ShadowCaster", false);
        veil.SetTexture("_EmissionMap", veil.GetTexture("_BaseMap"));
        veil.SetColor("_EmissionColor", new Color(0.3f, 0.3f, 0.3f)); veil.EnableKeyword("_EMISSION");
        veil.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(veil);
        AssetDatabase.SaveAssets();

        var mi = (ModelImporter)AssetImporter.GetAtPath(Fbx);
        mi.animationType = ModelImporterAnimationType.None;
        mi.importAnimation = false; mi.importBlendShapes = false; mi.importCameras = false; mi.importLights = false;
        mi.meshCompression = ModelImporterMeshCompression.Medium;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        var palette = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/WeddingCouple/WeddingCouple_Palette.mat");
        mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Bride_Tripo_Body"), body);
        mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "TBride_Veil_Mat"), veil);
        mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "WeddingCouple_Palette"), palette);
        mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "WeddingCouple_Veil"), palette);
        mi.SaveAndReimport();
        var mesh = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx).GetComponentInChildren<MeshFilter>().sharedMesh;
        Debug.Log($"[TripoBrideSetup] imported tris={mesh.triangles.Length / 3} bounds={mesh.bounds}");
    }

    static void SetTex(string path, bool srgb, bool normal)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        ti.sRGBTexture = srgb;
        ti.alphaSource = TextureImporterAlphaSource.FromInput;
        ti.alphaIsTransparency = path.Contains("Veil");
        ti.maxTextureSize = 1024;   // WebGL
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.SaveAndReimport();
    }

    static Material LoadOrCreate(string path, Shader s)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(s); AssetDatabase.CreateAsset(m, path); return m;
    }

    // 씬은 저장하지 않는다: 현재 BrideModel 자리에 임시로 놓고 캡처한 뒤 원래대로 되돌림.
    public static void CaptureInScene()
    {
        var current = GameObject.Find("BrideModel");
        var anchor = current.transform.parent;
        var rends = current.GetComponentsInChildren<Renderer>();
        foreach (var r in rends) r.enabled = false;
        var tmp = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Fbx));
        tmp.hideFlags = HideFlags.DontSave;
        tmp.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        tmp.transform.localScale = anchor.lossyScale;
        var go = new GameObject("~TripoCam") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>();
        var rt = new RenderTexture(900, 1200, 24); cam.targetTexture = rt;
        var views = new[] { ("front", 0f, 2.6f, 0.75f, 40f), ("q34L", -35f, 2.6f, 0.75f, 40f), ("q34R", 35f, 2.6f, 0.75f, 40f),
                            ("side", 90f, 2.6f, 0.75f, 40f), ("back", 180f, 2.6f, 0.75f, 40f), ("face", 0f, 1.4f, 1.1f, 30f) };
        try
        {
            foreach (var (v, yaw, dist, h, fov) in views)
            {
                cam.fieldOfView = fov;
                var focus = anchor.position + Vector3.up * h;
                var dir = Quaternion.AngleAxis(yaw, Vector3.up) * anchor.forward;
                cam.transform.position = focus + dir * dist + Vector3.up * 0.15f;
                cam.transform.LookAt(focus);
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(900, 1200, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 900, 1200), 0, 0);
                System.IO.File.WriteAllBytes($"Temp/tripo_{v}.png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
        }
        finally
        {
            RenderTexture.active = null; cam.targetTexture = null;
            Object.DestroyImmediate(rt); Object.DestroyImmediate(go); Object.DestroyImmediate(tmp);
            foreach (var r in rends) r.enabled = true;
        }
        Debug.Log("[TripoBrideSetup] captured (scene unchanged)");
    }
}
