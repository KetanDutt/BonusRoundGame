using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single 1x1 zone of a 4x5 board.
///
/// Owns the zone's model state (owner, multiplier value, shield) and its
/// visuals (colour, "xN" label, shield/selection overlays). Visual changes
/// are animated through the tween system — callers never jump-cut colours.
///
/// Serialized field names are part of the scene wiring and must stay stable.
/// </summary>
public class GridElement : MonoBehaviour
{
    [Header("Player tints")]
    [SerializeField] private Color player1Color = new Color(1f, 0.345f, 0f, 1f);
    [SerializeField] private Color player2Color = new Color(0.07f, 0.753f, 1f, 1f);

    [Header("References (set in prefab)")]
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private GameObject shieldedGO;
    [SerializeField] private GameObject selectedGO;

    [Header("State")]
    [SerializeField] private int value = 2;
    [SerializeField] private bool isShielded = false;
    [SerializeField] private int controllingPlayer = 0;

    // Cached, resolved at runtime (kept out of serialization).
    private Image _shieldImage;
    private Image _selectedImage;
    private RectTransform _shieldRt;
    private RectTransform _selectedRt;
    private RectTransform _rt;
    private bool _resolved;

    /// <summary>Player number currently controlling the zone (0 = nobody).</summary>
    public int ControllingPlayer
    {
        get { return controllingPlayer; }
    }

    /// <summary>Current multiplier of the zone (its score contribution).</summary>
    public int Value
    {
        get { return value; }
    }

    /// <summary>True while the zone is protected against steals.</summary>
    public bool IsShielded
    {
        get { return isShielded; }
    }

    public Color PlayerColor
    {
        get
        {
            if (controllingPlayer == 1) return player1Color;
            if (controllingPlayer == 2) return player2Color;
            return Color.white;
        }
    }

    private void Awake()
    {
        ResolveReferences();
    }

    // Palette shared by presentation code (player plates, cards, rings...).
    private static Color _staticP1 = new Color(1f, 0.345f, 0f, 1f);
    private static Color _staticP2 = new Color(0.07f, 0.753f, 1f, 1f);
    private static bool _paletteResolved;

    /// <summary>Accent colour for a player, taken from the live zone prefab tints.</summary>
    public static Color GetPlayerColorStatic(int player)
    {
        if (!_paletteResolved)
        {
            _paletteResolved = true;
            GridElement sample = FindObjectOfType<GridElement>();
            if (sample != null)
            {
                _staticP1 = sample.player1Color;
                _staticP2 = sample.player2Color;
            }
        }
        if (player == 1) return _staticP1;
        if (player == 2) return _staticP2;
        return Color.white;
    }

    private void ResolveReferences()
    {
        if (_resolved) return;
        _resolved = true;
        _rt = transform as RectTransform;
        if (shieldedGO != null)
        {
            _shieldImage = shieldedGO.GetComponent<Image>();
            _shieldRt = shieldedGO.transform as RectTransform;
        }
        if (selectedGO != null)
        {
            _selectedImage = selectedGO.GetComponent<Image>();
            _selectedRt = selectedGO.transform as RectTransform;
        }
    }

    private void Start()
    {
        // Runtime re-skin of the placeholder overlays with generated art.
        ResolveReferences();
        if (_shieldImage != null)
        {
            _shieldImage.sprite = SpriteFactory.Ring();
            _shieldImage.color = new Color(0.62f, 0.80f, 1f, 0.92f);
            _shieldImage.raycastTarget = false;
        }
        if (_selectedImage != null)
        {
            _selectedImage.sprite = SpriteFactory.Ring();
            _selectedImage.color = new Color(1f, 1f, 1f, 0.85f);
            _selectedImage.raycastTarget = false;
        }
        ApplyModelToVisuals(false);
    }

    // ------------------------------------------------------------------ model

    /// <summary>
    /// Assigns ownership of the zone to a player (0 = neutral). The zone's
    /// colour transitions smoothly and a takeover pulse plays when animated.
    /// </summary>
    public void ChangeControl(int newPlayer, bool animated = true)
    {
        if (controllingPlayer == newPlayer)
        {
            // Already owned by this player — nothing to do.
            return;
        }

        int oldPlayer = controllingPlayer;
        controllingPlayer = newPlayer;
        AnimateToOwnerVisuals(oldPlayer, animated);
    }

    /// <summary>Multiplies the zone value by <paramref name="amount"/> (BOOST).</summary>
    public void ModifyValue(int amount)
    {
        value *= amount;
        if (text != null) text.text = "x" + value;
        ZoneFx.Pop(_rt != null ? _rt : (transform as RectTransform));
    }

