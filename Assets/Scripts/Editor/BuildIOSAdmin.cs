// BuildIOSAdmin.cs
// 관리자(아이패드) 앱 iOS 빌드 설정·빌드.
//  - WebGL 빌드 씬 목록(EditorBuildSettings: Lobby, Wedding)은 건드리지 않고, iOS 빌드에는 WeddingAdmin 씬 하나만 넣는다.
//  - 설정은 iOS 전용 값만 바꾼다(번들 ID·기기·서명·IL2CPP). 화면 방향은 가로 고정 — WebGL 은 화면 방향 설정을 쓰지 않음.
//  - 출력은 Xcode 프로젝트 → Xcode 에서 Team 선택 후 아이패드에 설치.
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class BuildIOSAdmin
{
    const string BundleId = "com.biny.jshwwedding.admin";
    const string Output = "/Users/biny/Builds/admin-ios";

    [MenuItem("Tools/Admin/iOS 설정 적용 + 플랫폼 iOS 로 전환")]
    public static void ApplySettingsAndSwitch()
    {
        ApplySettings();
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
        Debug.Log($"[BuildIOSAdmin] 플랫폼: {EditorUserBuildSettings.activeBuildTarget}");
    }

    public static void ApplySettings()
    {
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPadOnly;
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;   // Team 은 Xcode 에서 선택

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.statusBarHidden = true;

        AssetDatabase.SaveAssets();
        Debug.Log($"[BuildIOSAdmin] iOS 설정 적용: {BundleId}, 아이패드 전용, 가로, IL2CPP");
    }

    [MenuItem("Tools/Admin/Build iOS Admin (Xcode 프로젝트)")]
    public static void Build()
    {
        ApplySettings();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { WeddingAdminSceneBuilder.Dst },
            locationPathName = Output,
            target = BuildTarget.iOS,
            options = BuildOptions.None,
        });
        var s = report.summary;
        Debug.Log($"[BuildIOSAdmin] {s.result} | {s.totalErrors} errors | {s.totalTime} | {Output}");
    }
}
