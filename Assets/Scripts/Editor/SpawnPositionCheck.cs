using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun.Demo.PunBasics;

// 스폰 위치 검증(에디터, 네트워크 없이): 씬의 GameManager 설정으로 GameManager.ComputeSpawnPosition 결과가
// 지정 SpawnPosition 과 같은 고정 지점인지, NavMesh 위인지, PlayerClickToMove 의 Start Warp(1m) 보정량이 ~0 인지 확인.
// (계산 입력에 ActorNumber 가 없으므로 접속 순서/재입장과 무관 — 시그니처로 보장)
public static class SpawnPositionCheck
{
    [MenuItem("Tools/JSHW/Check Spawn Position")]
    public static void Run()
    {
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null) { Debug.LogError("[SpawnCheck] 씬에 GameManager 없음"); return; }
        float snap = new SerializedObject(gm).FindProperty("navMeshSnapDistance").floatValue;
        Vector3 basePos = gm.SpawnPosition;

        var r = GameManager.ComputeSpawnPosition(basePos, snap);
        bool onNav = NavMesh.SamplePosition(r.position, out NavMeshHit h, 0.05f, NavMesh.AllAreas);
        // PlayerClickToMove.Start 와 같은 보정(1m) 을 스폰 결과에 적용했을 때의 이동량
        float warp = NavMesh.SamplePosition(r.position, out NavMeshHit w, 1f, NavMesh.AllAreas) ? (w.position - r.position).magnitude : -1f;
        Debug.Log($"[SpawnCheck] base={basePos:F3} spawn={r.position:F3} snapDelta={(r.position - basePos).magnitude:F3}m " +
                  $"fallback={r.fallback} onNavMesh={onNav} startWarpDelta={warp:F3}m");
    }
}
