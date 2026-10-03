using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 신랑(HwNPC/Hw_Char)·신부(BrideNPC/Jisu_NPC)의 GASTRO 파츠를 Blender 제작 단일 메시 모델로 교체.
// 원본: BlenderSource/WeddingCouple_v9.blend (wc_build.py로 생성, wc_export.py로 FBX 내보내기)
public static class WeddingCoupleSetup
{
    const string Dir = "Assets/Models/WeddingCouple/";
    const string GroomFbx = Dir + "Groom.fbx";
    const string BrideFbx = Dir + "Bride.fbx";
    const string AtlasTex = Dir + "WeddingCouple_Atlas.png";
    const string PaletteMat = Dir + "WeddingCouple_Palette.mat";
    const string VeilMat = Dir + "WeddingCouple_Veil.mat";
    const string HwPrefab = "Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Prefabs/HwNPC.prefab";
    const string BridePrefab = "Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Prefabs/BrideNPC.prefab";
    const string ScenePath = "Assets/Stylized Water 3/Scenes/Wedding.unity";

    // FBX/아틀라스만 다시 내보냈을 때: 임포트 설정과 머티리얼만 갱신 (씬·프리팹은 FBX GUID로 그대로 연결됨)
    [MenuItem("Tools/JSHW/Refresh Wedding Couple Assets")]
    public static void Refresh()
    {
        var palette = SetupMaterials(out var veil);
        SetupModel(GroomFbx, palette, veil);
        SetupModel(BrideFbx, palette, veil);
        Debug.Log("[WeddingCoupleSetup] 신랑/신부 에셋 갱신 완료");
    }

    // 씬·프리팹 적용 (여러 번 실행해도 같은 결과):
    //  - 신랑: HwNPC 프리팹의 Hw_Char 아래를 GroomModel로 교체
    //  - 신부: 씬에만 추가돼 있던 Jisu_NPC(+이름표)를 BrideNPC 프리팹 안으로 옮기고, 씬의 추가/제거 오버라이드를 정리
    //  - 정적 메시라 의미가 없어진 GASTRO 전용 컴포넌트(IdleVariantDriver, CharacterAssembler, 파트 Animator) 제거
    [MenuItem("Tools/JSHW/Apply Wedding Couple Models")]
    public static void Apply()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
        if (scene.isDirty)
        {
            Debug.LogWarning("[WeddingCoupleSetup] Wedding 씬에 저장되지 않은 변경이 있어 중단합니다. 먼저 저장하거나 되돌린 뒤 다시 실행하세요.");
            return;
        }
        Refresh();

        var root = PrefabUtility.LoadPrefabContents(HwPrefab);
        ReplaceChildren(root.transform.Find("Hw_Char"), GroomFbx, "GroomModel");
        RemoveLegacy(root);
        PrefabUtility.SaveAsPrefabAsset(root, HwPrefab);
        PrefabUtility.UnloadPrefabContents(root);

        var sceneBride = GameObject.Find("BrideNPC");
        Transform sceneJisu = null;
        foreach (Transform ch in sceneBride.transform)
            if (ch.name == "Jisu_NPC" && PrefabUtility.IsAddedGameObjectOverride(ch.gameObject)) sceneJisu = ch;

        root = PrefabUtility.LoadPrefabContents(BridePrefab);
        var jisu = root.transform.Find("Jisu_NPC");
        if (sceneJisu != null)
        {
            jisu.localPosition = sceneJisu.localPosition;
            jisu.localRotation = sceneJisu.localRotation;
            jisu.localScale = sceneJisu.localScale;
            var src = sceneJisu.GetComponent<JSHWWedding.PlayerNameTag>();
            var dst = jisu.GetComponent<JSHWWedding.PlayerNameTag>();
            if (dst == null) dst = jisu.gameObject.AddComponent<JSHWWedding.PlayerNameTag>();
            if (src != null) EditorUtility.CopySerialized(src, dst);   // 축하하기 InteractionZone이 이 이름으로 신부를 찾음
        }
        foreach (var a in jisu.GetComponents<Animator>()) Object.DestroyImmediate(a);
        ReplaceChildren(jisu, BrideFbx, "BrideModel");
        RemoveLegacy(root);
        PrefabUtility.SaveAsPrefabAsset(root, BridePrefab);
        PrefabUtility.UnloadPrefabContents(root);

