using UnityEditor;using UnityEngine;using System.IO;using System.Reflection;using System.Linq;
public static class BrideEyesV9 {
 const string Out=@"C:\Users\Pc\Documents\Codex\2026-10-02\blender-1-windows-pc-blender-blender\outputs\bride_eyes_v9";
 public static void Before(){Capture("before");}
 static void Capture(string prefix){
 Directory.CreateDirectory(Out);var root=GameObject.Find("BrideNPC");var model=root.GetComponentsInChildren<Transform>().First(t=>t.name=="BrideModel");var f=model.GetComponentsInChildren<MeshFilter>().First(f=>AssetDatabase.GetAssetPath(f.sharedMesh).EndsWith("Bride_Tripo.fbx"));
 var obj=new GameObject("~FaceVerification"){hideFlags=HideFlags.HideAndDontSave};var cam=obj.AddComponent<Camera>();var rt=new RenderTexture(1200,1200,24);var prev=RenderTexture.active;
 try{cam.targetTexture=rt;cam.nearClipPlane=.01f;cam.farClipPlane=100;cam.fieldOfView=25;
 var target=f.transform.TransformPoint(new Vector3(0,1.115f,.12f));var scale=f.transform.TransformVector(Vector3.up).magnitude;
 foreach(var angle in new[]{0f,35f,-35f,90f}){cam.transform.position=target+Quaternion.AngleAxis(angle,Vector3.up)*root.transform.forward*scale*1.8f;cam.transform.LookAt(target);cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1200,1200,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1200,1200),0,0);tex.Apply();File.WriteAllBytes(Out+"/"+prefix+"_face_"+angle+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);}
 }finally{RenderTexture.active=prev;cam.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(obj);}
 }
 static void SetupEyes(){
 const string dir="Assets/Models/WeddingBrideTripo/";var shader=Shader.Find("Wedding/Tripo Soft Lit");
 foreach(var name in new[]{"BrideEye_White","BrideEye_Iris","BrideEye_Lid"}){
 var path=dir+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}mat.shader=shader;mat.SetFloat("_BrideFinish",0);mat.SetFloat("_GroomSkin",0);mat.SetFloat("_ShadeFloor",.90f);
 mat.SetColor("_BaseColor",name.Contains("White")?new Color(.94f,.96f,.97f):name.Contains("Lid")?new Color(.035f,.020f,.015f):Color.white);
 if(name.Contains("Iris")){mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"Bride_Iris.png"));mat.SetFloat("_EyeFinish",1);}EditorUtility.SetDirty(mat);
 var importer=(ModelImporter)AssetImporter.GetAtPath(dir+"Bride_Tripo.fbx");importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),mat);
 }
 AssetDatabase.SaveAssets();AssetImporter.GetAtPath(dir+"Bride_Tripo.fbx").SaveAndReimport();
 var root=GameObject.Find("BrideNPC");foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>()){
 var materials=renderer.sharedMaterials;for(int j=0;j<materials.Length;j++)if(materials[j]!=null&&materials[j].name.StartsWith("BrideEye_")){materials[j]=AssetDatabase.LoadAssetAtPath<Material>(dir+materials[j].name+".mat");}renderer.sharedMaterials=materials;
 }
 }
 public static void Run(){
 SetupEyes();
 const string dir="Assets/Models/WeddingBrideTripo/";var shader=Shader.Find("Wedding/Tripo Soft Lit");if(ShaderUtil.ShaderHasError(shader))throw new System.Exception("Shader compile failed");
 foreach(var name in new[]{"Bride_Tripo_BaseColor.png","Bride_Tripo_Normal.png","Bride_Tripo_ORM.png","Bride_Iris.png"}){
 var ti=AssetImporter.GetAtPath(dir+name) as TextureImporter;if(ti==null)continue;
 ti.isReadable=false;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.compressionQuality=80;ti.crunchedCompression=false;ti.mipmapEnabled=true;ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=4;
 var p=ti.GetPlatformTextureSettings("WebGL");p.name="WebGL";p.overridden=true;p.maxTextureSize=name.Contains("Iris")?1024:2048;p.format=name.Contains("Normal")?TextureImporterFormat.ETC2_RGBA8:TextureImporterFormat.ETC2_RGB4;p.compressionQuality=80;ti.SetPlatformTextureSettings(p);ti.SaveAndReimport();
 }
 var body=AssetDatabase.LoadAssetAtPath<Material>(dir+"Bride_Tripo_Body.mat");body.SetTexture("_IrisMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"Bride_Iris.png"));body.SetFloat("_BrideFinish",1);body.SetFloat("_ShadeFloor",.77f);EditorUtility.SetDirty(body);AssetDatabase.SaveAssets();
 var gm=AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/WeddingGroomTripo/Groom_Tripo_Body.mat");gm.SetFloat("_GroomSkin",1);EditorUtility.SetDirty(gm);AssetDatabase.SaveAssets(); Capture("after");typeof(ApplyTripoGroom).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{GameObject.Find("HwNPC"),"EyesV9"});typeof(ApplyTripoBride).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{GameObject.Find("BrideNPC"),"BrideEyesV9"});UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
 AssetDatabase.ExportPackage(new[]{dir.TrimEnd('/'),"Assets/Models/WeddingGroomTripo"},Out+"/WeddingCouple_eyes_v9.unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);
 File.WriteAllText(Out+"/Compression.txt",string.Join("\n",new[]{"Bride_Tripo_BaseColor.png","Bride_Tripo_Normal.png","Bride_Tripo_ORM.png","Bride_Iris.png"}.Select(n=>{var t=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+n);var p=((TextureImporter)AssetImporter.GetAtPath(dir+n)).GetPlatformTextureSettings("WebGL");return n+" : "+t.width+"x"+t.height+" "+t.format+" target="+p.format+" bytes="+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t);})));
 }
}



