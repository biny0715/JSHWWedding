// PetalGrassBaker.cs  (Editor-only)
// 잔디 터레인 텍스처(SWS_DemoGrass.png)에 벚꽃 꽃잎을 절차적으로 합성해서
// "바닥에 떨어진 꽃잎" 효과를 낸다. 타일링 텍스처라 가장자리를 wrap(토로이드) 해서 이음새 없게.
// 원본은 보존하고 새 PNG(SWS_DemoGrass_Petals.png)를 만든 뒤 SWS_Grass 터레인 레이어를 그쪽으로 교체.

using System.IO;
using UnityEngine;
using UnityEditor;

public static class PetalGrassBaker
{
    const string Dir   = "Assets/Stylized Water 3/_Demo/DemoAssets/Terrain/";
    const string Src   = Dir + "SWS_DemoGrass.png";
    const string Dst   = Dir + "SWS_DemoGrass_Petals.png";
    const string Layer = Dir + "Layers/SWS_Grass.terrainlayer";

    // 튜닝 값 (미리보기 보고 조정).
    const int   Seed        = 7;
    const float PetalsPer1k = 1.1f;   // 1000px²당 꽃잎 수 (밀도)
    const float SizeFrac    = 1f/30f; // 꽃잎 반길이 = 텍스처폭 * 이 값

    [MenuItem("Tools/Hanok/Bake Cherry Petals into Grass")]
    public static void Bake()
    {
        string root = Directory.GetCurrentDirectory();
        string srcAbs = Path.Combine(root, Src);
        if (!File.Exists(srcAbs)) { Debug.LogError($"[Petals] not found: {srcAbs}"); return; }

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(srcAbs));   // PNG 디코드 (import readable 무관)
        int W = tex.width, H = tex.height;
        var px = tex.GetPixels32();

        var rnd = new System.Random(Seed);
        int count = Mathf.Max(30, Mathf.RoundToInt(W * H / 1000f * PetalsPer1k));

        // 벚꽃 팔레트 (연분홍~분홍)
        Color[] palette = {
            new Color(1.00f, 0.86f, 0.90f),
            new Color(1.00f, 0.79f, 0.85f),
            new Color(1.00f, 0.72f, 0.78f),
            new Color(0.98f, 0.67f, 0.77f),
        };

        for (int i = 0; i < count; i++)
        {
            float cx = (float)rnd.NextDouble() * W;
            float cy = (float)rnd.NextDouble() * H;
            float a  = W * SizeFrac * (0.7f + (float)rnd.NextDouble() * 0.9f); // 반길이
            float b  = a * (0.42f + (float)rnd.NextDouble() * 0.18f);          // 반너비
            float th = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float ct = Mathf.Cos(th), st = Mathf.Sin(th);
            Color pc = palette[rnd.Next(palette.Length)];
            float baseA = 0.78f + (float)rnd.NextDouble() * 0.18f;

            int rad = Mathf.CeilToInt(a) + 1;
            for (int dy = -rad; dy <= rad; dy++)
            for (int dx = -rad; dx <= rad; dx++)
            {
                float lx =  dx * ct + dy * st;
                float ly = -dx * st + dy * ct;
                float e = (lx * lx) / (a * a) + (ly * ly) / (b * b);
                if (e > 1f) continue;

                // 끝쪽(넓은 쪽)에 살짝 V 노치 → 꽃잎 느낌
                if (lx > a * 0.5f && Mathf.Abs(ly) < b * 0.28f)
                {
                    float nx = (lx - a * 0.5f) / (a * 0.5f);
                    if (nx > Mathf.Abs(ly) / (b * 0.28f)) continue;
                }

                float cov = Mathf.Clamp01((1f - e) * 3.2f); // 부드러운 가장자리
                float al = baseA * cov;
                if (al <= 0f) continue;

                int X = (int)Mathf.Repeat(cx + dx, W);
                int Y = (int)Mathf.Repeat(cy + dy, H);
                int idx = Y * W + X;
                Color32 g = px[idx];
                px[idx] = new Color32(
                    (byte)Mathf.Clamp(g.r + (pc.r * 255f - g.r) * al, 0, 255),
                    (byte)Mathf.Clamp(g.g + (pc.g * 255f - g.g) * al, 0, 255),
                    (byte)Mathf.Clamp(g.b + (pc.b * 255f - g.b) * al, 0, 255),
                    255);
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(root, Dst), tex.EncodeToPNG());
        AssetDatabase.ImportAsset(Dst, ImportAssetOptions.ForceUpdate);

        var ti = AssetImporter.GetAtPath(Dst) as TextureImporter;
        if (ti != null) { ti.wrapMode = TextureWrapMode.Repeat; ti.SaveAndReimport(); }

        var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(Layer);
        var newTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Dst);
        if (layer != null && newTex != null)
        {
            layer.diffuseTexture = newTex;
            EditorUtility.SetDirty(layer);
            AssetDatabase.SaveAssets();
        }
        AssetDatabase.Refresh();
        Debug.Log($"[Petals] baked {count} petals into {W}x{H} -> {Dst}; SWS_Grass layer repointed (original kept).");
    }
}
