using UnityEditor;using UnityEngine;using System.IO;using System.Reflection;
public static class BrideDetailV15 {
const string Out=@"C:\Users\Pc\Documents\Codex\2026-10-02\blender-1-windows-pc-blender-blender\outputs\bride_detail_v15";
public static void Run(){
const string dir="Assets/Models/WeddingBrideTripo/";
var lash=AssetDatabase.LoadAssetAtPath<Material>(dir+"Bride_CornerLash.mat");if(lash==null){lash=new Material(Shader.Find("Wedding/Tripo Soft Lit"));AssetDatabase.CreateAsset(lash,dir+"Bride_CornerLash.mat");}lash.SetColor("_BaseColor",new Color(.04f,.022f,.015f));lash.SetFloat("_ShadeFloor",.85f);EditorUtility.SetDirty(lash);
var importer=(ModelImporter)AssetImporter.GetAtPath(dir+"Bride_Tripo.fbx");importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"Bride_CornerLash"),lash);importer.SaveAndReimport();
foreach(var r in GameObject.Find("BrideNPC").GetComponentsInChildren<MeshRenderer>()){var f=r.GetComponent<MeshFilter>();if(f!=null&&AssetDatabase.GetAssetPath(f.sharedMesh).EndsWith("Bride_Tripo.fbx")){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(dir+"Bride_Tripo.fbx");var source=prefab.GetComponentInChildren<MeshRenderer>();var mats=source.sharedMaterials;for(int j=0;j<mats.Length;j++){var n=mats[j].name;if(n.Contains("V2_Body"))mats[j]=AssetDatabase.LoadAssetAtPath<Material>(dir+"Bride_Tripo_Body.mat");else if(n.Contains("CornerLash"))mats[j]=lash;}r.sharedMaterials=mats;}}
var shader=Shader.Find("Wedding/Tripo Soft Lit");if(ShaderUtil.ShaderHasError(shader))throw new System.Exception("Shader compilation failed");
typeof(BrideRestoreV10).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"DetailV15"});
foreach(var angle in new[]{"0","35","-35","90"})File.Copy(Out.Replace("bride_detail_v15","bride_restore_v10")+"/DetailV15_face_"+angle+".png",Out+"/after_face_"+angle+".png",true);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets(); typeof(ApplyTripoGroom).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{GameObject.Find("HwNPC"),"EarMatchV15"});
AssetDatabase.ExportPackage(new[]{"Assets/Models/WeddingBrideTripo","Assets/Models/WeddingGroomTripo"},Out+"/WeddingCouple_detail_v15.unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);
}
}


