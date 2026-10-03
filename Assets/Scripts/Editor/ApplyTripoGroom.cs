using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class ApplyTripoGroom {
 const string Dir="Assets/Models/WeddingGroomTripo/";
 const string Model=Dir+"Groom_Tripo.fbx";
 const string Prefab="Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Prefabs/HwNPC.prefab";
 const string Out=@"C:\Users\Pc\Documents\Codex\2026-10-02\blender-1-windows-pc-blender-blender\outputs\groom_tripo_finish";
 static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
 static void Replace(GameObject root){
 var old=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="GroomModel");
 if(old.GetComponentsInChildren<MeshFilter>().Any(f=>AssetDatabase.GetAssetPath(f.sharedMesh)==Model))return;
 var b=BoundsOf(old.gameObject);var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model),old.parent);go.name="GroomModel";go.transform.localPosition=old.localPosition;go.transform.localRotation=old.localRotation;go.transform.localScale=old.localScale;
 var nb=BoundsOf(go);go.transform.localScale*=b.size.y/nb.size.y;nb=BoundsOf(go);go.transform.position+=new Vector3(b.center.x-nb.center.x,b.min.y-nb.min.y,b.center.z-nb.center.z);old.name="GroomModel_PreTripo_Backup";old.gameObject.SetActive(false);
 }
 public static void Run(){
 if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode");
 Directory.CreateDirectory(Out);
 var shader=Shader.Find("Wedding/Tripo Soft Lit");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new Exception("Shader error");
 var mat=AssetDatabase.LoadAssetAtPath<Material>(Dir+"Groom_Tripo_Body.mat");if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,Dir+"Groom_Tripo_Body.mat");}mat.shader=shader;mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_ShadeFloor",.68f);mat.SetFloat("_Wrap",.6f);
 foreach(var p in new[]{"Groom_Tripo_BaseColor.png","Groom_Tripo_Normal.png","Groom_Tripo_ORM.png"}){
 var ti=(TextureImporter)AssetImporter.GetAtPath(Dir+p);ti.textureType=p.Contains("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=p.Contains("BaseColor");ti.maxTextureSize=4096;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=true;ti.anisoLevel=8;ti.filterMode=FilterMode.Trilinear;ti.ClearPlatformTextureSettings("WebGL");ti.ClearPlatformTextureSettings("Standalone");ti.SaveAndReimport();}
 mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"Groom_Tripo_BaseColor.png"));EditorUtility.SetDirty(mat);
 var imp=(ModelImporter)AssetImporter.GetAtPath(Model);imp.meshCompression=ModelImporterMeshCompression.Off;imp.importNormals=ModelImporterNormals.Calculate;imp.normalSmoothingAngle=180;imp.importCameras=false;imp.importLights=false;imp.animationType=ModelImporterAnimationType.None;imp.importAnimation=false;imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"Groom_Tripo_Body"),mat);imp.SaveAndReimport();
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var groom=GameObject.Find("HwNPC");if(groom==null)throw new Exception("HwNPC not found");if(!File.Exists(Out+"/Unity_before_0.png"))Capture(groom,"Unity_before");
 var root=PrefabUtility.LoadPrefabContents(Prefab);try{Replace(root);PrefabUtility.SaveAsPrefabAsset(root,Prefab);}finally{PrefabUtility.UnloadPrefabContents(root);}
 groom=GameObject.Find("HwNPC");var active=groom.GetComponentsInChildren<MeshFilter>();if(!active.Any(f=>AssetDatabase.GetAssetPath(f.sharedMesh)==Model))throw new Exception("Scene did not use new FBX");
 EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save error");AssetDatabase.SaveAssets();Capture(groom,"Unity_after");
 var report=new StringBuilder("Scene: "+scene.path+"\nNative textures: 2048 x 2048. Import max4096, uncompressed, mipmaps, anisotropy8.\n");foreach(var f in active)report.AppendLine(f.name+" => "+AssetDatabase.GetAssetPath(f.sharedMesh)+" vertices="+f.sharedMesh.vertexCount);foreach(var r in groom.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)report.AppendLine("Material: "+(m==null?"NULL":m.name+" / "+m.shader.name));report.AppendLine("Animation: static. No animation rig in generated source.");File.WriteAllText(Out+"/Unity_verification.txt",report.ToString());
 AssetDatabase.ExportPackage(Dir.TrimEnd('/'),Out+"/WeddingGroom_finished.unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);Selection.activeGameObject=groom;SceneView.FrameLastActiveSceneView();
 }
 static void Capture(GameObject root,string prefix){
 var model=root.GetComponentsInChildren<Transform>().First(t=>t.name=="GroomModel").gameObject;var b=BoundsOf(model);var obj=new GameObject("~GroomVerification"){hideFlags=HideFlags.HideAndDontSave};var cam=obj.AddComponent<Camera>();var rt=new RenderTexture(1200,1600,24);var previous=RenderTexture.active;
 try{cam.targetTexture=rt;cam.nearClipPlane=.01f;cam.farClipPlane=100;cam.fieldOfView=35;
 foreach(var angle in new[]{0f,35f,-35f,90f,180f,999f}){var target=b.center;float dist=b.size.y*2.1f;float yaw=angle;if(angle==999){target=b.center+Vector3.up*b.size.y*.29f;dist=b.size.y*.85f;yaw=0;}var dir=Quaternion.AngleAxis(yaw,Vector3.up)*root.transform.forward;cam.transform.position=target+dir*dist;cam.transform.LookAt(target);cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1200,1600,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1200,1600),0,0);tex.Apply();File.WriteAllBytes(Out+"/"+prefix+"_"+(angle==999?"face":angle.ToString())+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
 }finally{RenderTexture.active=previous;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
 }
}


