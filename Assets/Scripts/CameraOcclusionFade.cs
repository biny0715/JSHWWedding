// CameraOcclusionFade.cs
// 카메라와 플레이어 사이에 든 건물을 반투명(fade) 처리한다.
// 카메라를 당기지 않고(=과확대 없음), 가리는 건물만 비쳐서 캐릭터가 보이게 한다. (Deoccluder 대체)
// WeddingEnviroments/House 의 각 건물(Center/Left/Right, BoxCollider 보유)을 대상으로,
// 카메라→플레이어 선분이 건물 콜라이더를 통과하면 그 건물만 알파를 낮춘다.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Photon.Pun.Demo.PunBasics;

namespace JSHWWedding
{
    public class CameraOcclusionFade : MonoBehaviour
    {
        [Tooltip("건물 루트(비우면 WeddingEnviroments/House 자동). 각 자식 건물에 Collider 필요")]
        public Transform houseRoot;
        [Range(0f, 1f)] public float fadedAlpha = 0.28f;   // 가릴 때 알파(낮을수록 더 투명)
        public float fadeSpeed = 6f;
        public Color fadeColor = new Color(0.86f, 0.86f, 0.9f);
        [Tooltip("플레이어 머리 높이(가림 판정 타겟 오프셋)")]
        public float targetHeight = 1.2f;

        Camera cam;
        Transform player;

        class Bld
        {
            public Collider col;
            public Renderer[] rends;
            public Material[][] orig;   // 렌더러별 원본 sharedMaterials
            public Material fadeMat;    // 이 건물 전용 투명 머티리얼
            public float a = 1f;        // 현재 알파(1=불투명)
            public bool faded;          // 현재 투명 머티리얼 적용 중인지
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
                var col = child.GetComponent<Collider>();
                if (col == null) continue;
                var b = new Bld { col = col };
                b.rends = child.GetComponentsInChildren<Renderer>(true);
                b.orig = new Material[b.rends.Length][];
                for (int i = 0; i < b.rends.Length; i++) b.orig[i] = b.rends[i].sharedMaterials;
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

            for (int i = 0; i < blds.Count; i++)
            {
                var b = blds[i];
                if (b.col == null) continue;

                bool occ = b.col.Raycast(ray, out _, dist);          // 카메라→플레이어 선분이 이 건물을 통과?
                float target = occ ? fadedAlpha : 1f;
                b.a = Mathf.MoveTowards(b.a, target, fadeSpeed * Time.deltaTime);

                if (b.a < 0.999f)
                {
                    if (!b.faded) SwapMaterials(b, true);            // 1회만 투명 머티리얼로 교체
                    var c = b.fadeMat.GetColor("_BaseColor"); c.a = b.a; b.fadeMat.SetColor("_BaseColor", c);
                }
                else if (b.faded)
                {
                    SwapMaterials(b, false);                          // 원래 머티리얼 복구
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
            m.SetFloat("_Surface", 1f);                 // 0 Opaque, 1 Transparent
            m.SetFloat("_Blend", 0f);                   // Alpha
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
