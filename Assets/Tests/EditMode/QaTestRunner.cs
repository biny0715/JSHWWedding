using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace JSHWWedding.Tests.EditMode
{
    // 에디터에서 EditMode 테스트를 프로그램적으로 실행하고 결과 요약을 콘솔에 남긴다.
    // (CI/헤드리스 실행의 축소판 — Test Runner 창 없이도 결과를 로그로 확인)
    public static class QaTestRunner
    {
        [MenuItem("QA/Run EditMode Tests")]
        public static void Run()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
            Debug.Log("[QA] EditMode 테스트 실행 시작…");
        }

        private class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                Debug.Log(
                    $"[QA] EditMode 결과 — passed={result.PassCount} failed={result.FailCount} " +
                    $"skipped={result.SkipCount} duration={result.Duration:F2}s status={result.TestStatus}");
            }
        }
    }
}
