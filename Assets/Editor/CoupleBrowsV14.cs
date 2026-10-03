using UnityEditor;using UnityEngine;using System.IO;using System.Reflection;
public static class CoupleBrowsV14 {
const string Out=@"C:\Users\Pc\Documents\Codex\2026-10-02\blender-1-windows-pc-blender-blender\outputs\couple_brows_v14";
public static void Run(){
var shader=Shader.Find("Wedding/Tripo Soft Lit");if(ShaderUtil.ShaderHasError(shader))throw new System.Exception("Shader compilation failed");
typeof(BrideRestoreV10).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"BrowsV14"});
foreach(var angle in new[]{"0","35","-35","90"})File.Copy(Out.Replace("couple_brows_v14","bride_restore_v10")+"/BrowsV14_face_"+angle+".png",Out+"/after_face_"+angle+".png",true);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets(); typeof(ApplyTripoGroom).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{GameObject.Find("HwNPC"),"BrightV14"});
AssetDatabase.ExportPackage(new[]{"Assets/Models/WeddingBrideTripo","Assets/Models/WeddingGroomTripo"},Out+"/WeddingCouple_brows_v14.unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);
}
}


