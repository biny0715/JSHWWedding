// StripMcpFromBuild.cs
// realvirtual-MCP(에디터 전용 Unity MCP 서버: 임베디드 Python + .git)는 게임 런타임에
// 전혀 쓰이지 않지만 Assets/StreamingAssets 안에 있어 빌드에 통째로 복사된다(~105MB).
// 빌드가 끝난 뒤 결과물에서 해당 폴더만 자동 삭제해 배포 용량을 줄인다.
// (프로젝트의 Assets 는 건드리지 않으므로 에디터 MCP 연결/동작에 영향 없음)

using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JSHWWedding.EditorTools
{
    public class StripMcpFromBuild : IPostprocessBuildWithReport
    {
        // 다른 후처리보다 늦게 돌 필요는 없음
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            string outputPath = report.summary.outputPath;
            if (string.IsNullOrEmpty(outputPath)) return;

            // WebGL 은 outputPath 가 빌드 폴더, 그 외 플랫폼은 실행파일 경로일 수 있어 디렉터리로 보정
            string buildDir = Directory.Exists(outputPath) ? outputPath : Path.GetDirectoryName(outputPath);
            if (string.IsNullOrEmpty(buildDir)) return;

            string mcpDir = Path.Combine(buildDir, "StreamingAssets", "realvirtual-MCP");
            if (Directory.Exists(mcpDir))
            {
                try
                {
                    Directory.Delete(mcpDir, true);
                    Debug.Log($"[StripMcpFromBuild] 빌드 결과물에서 제거됨: {mcpDir}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[StripMcpFromBuild] 제거 실패: {e.Message}");
                    return;
                }
            }

            // StreamingAssets 가 비었으면 빈 폴더도 정리
            string saDir = Path.Combine(buildDir, "StreamingAssets");
            if (Directory.Exists(saDir) && Directory.GetFileSystemEntries(saDir).Length == 0)
            {
                try { Directory.Delete(saDir); } catch { /* 무시 */ }
            }
        }
    }
}
