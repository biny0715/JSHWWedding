using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;
public static class GroomNeutralV17 {
const string Out=@"C:\Users\Pc\Documents\Codex\2026-10-02\blender-1-windows-pc-blender-blender\outputs\groom_neutral_v17";
public static void Before(){Capture("before");}
public static void After(){if(ShaderUtil.ShaderHasError(Shader.Find("Wedding/Tripo Soft Lit")))throw new System.Exception("Shader compilation failed");Capture("after");AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.ExportPackage(new[]{"Assets/Models/WeddingGroomTripo","Assets/Models/WeddingBrideTripo/TripoSoftLit.shader"},Out+"/Groom_neutral_v17.unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);}
static void Capture(string prefix){
var root=GameObject.Find("HwNPC");var model=root.GetComponentsInChildren<Transform>().First(t=>t.name=="GroomModel").gameObject;var b=(Bounds)typeof(ApplyTripoGroom).GetMethod("BoundsOf",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{model});
var obj=new GameObject("~GroomMainCameraVerify"){hideFlags=HideFlags.HideAndDontSave};var cam=obj.AddComponent<Camera>();var main=Camera.main;if(main!=null)cam.CopyFrom(main);var data=cam.GetUniversalAdditionalCameraData();if(main!=null){var md=main.GetUniversalAdditionalCameraData();data.renderPostProcessing=md.renderPostProcessing;data.volumeLayerMask=md.volumeLayerMask;data.antialiasing=md.antialiasing;}else{data.renderPostProcessing=true;data.volumeLayerMask=~0;}
var rt=new RenderTexture(1200,1600,24);var previous=RenderTexture.active;
try{cam.targetTexture=rt;cam.nearClipPlane=.01f;cam.farClipPlane=100;cam.fieldOfView=35;
foreach(var angle in new[]{0f,35f,-35f,999f}){var target=b.center;float dist=b.size.y*2.1f;float yaw=angle;if(angle==999){target=b.center+Vector3.up*b.size.y*.29f;dist=b.size.y*.85f;yaw=0;}cam.transform.position=target+Quaternion.AngleAxis(yaw,Vector3.up)*root.transform.forward*dist;cam.transform.LookAt(target);VolumeManager.instance.Update(cam.transform,data.volumeLayerMask);cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1200,1600,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1200,1600),0,0);tex.Apply();File.WriteAllBytes(Out+"/"+prefix+"_"+(angle==999?"face":angle.ToString())+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);}
File.WriteAllText(Out+"/camera_settings.txt","Main camera post processing="+data.renderPostProcessing+"; HDR="+cam.allowHDR+"; color space="+QualitySettings.activeColorSpace);
}finally{RenderTexture.active=previous;cam.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(obj);}
}
}
