using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cancellation / completion handle returned by every tween.
/// </summary>
public sealed class TweenHandle
{
    private Action<TweenHandle> _cancel;
    private bool _cancelled;

    internal TweenHandle(Action<TweenHandle> cancel)
    {
        _cancel = cancel;
    }

    /// <summary>True once the handle has been cancelled (or its tween was removed).</summary>
    public bool Cancelled
    {
        get { return _cancelled; }
    }

    /// <summary>Immediately stop the tween without invoking its completion callback.</summary>
    public void Cancel()
    {
        if (_cancelled) return;
        _cancelled = true;
        if (_cancel != null)
        {
            _cancel(this);
            _cancel = null;
        }
    }
}

/// <summary>
/// Wait helper that respects <see cref="Tweens.TimeScale"/> so "turbo mode"
/// speeds up both waits and animations consistently.
/// </summary>
public sealed class WaitFor : CustomYieldInstruction
{
    private readonly float _duration;
    private float _elapsed;

    private WaitFor(float seconds)
    {
        _duration = seconds;
    }

    public override bool keepWaiting
    {
        get
        {
            _elapsed += Time.deltaTime * Tweens.TimeScale;
            return _elapsed < _duration;
        }
    }

    /// <summary>Waits for <paramref name="seconds"/> of scaled game time.</summary>
    public static WaitFor Seconds(float seconds)
    {
        return new WaitFor(seconds);
    }
}

/// <summary>
/// Minimal, dependency-free tween engine.
///
/// Supports float / Vector2 / Vector3 / Color value tweens with easing,
/// delays, chained callbacks and cancellation. Everything is updated by a
/// single hidden MonoBehaviour so no Update() methods are needed in gameplay
/// code. Durations honour <see cref="TimeScale"/> (used by turbo mode).
///
/// This deliberately does NOT depend on third-party tween libraries so the
/// project stays dependency-light and offline-safe.
/// </summary>
public static class Tweens
{
    /// <summary>Global time multiplier applied to all tweens and WaitFor waits.</summary>
    public static float TimeScale = 1f;

    private static readonly List<TweenBase> _active = new List<TweenBase>(64);
    private static TweenRunner _runner;

    /// <summary>Create (once) the hidden GameObject that pumps the tween loop.</summary>
    public static void Init()
    {
        if (_runner != null) return;
        GameObject go = new GameObject("[TweenRunner]");
        UnityEngine.Object.DontDestroyOnLoad(go);
        _runner = go.AddComponent<TweenRunner>();
    }

    /// <summary>Removes every running tween (used on hard resets).</summary>
    public static void KillAll()
    {
        for (int i = 0; i < _active.Count; i++)
        {
            _active[i].Kill();
        }
        _active.Clear();
    }

    internal static void Update(float dt)
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i].Update(dt))
            {
                _active.RemoveAt(i);
            }
        }
    }

    private static TweenHandle Start(TweenBase tween)
    {
        Init();
        _active.Add(tween);
        return new TweenHandle(delegate (TweenHandle h) { tween.Kill(); });
    }

    // ------------------------------------------------------------------ API

    public static TweenHandle Float(float from, float to, float duration, Action<float> onUpdate,
        Ease ease = Ease.Linear, float delay = 0f, Action onComplete = null)
    {
        return Start(new TweenFloat(from, to, duration, delay, ease, onUpdate, onComplete));
    }

    public static TweenHandle Color(Color from, Color to, float duration, Action<Color> onUpdate,
        Ease ease = Ease.Linear, float delay = 0f, Action onComplete = null)
    {
        return Start(new TweenColor(from, to, duration, delay, ease, onUpdate, onComplete));
    }

    public static TweenHandle Vector3(Vector3 from, Vector3 to, float duration, Action<Vector3> onUpdate,
        Ease ease = Ease.Linear, float delay = 0f, Action onComplete = null)
    {
        return Start(new TweenVec3(from, to, duration, delay, ease, onUpdate, onComplete));
    }

    public static TweenHandle Vector2(Vector2 from, Vector2 to, float duration, Action<Vector2> onUpdate,
        Ease ease = Ease.Linear, float delay = 0f, Action onComplete = null)
    {
        return Start(new TweenVec2(from, to, duration, delay, ease, onUpdate, onComplete));
    }

    /// <summary>Runs <paramref name="action"/> after <paramref name="delay"/> seconds.</summary>
    public static TweenHandle Delay(float delay, Action action)
    {
        return Start(new TweenFloat(0f, 1f, delay, 0f, Ease.Linear,
            delegate (float v) { }, delegate { if (action != null) action(); }));
    }

    // ------------------------------------------------------- RectTransform helpers

    /// <summary>Tween a RectTransform's anchoredPosition.</summary>
    public static TweenHandle AnchoredPosition(RectTransform rt, Vector2 to, float duration,
        Ease ease = Ease.QuadOut, float delay = 0f, Action onComplete = null)
    {
        if (rt == null) return null;
        Vector2 from = rt.anchoredPosition;
        return Start(new TweenVec2(from, to, duration, delay, ease,
            delegate (Vector2 v) { if (rt != null) rt.anchoredPosition = v; }, onComplete));
    }

    /// <summary>Tween a Transform/RectTransform localScale with a punchy overshoot.</summary>
    public static TweenHandle Scale(Transform t, Vector3 to, float duration,
        Ease ease = Ease.BackOut, float delay = 0f, Action onComplete = null)
    {
        if (t == null) return null;
        Vector3 from = t.localScale;
        return Start(new TweenVec3(from, to, duration, delay, ease,
            delegate (Vector3 v) { if (t != null) t.localScale = v; }, onComplete));
    }

    /// <summary>Punch scale up (from 1 to <paramref name="to"/>) then ease back to 1.</summary>
    public static TweenHandle PunchScale(Transform t, float to, float duration,
        Ease ease = Ease.QuadOut, float delay = 0f, Action onComplete = null)
    {
        if (t == null) return null;
        Vector3 rest = t.localScale;
        TweenHandle h = Start(new TweenVec3(rest, rest * to, duration * 0.45f, delay, ease,
            delegate (Vector3 v) { if (t != null) t.localScale = v; }, delegate
            {
                if (t != null)
                {
                    Start(new TweenVec3(t.localScale, rest, duration * 0.55f, 0f, Ease.QuadOut,
                        delegate (Vector3 v) { if (t != null) t.localScale = v; }, onComplete));
                }
            }));
        return h;
    }

    /// <summary>Fade a CanvasGroup alpha.</summary>
    public static TweenHandle Alpha(CanvasGroup group, float to, float duration,
        Ease ease = Ease.QuadInOut, float delay = 0f, Action onComplete = null)
    {
        if (group == null) return null;
        float from = group.alpha;
        return Start(new TweenFloat(from, to, duration, delay, ease,
            delegate (float v) { if (group != null) group.alpha = v; }, onComplete));
    }

    /// <summary>Fade an Image (or any Graphic) color towards target alpha.</summary>
    public static TweenHandle FadeGraphic(UnityEngine.UI.Graphic graphic, float toAlpha, float duration,
        Ease ease = Ease.QuadInOut, float delay = 0f, Action onComplete = null)
    {
        if (graphic == null) return null;
        Color c = graphic.color;
        float from = c.a;
        return Start(new TweenFloat(from, toAlpha, duration, delay, ease,
            delegate (float v)
            {
                if (graphic != null)
                {
                    Color n = graphic.color;
                    n.a = v;
                    graphic.color = n;
                }
            }, onComplete));
    }
}

