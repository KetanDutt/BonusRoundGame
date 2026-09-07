using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small effect helpers used by zones (grid cells): pop, shake, expanding
/// rings and floating text anchored to a specific cell.
/// </summary>
public static class ZoneFx
{
    private static readonly Dictionary<RectTransform, TweenHandle> _shakes =
        new Dictionary<RectTransform, TweenHandle>();

    /// <summary>Quick scale punch — used whenever a cell changes state.</summary>
    public static void Pop(RectTransform rt, float amount = 1.15f)
    {
        if (rt == null) return;
        Tweens.PunchScale(rt, amount, 0.26f);
    }

    /// <summary>Short horizontal shake (steal impact, shield break...).</summary>
    public static void Shake(RectTransform rt, float magnitude = 6f, float duration = 0.22f)
    {
        if (rt == null) return;
        TweenHandle previous;
        if (_shakes.TryGetValue(rt, out previous))
        {
            if (previous != null) previous.Cancel();
            _shakes.Remove(rt);
        }

        Vector2 home = rt.anchoredPosition;
        int waves = Mathf.Max(2, Mathf.RoundToInt(duration * 24f));
        float step = duration / waves;
        float mag = magnitude;
        TweenHandle handle = null;
        handle = Tweens.Float(0f, waves, duration * 0.999f, delegate (float t)
        {
            if (rt == null) return;
            int wave = Mathf.FloorToInt(t);
            float local = t - wave;
            float eased = Mathf.Sin(local * Mathf.PI);
            float amp = mag * Mathf.Lerp(1f, 0.15f, t / waves);
            rt.anchoredPosition = home + new Vector2((wave % 2 == 0 ? 1f : -1f) * eased * amp, 0f);
        }, Ease.Linear, 0f, delegate
        {
            if (rt != null) rt.anchoredPosition = home;
            if (handle != null && _shakes.ContainsKey(rt))
            {
                _shakes.Remove(rt);
            }
        });
        _shakes[rt] = handle;
    }

    /// <summary>Expanding ring emitted from a cell (shield / takeover).</summary>
    public static void EmitRing(RectTransform cell, Color color, float maxScale = 1.6f)
    {
        if (cell == null) return;
        Image ring = UiKit.AddStretchImage(cell, "FxRing", SpriteFactory.Ring(), color);
        ring.raycastTarget = false;
        RectTransform rt = ring.rectTransform;
        rt.localScale = Vector3.one * 0.45f;

        Tweens.Scale(rt, Vector3.one * maxScale, 0.34f, Ease.QuadOut);
        Tweens.Float(0.95f, 0f, 0.4f, delegate (float a)
        {
            if (ring != null)
            {
                Color c = ring.color;
                c.a = a;
                ring.color = c;
            }
        }, Ease.QuadOut, 0f, delegate
        {
            if (ring != null) Object.Destroy(ring.gameObject);
        });
    }

    /// <summary>Shows rising text centred on a specific cell.</summary>
    public static void FloatTextOnCell(GridElement cell, string message, Color color, float size = 22f)
    {
        if (cell == null) return;
        RectTransform rt = cell.transform as RectTransform;
        if (rt == null) return;
        Vector2 point;
        if (App.TryGetCanvasCenter(rt, out point))
        {
            RuntimeUi ui = RuntimeUi.EnsureCreated();
            if (ui != null) ui.FloatingText(message, point, color, size);
        }
    }
}
