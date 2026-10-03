using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.IO;
public static class TripoFinishUnity {
 public static void Run(){
 const string dir="Assets/Models/WeddingBrideTripo/";
 var imp=(ModelImporter)AssetImporter.GetAtPath(dir+"Bride_Tripo.fbx");imp.meshCompression=ModelImporterMeshCompression.Off;imp.importNormals=ModelImporterNormals.Calculate;imp.normalSmoothingAngle=150;imp.SaveAndReimport();
 var shader=Shader.Find("Wedding/Tripo Soft Lit");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new System.Exception("Soft lighting shader compilation failed");
 var body=AssetDatabase.LoadAssetAtPath<Material>(dir+"Bride_Tripo_Body.mat");body.shader=shader;body.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"Bride_Tripo_BaseColor.png"));body.SetColor("_BaseColor",new Color(1f,1.035f,1.05f,1));body.SetFloat("_ShadeFloor",.78f);body.SetFloat("_Wrap",.65f);EditorUtility.SetDirty(body);
 var veil=AssetDatabase.LoadAssetAtPath<Material>(dir+"Bride_Tripo_Veil.mat");veil.DisableKeyword("_EMISSION");veil.SetColor("_EmissionColor",Color.black);EditorUtility.SetDirty(veil);
 AssetDatabase.SaveAssets(); AssetDatabase.ExportPackage(dir.TrimEnd('/'),@"C:\Users\Pc\Documents\Codex\2026-10-02\blender-1-windows-pc-blender-blender\outputs\tripo_finish\WeddingBride_finished.unitypackage",ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
 var capture=typeof(ApplyTripoBride).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static);capture.Invoke(null,new object[]{GameObject.Find("BrideNPC"),"finished"});
 File.WriteAllText(@"C:\Users\Pc\Documents\Codex\2026-10-02\blender-1-windows-pc-blender-blender\outputs\tripo_finish\unity_verified.txt","Soft Lit shader compiled. New FBX imported, mesh compression off, normals recalculated. Actual BrideNPC scene captures saved. No animation added.\n");
 }
}

