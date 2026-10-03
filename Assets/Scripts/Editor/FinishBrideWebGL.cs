using UnityEditor;using UnityEngine;using System.Reflection;using System.IO;using System.Linq;
public static class FinishBrideWebGL {
 public static void Run(){ typeof(Editor).Assembly.GetType("UnityEditor.LogEntries").GetMethod("Clear",BindingFlags.Static|BindingFlags.Public).Invoke(null,null);
 var dir="Assets/Models/WeddingBrideTripo/";
 var shader=Shader.Find("Wedding/Tripo Soft Lit");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new System.Exception("Shader compile error");
 foreach(var p in AssetDatabase.FindAssets("t:Material",new[]{dir,"Assets/Models/WeddingGroomTripo"})){var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(p));if(m.shader==shader){m.shaderKeywords=new string[0];m.SetFloat("_BrideFinish",m.name.Contains("Bride")?1:0);if(m.name.Contains("Bride")){m.SetColor("_BaseColor",Color.white);m.SetFloat("_ShadeFloor",.82f);}EditorUtility.SetDirty(m);}}
 AssetDatabase.SaveAssets(); AssetDatabase.ExportPackage(dir.TrimEnd('/'),@"C:\Users\Pc\Documents\Codex\2026-10-02\blender-1-windows-pc-blender-blender\outputs\bride_webgl_v3\WeddingBride_webgl_v3.unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);
 var bride=GameObject.Find("BrideNPC");typeof(ApplyTripoBride).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{bride,"BrideWebGL_v3"});
 var f=bride.GetComponentsInChildren<MeshFilter>().First(x=>AssetDatabase.GetAssetPath(x.sharedMesh).Contains("Bride_Tripo.fbx"));Debug.Log("BRIDE_V3 bounds="+f.sharedMesh.bounds);
 UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
 }
}

