using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rolls bonus-round outcomes from the configured weights and applies their
/// effects to the two players with animated, staged feedback.
///
/// The rules follow the game spec:
///  - STEAL  takes control of up to 2 random zones owned by the rival
///           (a shielded zone loses its shield instead);
///  - BOOST  multiplies the value of up to 3 random own zones
///           (multipliers 2/3/5/10 with 35/30/25/10% chance each);
///  - SHIELD protects up to 2 random own zones;
///  - BLANK  does nothing.
///
/// Serialized field names are part of the scene wiring and must stay stable.
/// </summary>
public class OutcomeManager : MonoBehaviour
{
    [Header("Outcome weights (must add up to 1.0)")]
    [SerializeField] private float blankProbability = 0.35f;
    [SerializeField] private float stealProbability = 0.25f;
    [SerializeField] private float boostProbability = 0.30f;
    [SerializeField] private float shieldProbability = 0.10f;

    public float BlankProbability { get { return blankProbability; } }
    public float StealProbability { get { return stealProbability; } }
    public float BoostProbability { get { return boostProbability; } }
    public float ShieldProbability { get { return shieldProbability; } }

    private const float TotalWeight = 1.0f;

    // BOOST multiplier table (value, chance) — per spec.
    private static readonly int[] BoostValues = { 2, 3, 5, 10 };
    private static readonly float[] BoostChances = { 0.35f, 0.30f, 0.25f, 0.10f };

    /// <summary>Rolls a random outcome respecting the configured probabilities.</summary>
    public OutcomeType DetermineRandomOutcome()
    {
        float roll = Random.Range(0f, TotalWeight);
        float cumulative = 0f;

        cumulative += Mathf.Max(0f, blankProbability);
        if (roll < cumulative) return OutcomeType.BLANK;

        cumulative += Mathf.Max(0f, stealProbability);
        if (roll < cumulative) return OutcomeType.STEAL;

        cumulative += Mathf.Max(0f, boostProbability);
        if (roll < cumulative) return OutcomeType.BOOST;

        return OutcomeType.SHIELD;
    }

    /// <summary>
    /// Applies an outcome for <paramref name="actor"/> against
    /// <paramref name="rival"/>, playing its effect in a slow, readable
    /// sequence. Callers should yield on the returned enumerator.
    /// </summary>
    public IEnumerator PlayOutcome(OutcomeType outcome, PlayerController actor, PlayerController rival)
    {
        switch (outcome)
        {
            case OutcomeType.STEAL:
                yield return ApplySteal(actor, rival);
                break;
            case OutcomeType.BOOST:
                yield return ApplyBoost(actor);
                break;
            case OutcomeType.SHIELD:
                yield return ApplyShield(actor);
                break;
            default:
                yield return ApplyBlank(actor);
                break;
        }
    }

    // ------------------------------------------------------------------ STEAL

    private IEnumerator ApplySteal(PlayerController actor, PlayerController rival)
    {
        int targets = Mathf.Min(2, rival != null ? rival.ZonesControlled : 0);
        if (targets <= 0)
        {
            Sfx.Play(SfxId.Blank);
            yield return WaitFor.Seconds(0.35f);
            yield break;
        }

        List<GridElement> picks = PickDistinct(rival, targets);
        if (picks.Count <= 0)
        {
            Sfx.Play(SfxId.Blank);
            yield return WaitFor.Seconds(0.35f);
            yield break;
        }

        Sfx.Play(SfxId.Steal, 0.06f);
        for (int i = 0; i < picks.Count; i++)
        {
            GridElement zone = picks[i];
            if (zone == null) continue;

            if (zone.IsShielded)
            {
                // Shield intercepts the steal and breaks.
                Sfx.Play(SfxId.StealFail);
                zone.PlayShieldBreakVisuals();
                zone.ModifyShielded(false);
            }
            else
            {
                rival.DropZone(zone);
                actor.TakeZone(zone, true);
                ZoneFx.FloatTextOnCell(zone, "TAKEN!", GridElement.GetPlayerColorStatic(actor.number), 22f);
            }

            if (i < picks.Count - 1)
            {
                yield return WaitFor.Seconds(0.3f);
            }
        }
        yield return WaitFor.Seconds(0.2f);
    }

    // ------------------------------------------------------------------ BOOST

    private IEnumerator ApplyBoost(PlayerController actor)
    {
        List<GridElement> picks = PickDistinct(actor, 3);
        if (picks.Count <= 0)
        {
            Sfx.Play(SfxId.Blank);
            yield return WaitFor.Seconds(0.35f);
            yield break;
        }

        Sfx.Play(SfxId.Boost, 0.05f);
        for (int i = 0; i < picks.Count; i++)
        {
            GridElement zone = picks[i];
            if (zone == null) continue;

            int multiplier = RollBoostMultiplier();
            zone.ModifyValue(multiplier);
            zone.PlayBoostVisuals(multiplier);

            if (i < picks.Count - 1)
            {
                yield return WaitFor.Seconds(0.32f);
            }
        }
        yield return WaitFor.Seconds(0.2f);
    }

    // ------------------------------------------------------------------ SHIELD

    private IEnumerator ApplyShield(PlayerController actor)
    {
        List<GridElement> picks = PickDistinct(actor, 2);
        if (picks.Count <= 0)
        {
            Sfx.Play(SfxId.Blank);
            yield return WaitFor.Seconds(0.35f);
            yield break;
        }

        Sfx.Play(SfxId.Shield, 0.04f);
        for (int i = 0; i < picks.Count; i++)
        {
            GridElement zone = picks[i];
            if (zone == null) continue;

            // A pick that lands on an already-shielded zone is ignored (spec),
            // so only unprotected zones get the shield + feedback.
            if (!zone.IsShielded)
            {
                zone.ModifyShielded(true);
                ZoneFx.FloatTextOnCell(zone, "SHIELDED", OutcomeVisuals.ShieldColor, 20f);
            }

            if (i < picks.Count - 1)
            {
                yield return WaitFor.Seconds(0.24f);
            }
        }
        yield return WaitFor.Seconds(0.15f);
    }

    // ------------------------------------------------------------------ BLANK

    private IEnumerator ApplyBlank(PlayerController actor)
    {
        Sfx.Play(SfxId.Blank);
        yield return WaitFor.Seconds(0.4f);
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// Samples up to <paramref name="count"/> distinct zones controlled by
    /// the player (random order). Fewer are returned when the player controls
    /// fewer zones ("excess picks are ignored" per spec).
    /// </summary>
    private static List<GridElement> PickDistinct(PlayerController player, int count)
    {
        List<GridElement> result = new List<GridElement>();
        if (player == null || player.gridElements == null) return result;

        List<GridElement> pool = new List<GridElement>(player.gridElements);
        int wanted = Mathf.Min(count, pool.Count);
        for (int i = 0; i < wanted; i++)
        {
            // Random.Range(min, max) with an exclusive max is the correct
            // bounded-array index — never Random.Range(0, n + 1) which can
            // return n and crash with IndexOutOfRangeException.
            int index = Random.Range(0, pool.Count);
            GridElement zone = pool[index];
            pool.RemoveAt(index);
            if (zone != null) result.Add(zone);
        }
        return result;
    }

    private static int RollBoostMultiplier()
    {
        float roll = Random.Range(0f, 1f);
        float cumulative = 0f;
        for (int i = 0; i < BoostValues.Length; i++)
        {
            cumulative += BoostChances[i];
            if (roll < cumulative)
            {
                return BoostValues[i];
            }
        }
        return BoostValues[BoostValues.Length - 1];
    }
}
