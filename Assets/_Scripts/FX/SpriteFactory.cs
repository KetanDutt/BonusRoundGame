using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates crisp white UI sprites at runtime (circles, rings, rounded
/// rectangles, diamonds, arrows, triangles...). Sprites are cached and tinted
/// via Image.color, so the game needs no imported sprite assets and cannot
/// lose art files. All textures are generated once per shape and reused.
/// </summary>
public static class SpriteFactory
{
    private const int TexSize = 128;

    private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

    /// <summary>Solid rounded-rectangle sprite with 9-slice border (use Image.type = Sliced).</summary>
    public static Sprite RoundedRect()
    {
        return GetOrCreate("roundrect", delegate (Color32[] px)
        {
            int b = 16; // corner radius in texels
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    px[y * TexSize + x] = White(RoundedRectCoverage(x, y, b));
                }
            }
        }, new Vector4(b, b, b, b));
    }

    /// <summary>Plain filled square (confetti and generic chips).</summary>
    public static Sprite Rectangle()
    {
        return GetOrCreate("rect", delegate (Color32[] px)
        {
            for (int i = 0; i < px.Length; i++) px[i] = White(1f);
        }, Vector4.zero);
    }

    /// <summary>Plain circle.</summary>
    public static Sprite Circle()
    {
        return GetOrCreate("circle", delegate (Color32[] px)
        {
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    px[y * TexSize + x] = White(DiscCoverage(x, y, 0.46f));
                }
            }
        }, Vector4.zero);
    }

    /// <summary>Thin ring (outline circle) — used for shield FX.</summary>
    public static Sprite Ring()
    {
        return GetOrCreate("ring", delegate (Color32[] px)
        {
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    px[y * TexSize + x] = White(RingCoverage(x, y, 0.42f, 0.13f));
                }
            }
        }, Vector4.zero);
    }

    /// <summary>Diamond / gem shape (BOOST icon, confetti).</summary>
    public static Sprite Diamond()
    {
        return GetOrCreate("diamond", delegate (Color32[] px)
        {
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    float dx = (x + 0.5f) / TexSize - 0.5f;
                    float dy = (y + 0.5f) / TexSize - 0.5f;
                    float d = Mathf.Abs(dx) + Mathf.Abs(dy);
                    float c = 1f - Mathf.SmoothStep(0.47f - 0.02f, 0.47f, d);
                    px[y * TexSize + x] = White(c);
                }
            }
        }, Vector4.zero);
    }

    /// <summary>Right-pointing arrow (STEAL icon).</summary>
    public static Sprite ArrowRight()
    {
        return GetOrCreate("arrow", delegate (Color32[] px)
        {
            Vector2[] poly =
            {
                new Vector2(0.08f, 0.22f), new Vector2(0.52f, 0.22f), new Vector2(0.52f, 0.06f),
                new Vector2(0.93f, 0.5f), new Vector2(0.52f, 0.94f), new Vector2(0.52f, 0.78f),
                new Vector2(0.08f, 0.78f)
            };
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    float u = (x + 0.5f) / TexSize;
                    float v = 1f - (y + 0.5f) / TexSize;
                    float c = PointInPoly(new Vector2(u, v), poly) ? 1f : 0f;
                    px[y * TexSize + x] = White(Aa(u, v, poly, c));
                }
            }
        }, Vector4.zero);
    }

    /// <summary>Up-pointing triangle (generic 'effect up' icon).</summary>
    public static Sprite Triangle()
    {
        return GetOrCreate("triangle", delegate (Color32[] px)
        {
            Vector2[] poly = { new Vector2(0.5f, 0.05f), new Vector2(0.93f, 0.9f), new Vector2(0.07f, 0.9f) };
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    float u = (x + 0.5f) / TexSize;
                    float v = 1f - (y + 0.5f) / TexSize;
                    float c = PointInPoly(new Vector2(u, v), poly) ? 1f : 0f;
                    px[y * TexSize + x] = White(Aa(u, v, poly, c));
                }
            }
        }, Vector4.zero);
    }

    // ------------------------------------------------------------------ helpers

    private static Sprite GetOrCreate(string key, System.Action<Color32[]> painter, Vector4 border)
    {
        Sprite cached;
        if (_cache.TryGetValue(key, out cached) && cached != null) return cached;

        Texture2D tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
        tex.name = "gen_" + key;
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[TexSize * TexSize];
        painter(px);
        tex.SetPixels32(px);
        tex.Apply(false, true);

        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, TexSize, TexSize), new Vector2(0.5f, 0.5f), 100f, 0u,
            SpriteMeshType.FullRect, border);
        sprite.name = "spr_" + key;
        _cache[key] = sprite;
        return sprite;
    }

    private static Color32 White(float coverage)
    {
        byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(coverage * 255f), 0, 255);
        return new Color32(255, 255, 255, a);
    }

    private static float RoundedRectCoverage(int x, int y, int radius)
    {
        // A pixel is inside the rounded rectangle unless it lies in a corner
        // region where BOTH axis distances to the nearest edges are < radius.
        float xc = Mathf.Min(x + 0.5f, TexSize - (x + 0.5f));
        float yc = Mathf.Min(y + 0.5f, TexSize - (y + 0.5f));
        if (xc >= radius || yc >= radius) return 1f;
        float d = Mathf.Sqrt((radius - xc) * (radius - xc) + (radius - yc) * (radius - yc));
        return 1f - Mathf.SmoothStep(radius - 1f, radius + 1f, d);
    }

    private static float DiscCoverage(int x, int y, float radius)
    {
        float u = (x + 0.5f) / TexSize - 0.5f;
        float v = (y + 0.5f) / TexSize - 0.5f;
        float d = Mathf.Sqrt(u * u + v * v);
        return 1f - Mathf.SmoothStep(radius - 0.015f, radius, d);
    }

    private static float RingCoverage(int x, int y, float center, float halfWidth)
    {
        float u = (x + 0.5f) / TexSize - 0.5f;
        float v = (y + 0.5f) / TexSize - 0.5f;
        float d = Mathf.Sqrt(u * u + v * v);
        float band = Mathf.Abs(d - center);
        float c = 1f - Mathf.SmoothStep(halfWidth - 0.02f, halfWidth, band);
        return c * (1f - Mathf.SmoothStep(0.46f, 0.5f, d)); // clip stray outer AA
    }

    private static bool PointInPoly(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        int j = poly.Length - 1;
        for (int i = 0; i < poly.Length; i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }

    /// <summary>1px antialias around polygon edges via signed distance approximations.</summary>
    private static float Aa(float u, float v, Vector2[] poly, float inside)
    {
        if (inside > 0.5f) return 1f;
        // sample a few neighbours — cheap & good enough at icon size
        float e = 1f / TexSize;
        Vector2[] probes = { new Vector2(u + e, v), new Vector2(u - e, v), new Vector2(u, v + e), new Vector2(u, v - e) };
        float hit = 0f;
        for (int i = 0; i < probes.Length; i++)
        {
            if (PointInPoly(probes[i], poly)) hit += 1f;
        }
        return hit / probes.Length;
    }
}