/// <summary>Hidden MonoBehaviour driving <see cref="Tweens"/> from the game loop.</summary>
internal sealed class TweenRunner : MonoBehaviour
{
    private void Update()
    {
        float dt = Time.deltaTime * Tweens.TimeScale;
        Tweens.Update(dt);
    }
}

// --------------------------------------------------------------------- tween primitives

internal abstract class TweenBase
{
    protected float Elapsed;
    protected readonly float Duration;
    protected readonly float Delay;
    protected readonly Ease Ease;
    protected readonly Action OnComplete;
    protected bool Killed;

    protected TweenBase(float duration, float delay, Ease ease, Action onComplete)
    {
        Duration = Mathf.Max(0.0001f, duration);
        Delay = Mathf.Max(0f, delay);
        Ease = ease;
        OnComplete = onComplete;
    }

    public void Kill()
    {
        Killed = true;
    }

    /// <summary>Returns true when finished (caller removes it).</summary>
    public bool Update(float dt)
    {
        if (Killed) return true;
        Elapsed += dt;
        if (Elapsed < Delay) return false;

        float t = (Elapsed - Delay) / Duration;
        if (t < 1f)
        {
            Apply(Easing.Apply(Ease, t));
            return false;
        }
        Apply(1f);
        if (OnComplete != null) OnComplete();
        return true;
    }

    protected abstract void Apply(float easedT);
}

internal sealed class TweenFloat : TweenBase
{
    private readonly float _from;
    private readonly float _to;
    private readonly Action<float> _onUpdate;

    public TweenFloat(float from, float to, float duration, float delay, Ease ease,
        Action<float> onUpdate, Action onComplete)
        : base(duration, delay, ease, onComplete)
    {
        _from = from;
        _to = to;
        _onUpdate = onUpdate;
    }

    protected override void Apply(float t)
    {
        if (_onUpdate != null) _onUpdate(Mathf.LerpUnclamped(_from, _to, t));
    }
}

internal sealed class TweenColor : TweenBase
{
    private readonly Color _from;
    private readonly Color _to;
    private readonly Action<Color> _onUpdate;

    public TweenColor(Color from, Color to, float duration, float delay, Ease ease,
        Action<Color> onUpdate, Action onComplete)
        : base(duration, delay, ease, onComplete)
    {
        _from = from;
        _to = to;
        _onUpdate = onUpdate;
    }

    protected override void Apply(float t)
    {
        if (_onUpdate != null) _onUpdate(Color.LerpUnclamped(_from, _to, t));
    }
}

internal sealed class TweenVec2 : TweenBase
{
    private readonly Vector2 _from;
    private readonly Vector2 _to;
    private readonly Action<Vector2> _onUpdate;

    public TweenVec2(Vector2 from, Vector2 to, float duration, float delay, Ease ease,
        Action<Vector2> onUpdate, Action onComplete)
        : base(duration, delay, ease, onComplete)
    {
        _from = from;
        _to = to;
        _onUpdate = onUpdate;
    }

    protected override void Apply(float t)
    {
        if (_onUpdate != null) _onUpdate(Vector2.LerpUnclamped(_from, _to, t));
    }
}

internal sealed class TweenVec3 : TweenBase
{
    private readonly Vector3 _from;
    private readonly Vector3 _to;
    private readonly Action<Vector3> _onUpdate;

    public TweenVec3(Vector3 from, Vector3 to, float duration, float delay, Ease ease,
        Action<Vector3> onUpdate, Action onComplete)
        : base(duration, delay, ease, onComplete)
    {
        _from = from;
        _to = to;
        _onUpdate = onUpdate;
    }

    protected override void Apply(float t)
    {
        if (_onUpdate != null) _onUpdate(Vector3.LerpUnclamped(_from, _to, t));
    }
}
