using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds and owns all runtime-generated UI (announcement cards, floating
/// texts, screen flashes, confetti, the odds legend and the SFX/speed
/// controls). Everything is created under the scene Canvas at runtime, so the
/// gameplay code does not depend on hand-placed scene objects for polish.
/// </summary>
public class RuntimeUi : MonoBehaviour
{
    public static RuntimeUi Instance { get; private set; }

    private RectTransform _fxLayer;
    private Image _flashImage;
    private CanvasGroup _legendGroup;
    private TextMeshProUGUI _sfxButtonText;
    private TextMeshProUGUI _speedButtonText;

    private const string SfxOnLabel = "SFX: ON";
    private const string SfxOffLabel = "SFX: OFF";

    public static bool TurboMode
    {
        get { return _turbo; }
    }

    private static bool _turbo;

    /// <summary>Time scale used while turbo (fast-forward) mode is active.</summary>
    public const float TurboTimeScale = 2.6f;

    // ---------------------------------------------------------------- lifecycle

    /// <summary>Ensures the runtime UI root exists under the scene Canvas.</summary>
    public static RuntimeUi EnsureCreated()
    {
        if (Instance != null) return Instance;
        if (App.Canvas == null) return null;

        // Turbo state and tween time scale must start fresh after a reload.
        _turbo = false;
        Tweens.TimeScale = 1f;

        GameObject go = new GameObject("[RuntimeUi]", typeof(RectTransform));
        go.transform.SetParent(App.Canvas.transform, false);
        Instance = go.AddComponent<RuntimeUi>();
        Instance.Build();
        return Instance;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Build()
    {
        RectTransform root = transform as RectTransform;
        UiKit.SetStretch(root);

        // Static elements first...
        BuildOddsLegend(root);
        BuildControls(root);

        // ...FX layer last so floats/cards/confetti render above everything.
        RectTransform layer = UiKit.AddRect(transform, "FxLayer");
        UiKit.SetStretch(layer);

        // Full-screen flash overlay used for outcome "punch" moments.
        _flashImage = UiKit.AddStretchImage(layer, "Flash", SpriteFactory.RoundedRect(), new Color(1f, 1f, 1f, 0f));
        _flashImage.raycastTarget = false;
        _flashImage.gameObject.SetActive(false);

        _fxLayer = layer;
    }

    // -------------------------------------------------------------- odds legend

    private void BuildOddsLegend(RectTransform root)
    {
        OutcomeManager outcomeManager = FindObjectOfType<OutcomeManager>();
        float blankP = outcomeManager != null ? outcomeManager.BlankProbability : 0.35f;
        float stealP = outcomeManager != null ? outcomeManager.StealProbability : 0.25f;
        float boostP = outcomeManager != null ? outcomeManager.BoostProbability : 0.30f;
        float shieldP = outcomeManager != null ? outcomeManager.ShieldProbability : 0.10f;

        // White card mirrored on the right side of the board area.
        RectTransform card = UiKit.AddRect(root, "OddsLegend");
        UiKit.CenterIn(card, new Vector2(455f, 30f), new Vector2(240f, 236f));
        Image bg = card.gameObject.AddComponent<Image>();
        bg.sprite = SpriteFactory.RoundedRect();
        bg.type = Image.Type.Sliced;
        bg.color = new Color(1f, 1f, 1f, 0.94f);

        TextMeshProUGUI title = UiKit.AddText(card, "Title", "OUTCOME ODDS", 19f,
            new Color(0.15f, 0.16f, 0.19f), FontStyles.Bold, TextAlignmentOptions.Center);
        UiKit.CenterIn(title.rectTransform, new Vector2(0f, 90f), new Vector2(220f, 26f));

        AddLegendRow(card, OutcomeType.STEAL, stealP, 46f);
        AddLegendRow(card, OutcomeType.BOOST, boostP, 4f);
        AddLegendRow(card, OutcomeType.SHIELD, shieldP, -38f);
        AddLegendRow(card, OutcomeType.BLANK, blankP, -80f);

        TextMeshProUGUI hint = UiKit.AddText(card, "Hint", "Auto-play demo", 11.5f,
            new Color(0.45f, 0.47f, 0.52f), FontStyles.Normal, TextAlignmentOptions.Center);
        UiKit.CenterIn(hint.rectTransform, new Vector2(0f, -106f), new Vector2(216f, 16f));

        _legendGroup = card.gameObject.AddComponent<CanvasGroup>();
    }

    private static void AddLegendRow(RectTransform parent, OutcomeType outcome, float probability, float y)
    {
        RectTransform row = UiKit.AddRect(parent, "Row_" + outcome);
        UiKit.CenterIn(row, new Vector2(0f, y), new Vector2(216f, 32f));

        Image chip = UiKit.AddImage(row, "Chip", SpriteFactory.Circle(), OutcomeVisuals.GetColor(outcome));
        UiKit.CenterIn(chip.rectTransform, new Vector2(-98f, 0f), new Vector2(16f, 16f));

        TextMeshProUGUI name = UiKit.AddText(row, "Name", OutcomeVisuals.GetLabel(outcome), 15.5f,
            new Color(0.16f, 0.17f, 0.20f), FontStyles.Bold, TextAlignmentOptions.Left);
        UiKit.CenterIn(name.rectTransform, new Vector2(-25f, 0f), new Vector2(122f, 24f));

        TextMeshProUGUI chance = UiKit.AddText(row, "Chance", Mathf.RoundToInt(probability * 100f) + "%", 15.5f,
            new Color(0.16f, 0.17f, 0.20f), FontStyles.Bold, TextAlignmentOptions.Right);
        UiKit.CenterIn(chance.rectTransform, new Vector2(88f, 0f), new Vector2(60f, 24f));
    }

    public void SetLegendVisible(bool visible)
    {
        if (_legendGroup != null)
        {
            _legendGroup.alpha = visible ? 1f : 0f;
            _legendGroup.blocksRaycasts = visible;
        }
    }

    // ----------------------------------------------------------------- controls

    private void BuildControls(RectTransform root)
    {
        Sfx.Initialize();

        RectTransform bar = UiKit.AddRect(root, "Controls");
        UiKit.CenterIn(bar, new Vector2(455f, -190f), new Vector2(240f, 44f));

        Button sfx = MakeToggle(bar, "SfxButton", new Vector2(-56f, 0f), 112f);
        _sfxButtonText = sfx.GetComponentInChildren<TextMeshProUGUI>();
        _sfxButtonText.text = Sfx.Volume > 0.01f ? SfxOnLabel : SfxOffLabel;
        sfx.onClick.AddListener(ToggleSfx);

        Button speed = MakeToggle(bar, "SpeedButton", new Vector2(58f, 0f), 112f);
        _speedButtonText = speed.GetComponentInChildren<TextMeshProUGUI>();
        _speedButtonText.text = "SPEED x1";
        speed.onClick.AddListener(ToggleSpeed);
    }

    private static Button MakeToggle(Transform parent, string name, Vector2 pos, float width)
    {
        RectTransform rt = UiKit.AddRect(parent, name);
        UiKit.CenterIn(rt, pos, new Vector2(width, 40f));

        Button button = rt.gameObject.AddComponent<Button>();
        Image bg = rt.gameObject.AddComponent<Image>();
        bg.sprite = SpriteFactory.RoundedRect();
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.14f, 0.15f, 0.18f, 0.92f);
        button.targetGraphic = bg;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TextMeshProUGUI label = UiKit.AddText(rt, "Label", "", 14f, Color.white, FontStyles.Bold);
        UiKit.SetStretch(label.rectTransform, 8f, 8f, 0f, 0f);
        return button;
    }

    private void ToggleSfx()
    {
        Sfx.Play(SfxId.UiClick);
        if (Sfx.Volume > 0.01f)
        {
            Sfx.Volume = 0f;
            _sfxButtonText.text = SfxOffLabel;
        }
        else
        {
            Sfx.Volume = 1f;
            _sfxButtonText.text = SfxOnLabel;
        }
    }

    private void ToggleSpeed()
    {
        _turbo = !_turbo;
        Tweens.TimeScale = _turbo ? TurboTimeScale : 1f;
        _speedButtonText.text = _turbo ? "SPEED x2.6" : "SPEED x1";
        Sfx.Play(SfxId.UiClick);
    }

    // ------------------------------------------------------- outcome announce card

    /// <summary>
    /// Plays the announce-then-clear card animation above a player's board.
    /// Used by the round choreographer; honours turbo mode automatically.
    /// </summary>
    public IEnumerator AnnounceOutcome(OutcomeType outcome, PlayerController player, RectTransform board)
    {
        if (board == null)
        {
            yield break;
        }

        Color outcomeColor = OutcomeVisuals.GetColor(outcome);
        Color playerColor = player != null ? player.AccentColor : Color.white;

        GameObject cardGo = new GameObject("AnnounceCard");
        RectTransform card = cardGo.AddComponent<RectTransform>();
        card.SetParent(_fxLayer, false);

        CanvasGroup group = cardGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        Image bg = cardGo.AddComponent<Image>();
        bg.sprite = SpriteFactory.RoundedRect();
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.055f, 0.06f, 0.09f, 0.9f);

        Image accentBar = UiKit.AddImage(card, "AccentBar", SpriteFactory.RoundedRect(), playerColor);
        RectTransform barRt = accentBar.rectTransform;
        barRt.anchorMin = new Vector2(0f, 0f);
        barRt.anchorMax = new Vector2(0f, 1f);
        barRt.pivot = new Vector2(0f, 0.5f);
        barRt.anchoredPosition = new Vector2(-2f, 0f);
        barRt.sizeDelta = new Vector2(7f, -14f);

        Image icon = UiKit.AddImage(card, "Icon", OutcomeVisuals.GetIcon(outcome), outcomeColor);
        UiKit.CenterIn(icon.rectTransform, new Vector2(-64f, 0f), new Vector2(44f, 44f));

        TextMeshProUGUI label = UiKit.AddText(card, "Label", OutcomeVisuals.GetLabel(outcome), 26f,
            Color.white, FontStyles.Bold, TextAlignmentOptions.Left);
        RectTransform labelRt = label.rectTransform;
        labelRt.anchorMin = new Vector2(0f, 0.5f);
        labelRt.anchorMax = new Vector2(0f, 0.5f);
        labelRt.pivot = new Vector2(0f, 0.5f);
        labelRt.anchoredPosition = new Vector2(-30f, 13f);
        labelRt.sizeDelta = new Vector2(150f, 30f);

        TextMeshProUGUI caption = UiKit.AddText(card, "Caption", OutcomeVisuals.GetCaption(outcome), 13f,
            new Color(0.78f, 0.82f, 0.87f), FontStyles.Normal, TextAlignmentOptions.Left);
        RectTransform capRt = caption.rectTransform;
        capRt.anchorMin = new Vector2(0f, 0.5f);
        capRt.anchorMax = new Vector2(0f, 0.5f);
        capRt.pivot = new Vector2(0f, 0.5f);
        capRt.anchoredPosition = new Vector2(-30f, -10f);
        capRt.sizeDelta = new Vector2(160f, 20f);

        // Position above the target board centre using canvas coordinates.
        Vector2 boardCenter;
        if (!App.TryGetCanvasCenter(board, out boardCenter))
        {
            UiKit.CenterIn(card, Vector2.zero, new Vector2(250f, 80f));
        }
        else
        {
            boardCenter.y += 52f;
            UiKit.CenterIn(card, boardCenter, new Vector2(250f, 80f));
        }

        // Entrance: fade + overshoot scale.
        card.localScale = new Vector3(0.86f, 0.86f, 1f);
        float inDur = 0.24f;
        Tweens.Alpha(group, 1f, inDur, Ease.QuadOut);
        Tweens.Scale(card, Vector3.one, inDur, Ease.BackOut);
        Sfx.Play(SfxId.Whoosh, 0.05f);
        yield return WaitFor.Seconds(inDur + 0.05f);

        // Hold while the player reads it.
        yield return WaitFor.Seconds(0.42f);

        // Exit.
        float outDur = 0.2f;
        Tweens.Alpha(group, 0f, outDur, Ease.QuadIn);
        Tweens.Scale(card, new Vector3(1.04f, 1.04f, 1f), outDur, Ease.QuadIn);
        yield return WaitFor.Seconds(outDur);

        Destroy(cardGo);
    }

    // ------------------------------------------------------------- floating text

    /// <summary>
    /// Spawns a text that rises, scales in and fades out at a canvas position.
    /// </summary>
    public void FloatingText(string message, Vector2 canvasPos, Color color, float size = 24f)
    {
        if (_fxLayer == null) return;

        TextMeshProUGUI tmp = UiKit.AddText(_fxLayer, "FloatText", message, size, color, FontStyles.Bold);
        RectTransform rt = tmp.rectTransform;
        UiKit.CenterIn(rt, canvasPos, new Vector2(220f, 34f));
        tmp.alignment = TextAlignmentOptions.Center;

        StartCoroutine(FloatTextRoutine(tmp));
    }

    private IEnumerator FloatTextRoutine(TextMeshProUGUI tmp)
    {
        RectTransform rt = tmp.rectTransform;
        Vector2 start = rt.anchoredPosition;
        float duration = 1.0f;

        // Rise + fade.
        Tweens.Vector2(start, start + new Vector2(0f, 64f), duration, delegate (Vector2 v)
        {
            if (rt != null) rt.anchoredPosition = v;
        }, Ease.QuadOut);

        // Scale pop.
        rt.localScale = new Vector3(1.35f, 1.35f, 1f);
        Tweens.Scale(rt, Vector3.one, 0.16f, Ease.BackOut);

        float half = duration * 0.5f;
        yield return WaitFor.Seconds(half);
        Tweens.Float(1f, 0f, duration - half, delegate (float a)
        {
            if (tmp != null)
            {
                Color c = tmp.color;
                c.a = a;
                tmp.color = c;
            }
        }, Ease.Linear);
        yield return WaitFor.Seconds(duration - half + 0.05f);
        Destroy(tmp.gameObject);
    }

    // --------------------------------------------------------------- screen flash

    /// <summary>Brief full-screen colour flash (punch moment on outcomes).</summary>
    public void ScreenFlash(Color color, float peakAlpha = 0.16f, float fadeDuration = 0.3f)
    {
        if (_flashImage == null) return;
        _flashImage.color = new Color(color.r, color.g, color.b, peakAlpha);
        _flashImage.gameObject.SetActive(true);
        Tweens.FadeGraphic(_flashImage, 0f, fadeDuration, Ease.QuadOut, 0f, delegate
        {
            if (_flashImage != null) _flashImage.gameObject.SetActive(false);
        });
    }

    // ----------------------------------------------------------------- confetti

    private static readonly Color[] ConfettiColors =
    {
        new Color(1.00f, 0.55f, 0.20f), new Color(0.25f, 0.78f, 1.00f), new Color(1.00f, 0.92f, 0.30f),
        new Color(0.55f, 0.95f, 0.45f), new Color(1.00f, 0.42f, 0.65f), new Color(0.85f, 0.55f, 1.00f)
    };

    /// <summary>Celebration confetti raining over the whole screen.</summary>
    public void BurstConfetti(int count = 44)
    {
        if (_fxLayer == null) return;
        for (int i = 0; i < count; i++)
        {
            Color color = ConfettiColors[Random.Range(0, ConfettiColors.Length)];
            Sprite sprite = Random.value < 0.5f ? SpriteFactory.Rectangle() : SpriteFactory.Diamond();
            float w = Random.Range(9f, 15f);
            Image piece = UiKit.AddImage(_fxLayer, "Confetti", sprite, color);
            RectTransform rt = piece.rectTransform;
            Vector2 size = new Vector2(w, w * Random.Range(0.6f, 1.6f));
            UiKit.CenterIn(rt, new Vector2(Random.Range(-900f, 900f), Random.Range(480f, 640f)), size);
            StartCoroutine(ConfettiRoutine(piece));
        }
    }

    private IEnumerator ConfettiRoutine(Image piece)
    {
        RectTransform rt = piece.rectTransform;
        float duration = Random.Range(1.7f, 2.7f);
        float elapsed = 0f;
        float swayPhase = Random.Range(0f, 6.28f);
        float swayAmp = Random.Range(14f, 42f);
        Vector2 start = rt.anchoredPosition;
        Vector2 end = new Vector2(start.x, -660f);
        float spinDegrees = Random.Range(300f, 900f) * (Random.value < 0.5f ? -1f : 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime * Tweens.TimeScale;
            float t = elapsed / duration;
            float sway = Mathf.Sin(t * 10f + swayPhase) * swayAmp * (1f - t);
            rt.anchoredPosition = Vector2.Lerp(start, end, t) + new Vector2(sway, 0f);
            rt.Rotate(0f, 0f, spinDegrees * Time.deltaTime * Tweens.TimeScale);
            if (t > 0.7f && piece != null)
            {
                Color c = piece.color;
                c.a = Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);
                piece.color = c;
            }
            yield return null;
        }
        Destroy(piece.gameObject);
    }
}
