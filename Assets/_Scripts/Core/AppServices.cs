using UnityEngine;

/// <summary>
/// Central service registry for the game.
///
/// Everything is resolved lazily and re-resolved automatically after a scene
/// reload (Unity destroyed objects compare equal to null, so cached
/// references refresh themselves). Gameplay code should talk to this class
/// instead of scattering FindObjectOfType calls around.
/// </summary>
public static class App
{
    private static Canvas _canvas;
    private static RectTransform _canvasRect;

    /// <summary>
    /// Set to true in the editor to see detailed per-round log output.
    /// </summary>
    public static bool VerboseLogging
    {
        get { return Debug.unityLogger.logEnabled && _verbose; }
        set { _verbose = value; }
    }

    private static bool _verbose;

    /// <summary>The (single) screen-space Canvas that hosts the whole game UI.</summary>
    public static Canvas Canvas
    {
        get
        {
            if (_canvas == null)
            {
                _canvas = Object.FindObjectOfType<Canvas>();
                if (_canvas == null)
                {
                    Debug.LogError("[App] No Canvas found in the scene — the game UI cannot be built.");
                }
            }
            return _canvas;
        }
    }

    public static RectTransform CanvasRect
    {
        get
        {
            if (_canvasRect == null && Canvas != null) _canvasRect = Canvas.transform as RectTransform;
            return _canvasRect;
        }
    }

    /// <summary>
    /// Converts the centre of a RectTransform (zone, plate, board...) into
    /// Canvas local coordinates (suitable for anchoredPosition with the
    /// canvas-centre anchor). Works for any canvas render mode.
    /// </summary>
    public static bool TryGetCanvasCenter(RectTransform target, out Vector2 center)
    {
        center = Vector2.zero;
        if (target == null || CanvasRect == null) return false;

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector2 worldCenter = (corners[0] + corners[2]) * 0.5f;

        Camera cam = Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Canvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(CanvasRect, screenPoint, cam, out center);
    }

    /// <summary>Logs a verbose round event when verbose logging is enabled.</summary>
    public static void Log(string message)
    {
        if (_verbose) Debug.Log("[Game] " + message);
    }
}
