using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using JSHWWedding;

// NPC 대화 카메라 구도 미리보기 — NpcDialogCamera.DoFocus 와 같은 계산으로 임시 카메라를 놓고 캡처(씬 변경 없음).
// 출력: Temp/npccam/<npc>_<tag>.png (iPhone Safari 의 실제 게임 영역 비율)
public static class NpcCamPreview
{
    const float CamRaise = 0.3f;   // NpcDialogCamera.CamRaise 와 동일

    public static void Capture()
    {
        Directory.CreateDirectory("Temp/npccam");
        // 바라보는 높이(headHeight) 후보 비교 + 현재 적용값(cur)
        Shot("신랑\n김형원", "groom", 0f, InteractionZone.GroomCamDistance,
             ("prev", 0.55f), ("cur", InteractionZone.GroomCamHeight));
        Shot("신부\n박지수", "bride", 0.45f, InteractionZone.BrideCamDistance,
             ("prev", 0.55f), ("cur", InteractionZone.BrideCamHeight));
    }

    static void Shot(string tagName, string file, float extraRaise, float dist, params (string tag, float headHeight)[] variants)
    {
        Transform npc = null;
        foreach (var t in Object.FindObjectsByType<PlayerNameTag>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.overrideName == tagName) npc = t.transform;
        if (npc == null) { Debug.LogWarning("[NpcCamPreview] NPC 없음: " + tagName); return; }

        var main = Camera.main;
        var go = new GameObject("~NpcCamPreview") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>();
        if (main != null) cam.CopyFrom(main);
        var vcam = Object.FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam != null) cam.fieldOfView = vcam.Lens.FieldOfView;   // 플레이 중 메인 카메라 렌즈 = 시네머신 렌즈
        var data = cam.GetUniversalAdditionalCameraData();
        if (main != null) { var md = main.GetUniversalAdditionalCameraData(); data.renderPostProcessing = md.renderPostProcessing; data.volumeLayerMask = md.volumeLayerMask; data.antialiasing = md.antialiasing; }
        const int W = 593, H = 973;    // iPhone Safari 의 실제 게임 영역(주소창·하단바 제외 약 1185x1945)의 1/2
        var rt = new RenderTexture(W, H, 24);
        var prev = RenderTexture.active;
        try
        {
            cam.targetTexture = rt;
            foreach (var (tag, headHeight) in variants)
            {
                Vector3 head = npc.position + Vector3.up * headHeight;
                Vector3 fwd = npc.forward; fwd.y = 0f;
                fwd = fwd.sqrMagnitude < 1e-4f ? Vector3.forward : fwd.normalized;
                var pos = head + fwd * dist + Vector3.up * (CamRaise + extraRaise);
                cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation((head - pos).normalized, Vector3.up));
                VolumeManager.instance.Update(cam.transform, data.volumeLayerMask);
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
                File.WriteAllBytes($"Temp/npccam/{file}_{tag}.png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            Debug.Log($"[NpcCamPreview] {file} fov={cam.fieldOfView} dist={dist}");
        }
        finally { RenderTexture.active = prev; cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(go); }
    }
}
