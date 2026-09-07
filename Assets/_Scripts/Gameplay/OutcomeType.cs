using UnityEngine;

/// <summary>The four bonus-round outcomes.</summary>
public enum OutcomeType
{
    BLANK,
    STEAL,
    BOOST,
    SHIELD
}

/// <summary>
/// Static presentation helpers shared by the gameplay and the UI layers:
/// display names, short captions, accent colours and icons for outcomes.
/// </summary>
public static class OutcomeVisuals
{
    public static readonly Color BlankColor = new Color(0.62f, 0.64f, 0.68f);
    public static readonly Color StealColor = new Color(1.00f, 0.30f, 0.27f);
    public static readonly Color BoostColor = new Color(0.31f, 0.93f, 0.55f);
    public static readonly Color ShieldColor = new Color(0.28f, 0.76f, 1.00f);

    public static string GetLabel(OutcomeType outcome)
    {
        switch (outcome)
        {
            case OutcomeType.STEAL: return "STEAL";
            case OutcomeType.BOOST: return "BOOST";
            case OutcomeType.SHIELD: return "SHIELD";
            default: return "BLANK";
        }
    }

    public static string GetCaption(OutcomeType outcome)
    {
        switch (outcome)
        {
            case OutcomeType.STEAL: return "Takes up to 2 zones";
            case OutcomeType.BOOST: return "Boosts up to 3 zones";
            case OutcomeType.SHIELD: return "Shields up to 2 zones";
            default: return "Nothing happens";
        }
    }

    public static Color GetColor(OutcomeType outcome)
    {
        switch (outcome)
        {
            case OutcomeType.STEAL: return StealColor;
            case OutcomeType.BOOST: return BoostColor;
            case OutcomeType.SHIELD: return ShieldColor;
            default: return BlankColor;
        }
    }

    public static Sprite GetIcon(OutcomeType outcome)
    {
        switch (outcome)
        {
            case OutcomeType.STEAL: return SpriteFactory.ArrowRight();
            case OutcomeType.BOOST: return SpriteFactory.Diamond();
            case OutcomeType.SHIELD: return SpriteFactory.Ring();
            default: return SpriteFactory.Circle();
        }
    }

    /// <summary>Short word shown above an affected zone ("TAKEN", "BOOSTED"...).</summary>
    public static string GetZoneLabel(OutcomeType outcome)
    {
        switch (outcome)
        {
            case OutcomeType.STEAL: return "TAKEN";
            case OutcomeType.BOOST: return "BOOSTED";
            case OutcomeType.SHIELD: return "SHIELDED";
            default: return string.Empty;
        }
    }
}
