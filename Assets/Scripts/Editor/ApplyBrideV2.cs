using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
public static class ApplyBrideV2 {
 const string Dir="Assets/Models/WeddingBrideTripo/";
 const string Out=@"C:\Users\Pc\Documents\Codex\2026-10-02\blender-1-windows-pc-blender-blender\outputs\bride_tripo_v2";
 public static void Run(){
 if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Exit Play mode first");
 var shader=Shader.Find("Wedding/Tripo Soft Lit");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new System.Exception("Shader failed");
 foreach(var p in new[]{"Bride_Tripo_BaseColor.png","Bride_Tripo_Normal.png","Bride_Tripo_ORM.png"}){
 var ti=(TextureImporter)AssetImporter.GetAtPath(Dir+p);ti.textureType=p.Contains("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=p.Contains("BaseColor");ti.maxTextureSize=4096;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=true;ti.anisoLevel=8;ti.filterMode=FilterMode.Trilinear;ti.ClearPlatformTextureSettings("WebGL");ti.ClearPlatformTextureSettings("Standalone");ti.SaveAndReimport();}
 var body=AssetDatabase.LoadAssetAtPath<Material>(Dir+"Bride_Tripo_Body.mat");body.shader=shader;body.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"Bride_Tripo_BaseColor.png"));body.SetColor("_BaseColor",new Color(1.03f,1.065f,1.08f,1));body.SetFloat("_ShadeFloor",.82f);body.SetFloat("_Wrap",.65f);EditorUtility.SetDirty(body);
 var imp=(ModelImporter)AssetImporter.GetAtPath(Dir+"Bride_Tripo.fbx");imp.meshCompression=ModelImporterMeshCompression.Off;imp.importNormals=ModelImporterNormals.Calculate;imp.normalSmoothingAngle=180;imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"Bride_Tripo_V2_Body"),body);imp.SaveAndReimport();AssetDatabase.SaveAssets();
 var bride=GameObject.Find("BrideNPC");if(bride==null)throw new System.Exception("BrideNPC missing");var model=bride.GetComponentsInChildren<Transform>().First(t=>t.name=="BrideModel").gameObject;var f=model.GetComponentInChildren<MeshFilter>();if(AssetDatabase.GetAssetPath(f.sharedMesh)!=Dir+"Bride_Tripo.fbx")throw new System.Exception("Unexpected live mesh reference");
 var capture=typeof(ApplyTripoBride).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static);capture.Invoke(null,new object[]{bride,"BrideV2_after"});
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 var report="Scene: "+scene.path+"\nActive FBX: "+AssetDatabase.GetAssetPath(f.sharedMesh)+"\nVertices: "+f.sharedMesh.vertexCount+"\nNative textures: 2048x2048 uncompressed; import max4096.\n";foreach(var r in model.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)report+="Material: "+(m==null?"NULL":m.name+" / "+m.shader.name)+"\n";File.WriteAllText(Out+"/Unity_verification.txt",report);
 AssetDatabase.ExportPackage(Dir.TrimEnd('/'),Out+"/WeddingBride_v2_finished.unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);Selection.activeGameObject=bride;SceneView.FrameLastActiveSceneView();
 }
 public static void Before(){var capture=typeof(ApplyTripoBride).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static);capture.Invoke(null,new object[]{GameObject.Find("BrideNPC"),"BrideV2_before"});}
}
