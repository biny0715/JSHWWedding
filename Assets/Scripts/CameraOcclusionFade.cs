// CameraOcclusionFade.cs
// 카메라와 플레이어 사이에 든 건물을 반투명(fade) 처리. 카메라는 당기지 않음(과확대 없음).
// 판정: 큰 BoxCollider 대신 건물 "조각별 렌더러 bounds" 에 카메라->플레이어 선분이 통과하는지 검사.
//   - 조각 AABB 합집합이라 건물 실제 모양에 가깝다(처마/빈 공간 오탐 감소).
//   - Bounds.IntersectRay 는 카메라가 건물 안에 있어도(원점 내부) 감지된다(Collider.Raycast 의 한계 해결).
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
        [Range(0f, 1f)] public float fadedAlpha = 0.12f;   // 가릴 때 알파(낮을수록 더 투명)
        public float fadeSpeed = 8f;
        public Color fadeColor = new Color(0.86f, 0.86f, 0.9f);
        [Tooltip("플레이어 머리 높이(가림 판정 타겟 오프셋)")]
        public float targetHeight = 1.2f;
        [Tooltip("플레이어에 이만큼 가까운(앞쪽) 조각은 가림으로 안 침(플레이어가 벽에 붙어있을 때 오탐 방지)")]
        public float margin = 0.6f;

        Camera cam;
        Transform player;

        class Bld
        {
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

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (Transform child in houseRoot)
            {
                var rends = child.GetComponentsInChildren<Renderer>(true);
                if (rends.Length == 0) continue;
                var b = new Bld { rends = rends };
                b.orig = new Material[rends.Length][];
                for (int i = 0; i < rends.Length; i++) b.orig[i] = rends[i].sharedMaterials;
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
            Vector3 to = player.position + Vector3.up * targetHeight;
            Vector3 d = to - from;
            float dist = d.magnitude;
            var ray = new Ray(from, d.normalized);
            float limit = dist - margin;

            for (int i = 0; i < blds.Count; i++)
            {
                var b = blds[i];

                // 카메라->플레이어 선분이 이 건물 조각 중 하나라도 통과하면 가림
                bool occ = false;
                var rs = b.rends;
                for (int r = 0; r < rs.Length; r++)
                {
                    var ren = rs[r];
                    if (ren == null) continue;
                    if (ren.bounds.IntersectRay(ray, out float hd) && hd < limit) { occ = true; break; }
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
