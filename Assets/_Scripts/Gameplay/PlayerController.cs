using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// One contestant (a bot in this autoplay demo). Keeps track of the zones the
/// player currently controls and renders their running score.
///
/// Serialized field names are part of the scene wiring and must stay stable.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Zones")]
    [Tooltip("Zones currently controlled by this player (wired in the scene, 20 cells).")]
    [SerializeField] public List<GridElement> gridElements = new List<GridElement>();

    [Header("State")]
    [SerializeField] public int score;
    [SerializeField] public int number;

    private TweenHandle _scoreTween;

    /// <summary>How many zones this player currently controls.</summary>
    public int ZonesControlled
    {
        get { return gridElements != null ? gridElements.Count : 0; }
    }

    public bool HasZones
    {
        get { return ZonesControlled > 0; }
    }

    /// <summary>Display colour of this player (resolved from the zone prefab tints).</summary>
    public Color AccentColor
    {
        get { return GridElement.GetPlayerColorStatic(number); }
    }

    /// <summary>The white player card under the score (used for FX anchoring).</summary>
    public RectTransform PlateRect
    {
        get
        {
            if (scoreText != null)
            {
                Transform parent = scoreText.transform.parent;
                if (parent != null) return parent as RectTransform;
            }
            return transform as RectTransform;
        }
    }

    /// <summary>Subtle attention pulse on the player's score card.</summary>
    public void PulsePlate()
    {
        RectTransform plate = PlateRect;
        if (plate != null) Tweens.PunchScale(plate, 1.035f, 0.4f);
    }

    private void Awake()
    {
        if (gridElements == null) gridElements = new List<GridElement>();
        // The scene serializes a stale score; always resync from zone values.
        SetScoreTextSilent(CalculateScore());
    }

    /// <summary>Rebuilds initial zone ownership from the (serialized) zone list.</summary>
    public void InitializeOwnership(bool animated)
    {
        for (int i = 0; i < gridElements.Count; i++)
        {
            GridElement zone = gridElements[i];
            if (zone == null) continue;
            zone.ChangeControl(number, animated);
        }
    }

    /// <summary>Adds a zone to this player's controlled set (steal result).</summary>
    public void TakeZone(GridElement zone, bool animated = true)
    {
        if (zone == null || gridElements.Contains(zone)) return;
        zone.ChangeControl(number, animated);
        gridElements.Add(zone);
    }

    /// <summary>Removes a zone from this player's controlled set (stolen away).</summary>
    public void DropZone(GridElement zone)
    {
        if (zone == null) return;
        gridElements.Remove(zone);
    }

    public bool Owns(GridElement zone)
    {
        return zone != null && gridElements.Contains(zone);
    }

    /// <summary>Recalculates the score from zone values.</summary>
    public int CalculateScore()
    {
        int total = 0;
        for (int i = 0; i < gridElements.Count; i++)
        {
            if (gridElements[i] != null) total += gridElements[i].Value;
        }
        return total;
    }

    /// <summary>Syncs score + text (optionally with a count-up animation).</summary>
    public void RefreshScore(bool animate = true)
    {
        int next = CalculateScore();
        if (score == next)
        {
            return;
        }
        int old = score;
        score = next;

        if (scoreText == null) return;

        if (_scoreTween != null) _scoreTween.Cancel();
        if (!animate || !gameObject.activeInHierarchy)
        {
            SetScoreTextSilent(score);
            return;
        }

        _scoreTween = Tweens.Float(old, next, 0.5f, delegate (float v)
        {
            if (scoreText != null) scoreText.text = Mathf.RoundToInt(v).ToString();
        }, Ease.QuadOut, 0f, delegate
        {
            if (scoreText != null) scoreText.text = score.ToString();
            _scoreTween = null;
        });

        // Gentle pulse of the score text itself.
        if (scoreText != null)
        {
            RectTransform rt = scoreText.rectTransform;
            Tweens.PunchScale(rt, 1.25f, 0.3f);
        }
        Sfx.Play(SfxId.ScorePop, 0.04f);
    }

    private void SetScoreTextSilent(int value)
    {
        score = value;
        if (scoreText != null) scoreText.text = value.ToString();
    }
}
