using UnityEngine;

/// <summary>
/// Named easing curves used by the lightweight tween system (see <see cref="Tweens"/>).
/// Implementations follow the standard Robert Penner easing equations.
/// </summary>
public enum Ease
{
    Linear,
    QuadIn,
    QuadOut,
    QuadInOut,
    CubicIn,
    CubicOut,
    CubicInOut,
    SineIn,
    SineOut,
    SineInOut,
    BackOut,
    BackInOut,
    ElasticOut,
    BounceOut
}

/// <summary>
/// Pure functions mapping linear progress [0..1] to eased progress [0..1].
/// </summary>
public static class Easing
{
    private const float BackOvershoot = 1.70158f;

    public static float Apply(Ease ease, float t)
    {
        t = Mathf.Clamp01(t);
        switch (ease)
        {
            case Ease.Linear: return t;
            case Ease.QuadIn: return t * t;
            case Ease.QuadOut: return -t * (t - 2f);
            case Ease.QuadInOut: return t < 0.5f ? 2f * t * t : -2f * t * t + 4f * t - 1f;
            case Ease.CubicIn: return t * t * t;
            case Ease.CubicOut:
                { float f = t - 1f; return f * f * f + 1f; }
            case Ease.CubicInOut:
                return t < 0.5f ? 4f * t * t * t : 0.5f * (2f * t - 2f) * (2f * t - 2f) * (2f * t - 2f) + 1f;
            case Ease.SineIn: return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
            case Ease.SineOut: return Mathf.Sin(t * Mathf.PI * 0.5f);
            case Ease.SineInOut: return -0.5f * (Mathf.Cos(Mathf.PI * t) - 1f);
            case Ease.BackOut:
                {
                    float s = BackOvershoot + 1f;
                    float f = t - 1f;
                    return f * f * ((s + 1f) * f + s) + 1f;
                }
            case Ease.BackInOut:
                {
                    float s = BackOvershoot * 1.525f;
                    if (t < 0.5f)
                    {
                        float f = 2f * t;
                        return 0.5f * f * f * ((s + 1f) * f - s);
                    }
                    else
                    {
                        float f = 2f * t - 2f;
                        return 0.5f * (f * f * ((s + 1f) * f + s) + 2f);
                    }
                }
            case Ease.ElasticOut:
                {
                    if (t <= 0f || t >= 1f) return t;
                    float p = 0.3f;
                    float s = p / 4f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - s) * (2f * Mathf.PI) / p) + 1f;
                }
            case Ease.BounceOut:
                {
                    if (t < 1f / 2.75f) return 7.5625f * t * t;
                    if (t < 2f / 2.75f)
                    {
                        float f = t - 1.5f / 2.75f;
                        return 7.5625f * f * f + 0.75f;
                    }
                    if (t < 2.5f / 2.75f)
                    {
                        float f = t - 2.25f / 2.75f;
                        return 7.5625f * f * f + 0.9375f;
                    }
                    float g = t - 2.625f / 2.75f;
                    return 7.5625f * g * g + 0.984375f;
                }
            default: return t;
        }
    }

    /// <summary>Randomised micro variation used to make repeated tweens feel organic.</summary>
    public static float Jitter(float amount)
    {
        return Random.Range(-amount, amount);
    }
}
