using UnityEngine;
using UnityEditor;
using System.Reflection;
public static class TripoDiagnostics {
 public static void Run(){
 var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/WeddingBrideTripo/Bride_Tripo_Body.mat");var backup=new Material(m);
 var capture=typeof(ApplyTripoBride).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static);var bride=GameObject.Find("BrideNPC");
 try{capture.Invoke(null,new object[]{bride,"diag_original"});m.DisableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",0);capture.Invoke(null,new object[]{bride,"diag_no_normal"});m.SetTexture("_BaseMap",null);m.SetColor("_BaseColor",new Color(.85f,.65f,.55f));capture.Invoke(null,new object[]{bride,"diag_solid"});m.shader=Shader.Find("Universal Render Pipeline/Unlit");m.SetTexture("_BaseMap",backup.GetTexture("_BaseMap"));m.SetColor("_BaseColor",Color.white);capture.Invoke(null,new object[]{bride,"diag_unlit"});}
 finally{m.shader=backup.shader;m.CopyPropertiesFromMaterial(backup);Object.DestroyImmediate(backup);}
 }
}
