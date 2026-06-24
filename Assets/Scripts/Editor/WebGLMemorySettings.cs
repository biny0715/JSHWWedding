// WebGLMemorySettings.cs (Editor)
// Tools/JSHW/Apply WebGL Memory Settings — iOS Safari 메모리/용량 절감용 Player Settings 일괄 적용.
//  - Managed Stripping: Medium (High는 Photon 등 리플렉션 코드를 침묵 제거할 위험 → link.xml 없이는 위험. Medium이 안전한 절충)
//  - WebGL Exception Support: ExplicitlyThrownExceptionsOnly (Full+Stacktrace는 코드/메모리 큼)
//  - Strip Engine Code: 켜기
//  - Memory: Geometric 성장 + 초기 32MB (iOS가 큰 선예약 거부하는 것 회피)
// 적용 후 WebGL 재빌드하면 반영된다.
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace JSHWWedding.Customization.EditorTools
{
    public static class WebGLMemorySettings
    {
        [MenuItem("Tools/JSHW/Apply WebGL Memory Settings")]
        public static void Apply()
        {
            string Snapshot() =>
                $"strip={PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL)}, " +
                $"exception={PlayerSettings.WebGL.exceptionSupport}, stripEngineCode={PlayerSettings.stripEngineCode}, " +
                $"growth={PlayerSettings.WebGL.memoryGrowthMode}, init={PlayerSettings.WebGL.initialMemorySize}MB, max={PlayerSettings.WebGL.maximumMemorySize}MB";

            string before = Snapshot();

            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.initialMemorySize = 32;

            AssetDatabase.SaveAssets();
            Debug.Log($"[WebGLMem] 변경 전: {before}\n[WebGLMem] 변경 후: {Snapshot()}");
        }
    }
}
