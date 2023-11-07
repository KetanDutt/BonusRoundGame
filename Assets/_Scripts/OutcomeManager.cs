using UnityEngine;

public class OutcomeManager : MonoBehaviour
{
    [SerializeField] private float blankProbability = 0.35f;
    [SerializeField] private float stealProbability = 0.25f;
    [SerializeField] private float boostProbability = 0.30f;
    [SerializeField] private float shieldProbability = 0.10f;

    public OutcomeType DetermineRandomOutcome()
    {
        float randomValue = Random.Range(0f, 1f);

        if (randomValue < blankProbability)
        {
            return OutcomeType.BLANK;
        }
        else if (randomValue < blankProbability + stealProbability)
        {
            return OutcomeType.STEAL;
        }
        else if (randomValue < blankProbability + stealProbability + boostProbability)
        {
            return OutcomeType.BOOST;
        }
        else
        {
            return OutcomeType.SHIELD;
        }
    }

    public void PlayOutcome(OutcomeType outcomeType, PlayerController player1, PlayerController player2)
    {
        switch (outcomeType)
        {
            case OutcomeType.STEAL:
                ApplyStealOutcome(player1, player2);
                break;
            case OutcomeType.BOOST:
                ApplyBoostOutcome(player1);
                break;
            case OutcomeType.SHIELD:
                ApplyShieldOutcome(player1);
                break;
            case OutcomeType.BLANK:
                break;
        }
        player1.UpdateScore();
        player2.UpdateScore();
    }

    // Method to apply the STEAL outcome
    public void ApplyStealOutcome(PlayerController player1, PlayerController player2)
    {
        if (player2.gridElements.Count > 0)
        {
            // Steal 2 random zones from the opponent
            int numZonesToSteal = Mathf.Min(2, player2.gridElements.Count);
            for (int i = 0; i < numZonesToSteal; i++)
            {
                int randomIndex = Random.Range(0, player2.gridElements.Count);
                GridElement zoneToSteal = player2.gridElements[randomIndex];
                if (zoneToSteal.GetShielded())
                {
                    zoneToSteal.ModifyShielded(false);
                }
                else
                {
                    zoneToSteal.ChangeControl(player1.number);
                    player1.gridElements.Add(zoneToSteal);
                    player2.gridElements.RemoveAt(randomIndex);
                }
            }
        }
    }

    // Method to apply the BOOST outcome
    public void ApplyBoostOutcome(PlayerController player)
    {
        // Define boost values and chances
        int[] boostValues = { 2, 3, 5, 10 };
        float[] boostChances = { 0.35f, 0.30f, 0.25f, 0.10f };

        for (int i = 0; i < 3; i++)
        {
            float randomValue = Random.Range(0f, 1f);

            float cumulativeChance = 0f;
            int boostValue = 0;

            for (int j = 0; j < boostChances.Length; j++)
            {
                cumulativeChance += boostChances[j];
                if (randomValue <= cumulativeChance)
                {
                    boostValue = boostValues[j];
                    break;
                }
            }

            if (player.gridElements.Count > 0)
            {
                int randomIndex = Random.Range(0, player.gridElements.Count);
                GridElement zoneToBoost = player.gridElements[randomIndex];
                zoneToBoost.ModifyValue(boostValue);
            }
        }
    }

    // Method to apply the SHIELD outcome
    public void ApplyShieldOutcome(PlayerController player)
    {
        int numZonesToShield = Mathf.Min(2, player.gridElements.Count);

        for (int i = 0; i < numZonesToShield; i++)
        {
            int randomIndex = Random.Range(0, player.gridElements.Count);
            GridElement zoneToShield = player.gridElements[randomIndex];
            zoneToShield.ModifyShielded(true);
        }
    }

    // Method to apply the BLANK outcome (no action is performed)
    public void ApplyBlankOutcome(PlayerController player)
    {
        // No action is performed for the BLANK outcome
    }
}

public enum OutcomeType
{
    BLANK,
    STEAL,
    BOOST,
    SHIELD
}