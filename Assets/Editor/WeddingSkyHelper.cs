// WeddingSkyHelper.cs
// 일회성 에디터 유틸 — Wedding 씬의 하늘/안개를 정리한다.
// realvirtual MCP 의 editor_invoke_method 로 JSHWWedding.WeddingSkyHelper.ApplyCleanSky 를 호출해 실행.
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace JSHWWedding
{
    public static class WeddingSkyHelper
    {
        //! 안개 OFF + 프로시저럴 맑은 하늘 적용 + 씬 저장 (한 번에)
        public static void ApplyCleanSky()
        {
            // 1) 전역 안개 끄기
            RenderSettings.fog = false;

            // 2) 프로시저럴 맑은 하늘 머티리얼 생성
            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                Debug.LogError("[WeddingSkyHelper] 'Skybox/Procedural' 셰이더를 못 찾음");
                return;
            }
            var mat = new Material(shader) { name = "CleanProceduralSky" };
            if (mat.HasProperty("_SunSize")) mat.SetFloat("_SunSize", 0.035f);
            if (mat.HasProperty("_SunSizeConvergence")) mat.SetFloat("_SunSizeConvergence", 6f);
            if (mat.HasProperty("_AtmosphereThickness")) mat.SetFloat("_AtmosphereThickness", 1.0f);
            if (mat.HasProperty("_Exposure")) mat.SetFloat("_Exposure", 1.25f);

            const string path = "Assets/Settings/CleanProceduralSky.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();

            // 3) 씬 스카이박스로 지정
            RenderSettings.skybox = mat;
            DynamicGI.UpdateEnvironment();

            // 4) 씬 dirty 표시 + 저장
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[WeddingSkyHelper] 안개 OFF + 프로시저럴 하늘 적용 + 씬 저장 완료.");
        }

        //! Carpet_White 를 터레인 표면 바로 위로 정밀 배치 (z-fighting/묻힘 방지).
        //! 터레인 높이를 SampleHeight 로 샘플링해 카펫 바닥이 표면 + 5cm 에 오게 한다.
        public static void RaiseCarpetAboveTerrain()
        {
            var go = GameObject.Find("WeddingEnviroments/Party/Carpet_White");
            if (go == null) { Debug.LogError("[WeddingSkyHelper] Carpet_White 못 찾음"); return; }

            var rend = go.GetComponent<Renderer>();
            Vector3 pos = go.transform.position;

            var terrain = Terrain.activeTerrain;
            float groundY = pos.y;
            if (terrain != null)
                groundY = terrain.transform.position.y + terrain.SampleHeight(pos);
            else
                Debug.LogWarning("[WeddingSkyHelper] activeTerrain 없음 — 현재 높이 기준 사용");

            float bottom = rend != null ? rend.bounds.min.y : pos.y;
            float pivotToBottom = pos.y - bottom;      // 피벗이 바닥보다 얼마나 위인지
            const float offset = 0.05f;                // 표면 위 5cm (z-fight 방지)
            float newPivotY = groundY + offset + pivotToBottom;

            go.transform.position = new Vector3(pos.x, newPivotY, pos.z);

            EditorSceneManager.MarkSceneDirty(go.scene);
            EditorSceneManager.SaveScene(go.scene);
            Debug.Log($"[WeddingSkyHelper] Carpet 배치: groundY={groundY:F3}, 바닥→{groundY + offset:F3}, pivotY={newPivotY:F3}");
        }

        //! 경사면 대응: 원래 기울기(경사 conform) 복원 + 레이캐스트로 실제 지면 찾아 그 위 clearance 만큼 띄움.
        public static void ConformCarpetToSlope()
        {
            var go = GameObject.Find("WeddingEnviroments/Party/Carpet_White");
            if (go == null) { Debug.LogError("[WeddingSkyHelper] Carpet_White 못 찾음"); return; }

            // 1) 원래 경사 기울기 복원 (씬 작성자가 경사에 맞춰 둔 값)
            go.transform.rotation = Quaternion.Euler(355.647369f, 312.4423f, 6.26946f);

            // 2) 카펫 중심 위 50m 에서 아래로 레이캐스트 → 실제 지면 표면 (카펫엔 콜라이더 없음)
            var rend = go.GetComponent<Renderer>();
            Vector3 pos = go.transform.position;
            Vector3 centerXZ = rend != null ? new Vector3(rend.bounds.center.x, pos.y, rend.bounds.center.z) : pos;
            Vector3 rayStart = new Vector3(centerXZ.x, centerXZ.y + 50f, centerXZ.z);
            float groundY;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 300f))
            {
                groundY = hit.point.y;
                Debug.Log($"[WeddingSkyHelper] 레이캐스트 지면 히트: {hit.collider.name} @ y={groundY:F3}");
            }
            else
            {
                var terrain = Terrain.activeTerrain;
                groundY = terrain != null ? terrain.transform.position.y + terrain.SampleHeight(pos) : pos.y;
                Debug.LogWarning($"[WeddingSkyHelper] 레이캐스트 실패 → SampleHeight 폴백 y={groundY:F3}");
            }

            // 3) 카펫 중심이 지면 위 clearance 오도록 피벗 Y 조정 (기울기가 경사와 맞으면 전체가 그만큼 뜸)
            const float clearance = 0.08f;
            float centerY = rend != null ? rend.bounds.center.y : pos.y;
            float pivotToCenter = pos.y - centerY;
            float newPivotY = groundY + clearance + pivotToCenter;
            go.transform.position = new Vector3(pos.x, newPivotY, pos.z);

            EditorSceneManager.MarkSceneDirty(go.scene);
            EditorSceneManager.SaveScene(go.scene);
            Debug.Log($"[WeddingSkyHelper] Carpet 경사복원+리프트: ground={groundY:F3}, 중심→{groundY + clearance:F3}, pivotY={newPivotY:F3}");
        }
    }
}
