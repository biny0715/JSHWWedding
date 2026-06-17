// CameraOcclusionFade.cs
// 카메라와 플레이어 사이에 든 건물을 반투명(fade) 처리. 카메라는 당기지 않음(과확대 없음).
// 판정: 플레이어 실루엣(머리~발 × 좌우)을 여러 점으로 샘플링하고, 카메라->각 점을 "실제 건물 메시"
//        (BuildingMeshOccluderBaker 가 붙인 MeshCollider)에 Physics.Raycast. 가려진 점 비율이
//        occludeThreshold 이상인 건물만 fade. AABB 근사가 아니라 실제 메시라 오목/겹친 건물도 정확.
//   - 콜라이더는 Default 레이어. 같은 레이어의 비건물(나무/플레이어)에 맞아도 건물로 매핑 안 되면 무시.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Photon.Pun.Demo.PunBasics;

namespace JSHWWedding
{
    public class CameraOcclusionFade : MonoBehaviour
    {
        [Tooltip("건물 루트(비우면 WeddingEnviroments/House 자동). 자식 = 각 건물")]
        public Transform houseRoot;
        [Range(0f, 1f)] public float fadedAlpha = 0.1f;    // 가릴 때 알파(낮을수록 더 투명)
        public float fadeSpeed = 8f;
        public Color fadeColor = new Color(0.86f, 0.86f, 0.9f);

        [Header("가림 판정")]
        [Tooltip("이 비율 이상의 몸 샘플점이 가려져야 fade (낮을수록 민감)")]
        [Range(0f, 1f)] public float occludeThreshold = 0.7f;
        [Tooltip("샘플할 캐릭터 키")]
        public float bodyHeight = 1.7f;
        [Tooltip("샘플할 캐릭터 폭")]
        public float bodyWidth = 0.6f;
        [Tooltip("이만큼 가까운(앞쪽) 히트는 무시(카메라 바로 앞 자기충돌 방지)")]
        public float nearSkip = 0.1f;
        [Tooltip("플레이어에 이만큼 못 미치는 지점까지만 검사(발밑 바닥 오탐 방지)")]
        public float margin = 0.6f;

        // 몸 세로/가로 샘플 위치 비율
        static readonly float[] HEIGHTS = { 0.12f, 0.4f, 0.68f, 0.95f };
        static readonly float[] WIDTHS = { -1f, 0f, 1f };

        Camera cam;
        Transform player;
        Vector3[] samples;
        int occluderMask;
        readonly RaycastHit[] hitBuf = new RaycastHit[32];
        int[] occCount;
        bool[] sampleHit;

        class Bld
        {
            public Transform root;
            public Renderer[] rends;
            public Material[][] orig;
            public Material fadeMat;
            public float a = 1f;
            public bool faded;
        }
        readonly List<Bld> blds = new List<Bld>();

        void Start()
        {
            if (houseRoot == null)
            {
                var go = GameObject.Find("WeddingEnviroments/House");
                if (go) houseRoot = go.transform;
            }
            if (houseRoot == null) { enabled = false; return; }

            samples = new Vector3[HEIGHTS.Length * WIDTHS.Length];

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (Transform child in houseRoot)
            {
                var rends = child.GetComponentsInChildren<Renderer>(true);
                if (rends.Length == 0) continue;
                var b = new Bld { root = child, rends = rends };
                b.orig = new Material[rends.Length][];
                for (int i = 0; i < rends.Length; i++) b.orig[i] = rends[i].sharedMaterials;
                b.fadeMat = MakeTransparent(lit, fadeColor);
                blds.Add(b);
            }

            occCount = new int[blds.Count];
            sampleHit = new bool[blds.Count];
            // 건물 조각이 올라가 있는 레이어로 마스크 구성(보통 Default)
            occluderMask = blds.Count > 0 ? (1 << blds[0].rends[0].gameObject.layer) : ~0;
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (player == null) player = FindPlayer();
            if (cam == null || player == null || blds.Count == 0) return;

            Vector3 from = cam.transform.position;
            Vector3 basePos = player.position;

            Vector3 right = Vector3.Cross(basePos - from, Vector3.up);
            right = right.sqrMagnitude < 1e-4f ? cam.transform.right : right.normalized;

            int n = 0;
            for (int h = 0; h < HEIGHTS.Length; h++)
                for (int w = 0; w < WIDTHS.Length; w++)
                    samples[n++] = basePos + Vector3.up * (bodyHeight * HEIGHTS[h]) + right * (bodyWidth * 0.5f * WIDTHS[w]);
            int total = n;

            for (int i = 0; i < occCount.Length; i++) occCount[i] = 0;

            for (int s = 0; s < total; s++)
            {
                Vector3 d = samples[s] - from;
                float dist = d.magnitude;
                float maxd = dist - margin;
                if (maxd <= nearSkip) continue;

                int hits = Physics.RaycastNonAlloc(new Ray(from, d / dist), hitBuf, maxd, occluderMask, QueryTriggerInteraction.Collide);
                if (hits == 0) continue;

                for (int i = 0; i < sampleHit.Length; i++) sampleHit[i] = false;
                for (int h = 0; h < hits; h++)
                {
                    if (hitBuf[h].distance <= nearSkip) continue;
                    int bi = ColliderToBuilding(hitBuf[h].collider);
                    if (bi >= 0) sampleHit[bi] = true;
                }
                for (int i = 0; i < sampleHit.Length; i++) if (sampleHit[i]) occCount[i]++;
            }

            for (int i = 0; i < blds.Count; i++)
            {
                var b = blds[i];
                bool occ = total > 0 && (float)occCount[i] / total >= occludeThreshold;
                float target = occ ? fadedAlpha : 1f;
                b.a = Mathf.MoveTowards(b.a, target, fadeSpeed * Time.deltaTime);

                if (b.a < 0.999f)
                {
                    if (!b.faded) SwapMaterials(b, true);
                    var c = b.fadeMat.GetColor("_BaseColor"); c.a = b.a; b.fadeMat.SetColor("_BaseColor", c);
                }
                else if (b.faded)
                {
                    SwapMaterials(b, false);
                }
            }
        }

        int ColliderToBuilding(Collider col)
        {
            Transform t = col.transform;
            while (t != null)
            {
                for (int i = 0; i < blds.Count; i++) if (blds[i].root == t) return i;
                t = t.parent;
            }
            return -1;
        }

        void SwapMaterials(Bld b, bool toFade)
        {
            b.faded = toFade;
            for (int i = 0; i < b.rends.Length; i++)
            {
                if (b.rends[i] == null) continue;
                if (toFade)
                {
                    var arr = new Material[b.orig[i].Length];
                    for (int k = 0; k < arr.Length; k++) arr[k] = b.fadeMat;
                    b.rends[i].sharedMaterials = arr;
                }
                else b.rends[i].sharedMaterials = b.orig[i];
            }
        }

        Transform FindPlayer()
        {
            var ms = FindObjectsByType<PlayerClickToMove>(FindObjectsSortMode.None);
            foreach (var m in ms) if (m.photonView != null && m.photonView.IsMine) return m.transform;
            return null;
        }

        static Material MakeTransparent(Shader lit, Color col)
        {
            var m = new Material(lit);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetOverrideTag("RenderType", "Transparent");
            m.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetColor("_BaseColor", new Color(col.r, col.g, col.b, 1f));
            return m;
        }
    }
}