    /// <summary>Turns the zone's shield on or off (SHIELD / shield break).</summary>
    public void ModifyShielded(bool shielded, bool animated = true)
    {
        isShielded = shielded;
        ApplyShieldVisuals(animated);
    }

    // ------------------------------------------------------------------ visuals

    private void AnimateToOwnerVisuals(int oldPlayer, bool animated)
    {
        if (image == null) return;

        Color target = PlayerColor;
        Color source = oldPlayer == 1 ? player1Color : oldPlayer == 2 ? player2Color : Color.white;

        if (!animated || !gameObject.activeInHierarchy)
        {
            image.color = target;
            FlashSelection();
            return;
        }

        Tweens.Color(source, target, 0.28f, delegate (Color c)
        {
            if (image != null) image.color = c;
        }, Ease.QuadInOut);

        if (_rt != null)
        {
            ZoneFx.Pop(_rt, 1.14f);
            ZoneFx.EmitRing(_rt, target, 1.75f);
        }
        FlashSelection();
    }

    /// <summary>Applies the serialized model (owner/value/shield) to the visuals.</summary>
    public void ApplyModelToVisuals(bool animated = false)
    {
        if (image != null)
        {
            Color target = PlayerColor;
            if (!animated)
            {
                image.color = target;
            }
            else
            {
                Tweens.Color(image.color, target, 0.3f, delegate (Color c)
                {
                    if (image != null) image.color = c;
                }, Ease.QuadInOut);
            }
        }
        if (text != null) text.text = "x" + value;
        ApplyShieldVisuals(animated);
        if (selectedGO != null) selectedGO.SetActive(false);
    }

    /// <summary>
    /// Shows/hides the shield ring. When animated, the ring scales in (apply)
    /// or fades out (shield break); instant mode is used on boot so no stale
    /// overlay ever flashes on screen.
    /// </summary>
    private void ApplyShieldVisuals(bool animated = true)
    {
        if (shieldedGO == null) return;

        if (isShielded)
        {
            if (!shieldedGO.activeSelf)
            {
                shieldedGO.SetActive(true);
                if (_shieldRt != null)
                {
                    if (animated)
                    {
                        _shieldRt.localScale = Vector3.one * 1.5f;
                        Tweens.Scale(_shieldRt, Vector3.one, 0.3f, Ease.BackOut);
                    }
                    else
                    {
                        _shieldRt.localScale = Vector3.one;
                    }
                }
            }
        }
        else if (shieldedGO.activeSelf)
        {
            if (animated && _shieldImage != null)
            {
                Tweens.FadeGraphic(_shieldImage, 0f, 0.22f, Ease.QuadIn, 0f, delegate
                {
                    if (_shieldImage != null) _shieldImage.color = new Color(0.62f, 0.80f, 1f, 0.92f);
                });
                Tweens.Delay(0.22f, delegate
                {
                    if (shieldedGO != null) shieldedGO.SetActive(false);
                });
            }
            else
            {
                shieldedGO.SetActive(false);
            }
        }
    }

    /// <summary>Brief selection highlight (used on every state change).</summary>
    private void FlashSelection()
    {
        if (selectedGO == null) return;
        selectedGO.SetActive(true);
        if (_selectedRt != null)
        {
            _selectedRt.localScale = Vector3.one * 0.7f;
            Tweens.Scale(_selectedRt, Vector3.one, 0.18f, Ease.BackOut);
        }
        Tweens.Delay(0.22f, delegate
        {
            if (selectedGO != null) selectedGO.SetActive(false);
        });
    }

    /// <summary>Visual + text feedback when this zone's value is boosted.</summary>
    public void PlayBoostVisuals(int multiplier)
    {
        if (_rt != null)
        {
            ZoneFx.Pop(_rt, 1.24f);
            ZoneFx.EmitRing(_rt, new Color(1f, 0.86f, 0.3f), 1.9f);
        }
        if (text != null)
        {
            Tweens.PunchScale(text.rectTransform, 1.6f, 0.4f);
            Tweens.Delay(0.08f, delegate
            {
                if (text != null) text.text = "x" + value;
            });
        }
        ZoneFx.FloatTextOnCell(this, "x" + multiplier, new Color(1f, 0.9f, 0.45f), 24f);
    }

    /// <summary>Feedback when a steal attempt hits this zone's shield.</summary>
    public void PlayShieldBreakVisuals()
    {
        if (_rt != null)
        {
            ZoneFx.Shake(_rt, 7f, 0.24f);
            ZoneFx.EmitRing(_rt, new Color(1f, 0.45f, 0.35f), 2f);
        }
        ZoneFx.FloatTextOnCell(this, "SHIELD!", new Color(1f, 0.5f, 0.42f), 20f);
    }
}
