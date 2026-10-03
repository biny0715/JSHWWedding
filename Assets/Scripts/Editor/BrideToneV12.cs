using UnityEditor;using UnityEngine;using System.IO;using System.Reflection;
public static class BrideToneV12 {
const string Out=@"C:\Users\Pc\Documents\Codex\2026-10-02\blender-1-windows-pc-blender-blender\outputs\bride_tone_v12";
public static void Run(){
var shader=Shader.Find("Wedding/Tripo Soft Lit");if(ShaderUtil.ShaderHasError(shader))throw new System.Exception("Shader compilation failed");
typeof(BrideRestoreV10).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"ToneV12"});
foreach(var angle in new[]{"0","35","-35","90"})File.Copy(Out.Replace("bride_tone_v12","bride_restore_v10")+"/ToneV12_face_"+angle+".png",Out+"/after_face_"+angle+".png",true);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets(); typeof(ApplyTripoGroom).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{GameObject.Find("HwNPC"),"ToneV12Groom"});
AssetDatabase.ExportPackage(new[]{"Assets/Models/WeddingBrideTripo","Assets/Models/WeddingGroomTripo"},Out+"/WeddingBride_tone_v12.unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);
}
}


