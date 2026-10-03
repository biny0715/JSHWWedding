using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class ApplyTripoBride {
 const string Model="Assets/Models/WeddingBrideTripo/Bride_Tripo.fbx";
 const string Prefab="Assets/Photon/PhotonUnityNetworking/Demos/PunBasics-Tutorial/Prefabs/BrideNPC.prefab";
 const string Out=@"C:\Users\Pc\Documents\Codex\2026-10-02\blender-1-windows-pc-blender-blender\outputs\tripo_scene";
 static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>(); var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
 static void Replace(GameObject root){
  var old=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="BrideModel");
  if(old==null)throw new Exception("BrideModel missing");
  if(old.GetComponentsInChildren<MeshFilter>().Any(f=>AssetDatabase.GetAssetPath(f.sharedMesh)==Model))return;
  var b=BoundsOf(old.gameObject);var parent=old.parent;
  var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model),parent);
  go.name="BrideModel";go.transform.localPosition=old.localPosition;go.transform.localRotation=old.localRotation;go.transform.localScale=old.localScale;
  var nb=BoundsOf(go);float ratio=b.size.y/nb.size.y;go.transform.localScale*=ratio;
  nb=BoundsOf(go);go.transform.position+=new Vector3(b.center.x-nb.center.x,b.min.y-nb.min.y,b.center.z-nb.center.z);
  old.name="BrideModel_PreTripo_Backup";old.gameObject.SetActive(false);
 }
 public static void Run(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  var bride=GameObject.Find("BrideNPC");if(bride==null)throw new Exception("BrideNPC not in current scene");
  Directory.CreateDirectory(Out);File.Copy(scene.path,Out+"/Wedding_before.unity",true);File.Copy(Prefab,Out+"/BrideNPC_before.prefab",true);
  Capture(bride,"before");

  var root=PrefabUtility.LoadPrefabContents(Prefab);try{Replace(root);PrefabUtility.SaveAsPrefabAsset(root,Prefab);}finally{PrefabUtility.UnloadPrefabContents(root);}
  EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");AssetDatabase.SaveAssets();
  bride=GameObject.Find("BrideNPC");Capture(bride,"after");
  var report=new StringBuilder("Scene: "+scene.path+"\n");
  foreach(var f in bride.GetComponentsInChildren<MeshFilter>())report.AppendLine(f.name+" => "+AssetDatabase.GetAssetPath(f.sharedMesh));
  foreach(var r in bride.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)report.AppendLine("Material: "+(m==null?"NULL":m.name+" / "+m.shader.name));
  report.AppendLine("Animator count: "+bride.GetComponentsInChildren<Animator>().Length);
  if(!bride.GetComponentsInChildren<MeshFilter>().Any(f=>AssetDatabase.GetAssetPath(f.sharedMesh)==Model))throw new Exception("New model not active");
  File.WriteAllText(Out+"/verification.txt",report.ToString());Selection.activeGameObject=bride;SceneView.FrameLastActiveSceneView();
 }
 static void Capture(GameObject root,string prefix){
  var model=root.GetComponentsInChildren<Transform>().First(t=>t.name=="BrideModel").gameObject;var b=BoundsOf(model);
  var obj=new GameObject("~TripoVerification"){hideFlags=HideFlags.HideAndDontSave};var cam=obj.AddComponent<Camera>();var rt=new RenderTexture(900,1200,24);var previous=RenderTexture.active;
  try{cam.targetTexture=rt;cam.nearClipPlane=.01f;cam.farClipPlane=100;cam.fieldOfView=35;
   foreach(var angle in new[]{0f,35f,-35f,90f,180f}){var target=b.center;var dir=Quaternion.AngleAxis(angle,Vector3.up)*root.transform.forward;cam.transform.position=target+dir*b.size.y*2.1f;cam.transform.LookAt(target);cam.Render();RenderTexture.active=rt;var tex=new Texture2D(900,1200,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,900,1200),0,0);tex.Apply();File.WriteAllBytes(Out+"/"+prefix+"_"+angle+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
  }finally{RenderTexture.active=previous;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
 }
}