        sceneBride = GameObject.Find("BrideNPC");
        var added = new System.Collections.Generic.List<GameObject>();
        foreach (Transform ch in sceneBride.transform)
            if (ch.name == "Jisu_NPC" && PrefabUtility.IsAddedGameObjectOverride(ch.gameObject)) added.Add(ch.gameObject);
        foreach (var go in added) Object.DestroyImmediate(go);
        foreach (var removed in PrefabUtility.GetRemovedGameObjects(sceneBride)) removed.Revert();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Verify();
    }

    // InteractionZone이 실제로 쓰는 이름 문자열(소스에서 추출)로 FindNpcByName을 호출해 신랑/신부가 찾아지는지 확인
    public static void VerifyNpcLookup()
    {
        var src = System.IO.File.ReadAllText("Assets/Scripts/InteractionZone.cs");
        var names = new System.Collections.Generic.List<string>();
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(src, "(?:FindNpcByName|AttachTalkTo)\\(\"([^\"]+)\""))
            names.Add(m.Groups[1].Value.Replace("\\n", "\n"));
        System.Type zone = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            foreach (var t in asm.GetTypes())
                if (t.Name == "InteractionZone") zone = t;
        var find = zone.GetMethod("FindNpcByName", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        foreach (var n in names)
        {
            var tr = find.Invoke(null, new object[] { n }) as Transform;
            Debug.Log($"[WeddingCoupleSetup] lookup #{names.IndexOf(n)} -> {(tr != null ? tr.name + " / model=" + (tr.GetComponentInChildren<MeshRenderer>() != null ? tr.GetComponentInChildren<MeshRenderer>().name : "none") : "NOT FOUND")}");
        }
    }

    // NpcDialogCamera.DoFocus와 같은 식으로 말걸기(신랑)/축하하기(신부) 대화 카메라 구도를 세로 화면으로 촬영
    public static void CaptureDialogViews()
    {
        var main = Camera.main;
        var go = new GameObject("~DialogCam") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = main != null ? main.fieldOfView : 60f;
        var rt = new RenderTexture(540, 960, 24);
        cam.targetTexture = rt;
        // (대상, headHeight, extraCamRaise, distance) — InteractionZone.cs:106,110의 호출값
        var shots = new[] { ("Hw_Char", 0.45f, 0f, 4f, "dialog_groom"), ("Jisu_NPC", 0.45f, 0.45f, 3.5f, "dialog_bride") };
        foreach (var (name, h, extra, dist, file) in shots)
        {
            var npc = GameObject.Find(name).transform;
            var head = npc.position + Vector3.up * h;
            var fwd = npc.forward; fwd.y = 0f; fwd.Normalize();
            var pos = head + fwd * dist + Vector3.up * (0.3f + extra);
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation((head - pos).normalized, Vector3.up));
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(540, 960, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 540, 960), 0, 0);
            System.IO.File.WriteAllBytes($"Temp/{file}.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(go);
        Debug.Log($"[WeddingCoupleSetup] dialog views captured (fov {(main != null ? main.fieldOfView : 60f)})");
    }

    static void RemoveLegacy(GameObject root)
    {
        foreach (var c in root.GetComponentsInChildren<JSHWWedding.IdleVariantDriver>(true)) Object.DestroyImmediate(c);
        foreach (var c in root.GetComponentsInChildren<JSHWWedding.Customization.CharacterAssembler>(true)) Object.DestroyImmediate(c);
    }

    // 적용 결과 점검: 모델/이름표 개수, 남은 Animator·구 컴포넌트, 신부 프리팹 인스턴스 오버라이드
    public static void Verify()
    {
        int groom = 0, bride = 0, anim = 0, legacy = 0, tags = 0;
        foreach (var name in new[] { "HwNPC", "BrideNPC" })
        {
            var go = GameObject.Find(name);
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "GroomModel") groom++;
                if (t.name == "BrideModel") bride++;
            }
            anim += go.GetComponentsInChildren<Animator>(true).Length;
            legacy += go.GetComponentsInChildren<JSHWWedding.IdleVariantDriver>(true).Length
                    + go.GetComponentsInChildren<JSHWWedding.Customization.CharacterAssembler>(true).Length;
            tags += go.GetComponentsInChildren<JSHWWedding.PlayerNameTag>(true).Length;
        }
        var b = GameObject.Find("BrideNPC");
        Debug.Log($"[WeddingCoupleSetup] verify: GroomModel={groom} BrideModel={bride} nameTags={tags} animators={anim} legacy={legacy} " +
                  $"brideAddedGO={PrefabUtility.GetAddedGameObjects(b).Count} brideRemovedGO={PrefabUtility.GetRemovedGameObjects(b).Count}");
    }
    // 확인용: 각 NPC를 정면/3/4 전신, 얼굴 정면/3/4로 촬영해 프로젝트 루트 Temp/u_*.png로 저장
    public static void CapturePreview()
    {
        var go = new GameObject("~PreviewCam") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>();
        var rt = new RenderTexture(900, 1200, 24);
        cam.targetTexture = rt;
        // (뷰 이름, 수평 각도, 거리, 초점 높이, 시야각)
        var views = new[] { ("front", 0f, 2.6f, 0.8f, 40f), ("q34", 35f, 2.6f, 0.8f, 40f), ("q34L", -35f, 2.6f, 0.8f, 40f),
                            ("side", 90f, 2.6f, 0.8f, 40f), ("sideL", -90f, 2.6f, 0.8f, 40f), ("face", 0f, 1.5f, 1.1f, 30f), ("face34", 35f, 1.5f, 1.1f, 30f),
                            ("face34L", -35f, 1.5f, 1.1f, 30f), ("faceside", 90f, 1.5f, 1.1f, 30f) };
        foreach (var name in new[] { "GroomModel", "BrideModel" })
        {
            var t = GameObject.Find(name).transform;
            foreach (var (view, yaw, dist, height, fov) in views)
            {
                cam.fieldOfView = fov;
                var focus = t.position + Vector3.up * height;
                var dir = Quaternion.AngleAxis(yaw, Vector3.up) * t.forward;
                cam.transform.position = focus + dir * dist + Vector3.up * (dist > 2f ? 0.25f : 0.03f);
                cam.transform.LookAt(focus);
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                System.IO.File.WriteAllBytes($"Temp/u_{name}_{view}.png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
        }
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(go);
        Debug.Log("[WeddingCoupleSetup] preview captured");
    }

    // 진단용: 신부 얼굴 3/4를 조건별로 촬영 (에셋은 바꾸지 않고 임시 머티리얼/렌더러 설정만 사용 후 원복)
    public static void CaptureShadowDiag()
    {
        var r = GameObject.Find("BrideModel").GetComponentInChildren<MeshRenderer>();
        var origMats = r.sharedMaterials;
        var origCast = r.shadowCastingMode;
        var sun = RenderSettings.sun;
        Debug.Log($"[Diag] sun={(sun ? sun.name : "none")} rot={(sun ? sun.transform.eulerAngles.ToString() : "-")} shadows={(sun ? sun.shadows.ToString() : "-")} ambient={RenderSettings.ambientMode}");
        var noRecv = System.Array.ConvertAll(origMats, m => { var c = new Material(m); c.EnableKeyword("_RECEIVE_SHADOWS_OFF"); c.SetFloat("_ReceiveShadows", 0f); return c; });
        try
        {
            CaptureOne(r.transform, "diag_normal");
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            CaptureOne(r.transform, "diag_noselfcast");
            r.shadowCastingMode = origCast;
            r.sharedMaterials = noRecv;
            CaptureOne(r.transform, "diag_noreceive");
            r.sharedMaterials = origMats;
            // ambient only: if the light/dark boundary disappears it is the sun terminator, not the mesh/normals
            if (sun != null) { sun.enabled = false; CaptureOne(r.transform, "diag_nolight"); sun.enabled = true; }
        }
        finally
        {
            r.sharedMaterials = origMats;
            r.shadowCastingMode = origCast;
            if (sun != null) sun.enabled = true;
            foreach (var m in noRecv) Object.DestroyImmediate(m);
        }
    }

    static void CaptureOne(Transform t, string tag)
    {
        var go = new GameObject("~DiagCam") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>();
        var rt = new RenderTexture(600, 600, 24);
        cam.targetTexture = rt;
        cam.fieldOfView = 30f;
        var focus = t.position + Vector3.up * 1.0f;
        cam.transform.position = focus + Quaternion.AngleAxis(35f, Vector3.up) * t.forward * 1.6f;
        cam.transform.LookAt(focus);
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(600, 600, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 600, 600), 0, 0);
        System.IO.File.WriteAllBytes($"Temp/{tag}.png", tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(go);
    }

    static Material SetupMaterials(out Material veil)
    {
        // 아틀라스: RGB=색, A=부위별 스무스니스(피부/머리/천/눈/구두 질감 구분). 베일 영역의 A는 투명도로 사용.
        var ti = (TextureImporter)AssetImporter.GetAtPath(AtlasTex);
        ti.sRGBTexture = true;
        ti.alphaSource = TextureImporterAlphaSource.FromInput;
        ti.alphaIsTransparency = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.mipmapEnabled = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.maxTextureSize = 1024;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.SaveAndReimport();
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasTex);

        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var pal = LoadOrCreate(PaletteMat, lit);
        pal.SetTexture("_BaseMap", tex);
        pal.SetColor("_BaseColor", Color.white);
        pal.SetFloat("_Smoothness", 1f);
        pal.SetFloat("_SmoothnessTextureChannel", 1f);
        pal.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
        pal.SetFloat("_Metallic", 0f);
        // 머리카락이 얼굴에 드리우는 실시간 자기 그림자가 얼룩진 명암 경계를 만들어서 캐릭터는 그림자를 받지 않음
        // (바닥에 드리우는 그림자는 유지). 진단: CaptureShadowDiag
        pal.SetFloat("_ReceiveShadows", 0f);
        pal.EnableKeyword("_RECEIVE_SHADOWS_OFF");
        // 그늘 쪽이 씬의 Flat 앰비언트(회녹색)로 탁해지지 않도록 아틀라스 색으로 약한 자체 발광(피규어 톤 유지)
        pal.SetTexture("_EmissionMap", tex);
        pal.SetColor("_EmissionColor", new Color(0.08f, 0.075f, 0.07f));   // 약하게, 살짝 따뜻하게 (얼굴 창백함 방지)
        pal.EnableKeyword("_EMISSION");
        pal.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;  // EmissiveIsBlack이면 URP 검증이 _EMISSION을 끔
        pal.enableInstancing = true;
        EditorUtility.SetDirty(pal);

        veil = LoadOrCreate(VeilMat, lit);
        veil.SetTexture("_BaseMap", tex);
        veil.SetColor("_BaseColor", Color.white);
        veil.SetFloat("_Smoothness", 0.25f);
        veil.SetFloat("_SmoothnessTextureChannel", 0f);
        veil.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
        veil.SetFloat("_Surface", 1f);
        veil.SetFloat("_Blend", 0f);
        veil.SetFloat("_Cull", 0f);
        veil.SetFloat("_ZWrite", 0f);
        veil.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        veil.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        veil.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        veil.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        veil.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        veil.SetOverrideTag("RenderType", "Transparent");
        veil.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        veil.doubleSidedGI = true;
        // 투명 베일이 얼굴에 직선 그림자를 드리우지 않도록 그림자 캐스팅 끔 (URP는 투명도 ShadowCaster 패스를 그림)
        veil.SetShaderPassEnabled("ShadowCaster", false);
        // 그늘·하늘색 반사로 회색/푸르게 보이지 않도록 흰 면사포 색을 약하게 자체 발광
        veil.SetTexture("_EmissionMap", tex);
        veil.SetColor("_EmissionColor", new Color(0.35f, 0.35f, 0.35f));
        veil.EnableKeyword("_EMISSION");
        veil.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(veil);
        AssetDatabase.SaveAssets();
        return pal;
    }

    static Material LoadOrCreate(string path, Shader shader)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(shader);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void SetupModel(string path, Material palette, Material veil)
    {
        var mi = (ModelImporter)AssetImporter.GetAtPath(path);
        mi.animationType = ModelImporterAnimationType.None;
        mi.importAnimation = false;
        mi.importBlendShapes = false;
        mi.importCameras = false;
        mi.importLights = false;
        mi.importVisibility = false;
        mi.isReadable = false;
        mi.meshCompression = ModelImporterMeshCompression.Medium;
        mi.meshOptimizationFlags = MeshOptimizationFlags.Everything;
        mi.generateSecondaryUV = false;
        mi.importNormals = ModelImporterNormals.Import;
        mi.importTangents = ModelImporterTangents.None;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        // 이 FBX에 없는 머티리얼 이름의 리맵(이전 작업에서 남은 항목)은 제거
        foreach (var kv in mi.GetExternalObjectMap())
            if (kv.Key.type == typeof(Material) && kv.Key.name != "WeddingCouple_Palette" && kv.Key.name != "WeddingCouple_Veil")
                mi.RemoveRemap(kv.Key);
        mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "WeddingCouple_Palette"), palette);
        mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "WeddingCouple_Veil"), veil);
        mi.SaveAndReimport();
    }

    static void ReplaceChildren(Transform anchor, string fbxPath, string name)
    {
        for (int i = anchor.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(anchor.GetChild(i).gameObject);

        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath), anchor);
        model.name = name;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        if (FacesMinusZ()) model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var mesh = model.GetComponentInChildren<MeshFilter>().sharedMesh;
        Debug.Log($"[WeddingCoupleSetup] {name}: rotate180={FacesMinusZ()}, tris={mesh.triangles.Length / 3}, bounds={mesh.bounds}");
    }

    // 정면 판별: 신부 머리 높이 이상 정점 중 뒤쪽(올림머리/베일)이 더 길게 뻗음 → 그 반대가 정면.
    // 두 모델은 같은 Blender 축(-Y 정면)으로 내보내므로 신랑에도 동일하게 적용.
    static bool FacesMinusZ()
    {
        var mesh = AssetDatabase.LoadAssetAtPath<GameObject>(BrideFbx).GetComponentInChildren<MeshFilter>().sharedMesh;
        float minZ = 0f, maxZ = 0f;
        foreach (var v in mesh.vertices)
        {
            if (v.y < 1.05f) continue;
            minZ = Mathf.Min(minZ, v.z);
            maxZ = Mathf.Max(maxZ, v.z);
        }
        return maxZ > -minZ;
    }
}
