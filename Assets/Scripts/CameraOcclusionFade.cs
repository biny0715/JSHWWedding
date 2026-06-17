// CameraOcclusionFade.cs
// 카메라와 플레이어 사이에 든 건물을 반투명(fade) 처리. 카메라는 당기지 않음(과확대 없음).
// 판정: 플레이어 실루엣(머리~발 × 좌우 폭)을 여러 점으로 샘플링하고, 카메라->각 점 선분이
//        건물 조각 렌더러 bounds 를 지나면 그 점이 "가려짐". 가려진 점 비율이 occludeThreshold
//        이상일 때만 그 건물을 fade. (가장자리만 살짝 걸치면 비율이 낮아 fade 안 함.)
//   - Bounds.IntersectRay 는 카메라가 건물 안에 있어도(원점 내부) 감지된다.
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
        [Range(0f, 1f)] public float occludeThreshold = 0.5f;
        [Tooltip("샘플할 캐릭터 키")]
        public float bodyHeight = 1.7f;
        [Tooltip("샘플할 캐릭터 폭")]
        public float bodyWidth = 0.6f;
        [Tooltip("이만큼 가까운(앞쪽) 조각은 가림으로 안 침(벽에 붙어있을 때 오탐 방지)")]
        public float margin = 0.6f;
        [Tooltip("거리 이내(카메라를 감싸거나 바로 앞) 조각 AABB 무시 — 오목 건물 안뜰/지붕 오탐 방지")]
        public float nearSkip = 0.3f;

        // 몸 세로/가로 샘플 위치 비율
        static readonly float[] HEIGHTS = { 0.12f, 0.4f, 0.68f, 0.95f };
        static readonly float[] WIDTHS = { -1f, 0f, 1f };

        Camera cam;
        Transform player;
        Vector3[] samples;

        class Bld
        {
            public Renderer[] rends;
            public Material[][] orig;
            public Material fadeMat;
            public Bounds bounds;   // 건물 전체 합산 AABB (정적, Start 1회 계산)
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
                var b = new Bld { rends = rends };
                b.orig = new Material[rends.Length][];
                var bb = rends[0].bounds;
                for (int i = 0; i < rends.Length; i++)
                {
                    b.orig[i] = rends[i].sharedMaterials;
                    bb.Encapsulate(rends[i].bounds);
                }
                b.bounds = bb;
                b.fadeMat = MakeTransparent(lit, fadeColor);
                blds.Add(b);
            }
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (player == null) player = FindPlayer();
            if (cam == null || player == null) return;

            Vector3 from = cam.transform.position;
            Vector3 basePos = player.position;

            // 카메라->플레이어 시선에 수직인 가로축(수평)
            Vector3 right = Vector3.Cross(basePos - from, Vector3.up);
            right = right.sqrMagnitude < 1e-4f ? cam.transform.right : right.normalized;

            // 몸 샘플점 생성 (머리~발 × 좌우)
            int n = 0;
            for (int h = 0; h < HEIGHTS.Length; h++)
                for (int w = 0; w < WIDTHS.Length; w++)
                    samples[n++] = basePos + Vector3.up * (bodyHeight * HEIGHTS[h]) + right * (bodyWidth * 0.5f * WIDTHS[w]);
            int total = n;

            for (int i = 0; i < blds.Count; i++)
            {
                var b = blds[i];

                // 카메라는 건물 "밖" 인데 캐릭터만 건물 bounds "안" → 밖에서 입구/앞의 캐릭터가 보이는 상황
                //   (이때만 fade 제외). 카메라가 건물 안이면 캐릭터가 그 건물에 덮인 것이므로 정상 판정 → fade.
                bool occ;
                if (!b.bounds.Contains(from) && b.bounds.Contains(player.position))
                {
                    occ = false;
                }
                else
                {
                    int occluded = 0;
                    for (int s = 0; s < total; s++)
                    {
                        Vector3 d = samples[s] - from;
                        float dist = d.magnitude;
                        var ray = new Ray(from, d / dist);
                        float limit = dist - margin;

                        var rs = b.rends;
                        for (int r = 0; r < rs.Length; r++)
                        {
                            var ren = rs[r];
                            if (ren == null) continue;
                            if (ren.bounds.IntersectRay(ray, out float hd) && hd > nearSkip && hd < limit) { occluded++; break; }
                        }
                    }
                    occ = total > 0 && (float)occluded / total >= occludeThreshold;
                }

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
