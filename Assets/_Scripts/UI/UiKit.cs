using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small factories for creating UI elements (rects, images, text) from code.
/// Used by the runtime UI layer so the whole FX/announcement system works
/// without needing scene references or prefabs.
/// </summary>
public static class UiKit
{
    private static TMP_FontAsset _font;
    private static bool _fontLookedUp;

    /// <summary>The project default TMP font (Liberation Sans SDF).</summary>
    public static TMP_FontAsset DefaultFont
    {
        get
        {
            if (_font == null && !_fontLookedUp)
            {
                _fontLookedUp = true;
                if (TMP_Settings.defaultFontAsset != null)
                {
                    _font = TMP_Settings.defaultFontAsset;
                }
                if (_font == null)
                {
                    _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                }
            }
            return _font;
        }
    }

    /// <summary>Creates a plain RectTransform under <paramref name="parent"/>.</summary>
    public static RectTransform AddRect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.transform as RectTransform;
        rt.SetParent(parent, false);
        return rt;
    }

    /// <summary>Anchors a rect so it fills its parent with the given margins.</summary>
    public static void SetStretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    /// <summary>Centres a rect on <paramref name="parentCenter"/> with a fixed size.</summary>
    public static void CenterIn(RectTransform rt, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }

    public static Image AddImage(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
    {
        RectTransform rt = AddRect(parent, name);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = (sprite != null && sprite.border.sqrMagnitude > 0f) ? Image.Type.Sliced : Image.Type.Simple;
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    /// <summary>Adds a fully stretched image filling its parent.</summary>
    public static Image AddStretchImage(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
    {
        Image img = AddImage(parent, name, sprite, color, raycast);
        SetStretch(img.rectTransform);
        return img;
    }

    public static TextMeshProUGUI AddText(Transform parent, string name, string content, float fontSize,
        Color color, FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.Center,
        bool raycast = false)
    {
        RectTransform rt = AddRect(parent, name);
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = DefaultFont;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.text = content;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = raycast;
        return tmp;
    }

    public static void SetAnchored(RectTransform rt, float sizeW, float sizeH)
    {
        rt.sizeDelta = new Vector2(sizeW, sizeH);
    }

    /// <summary>Unity named-color -> Color quick conversion (safe fallback to white).</summary>
    public static Color ParseColor(string hexOrName, Color fallback)
    {
        Color c;
        if (ColorUtility.TryParseHtmlString(hexOrName, out c)) return c;
        return fallback;
    }
}
