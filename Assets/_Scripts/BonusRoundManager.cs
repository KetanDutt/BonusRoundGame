using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class BonusRoundManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roundsLeftText;
    [SerializeField] private PlayerController player1Controller;
    [SerializeField] private TextMeshProUGUI player1Text;
    [SerializeField] private PlayerController player2Controller;
    [SerializeField] private TextMeshProUGUI player2Text;
    [SerializeField] private int roundsLeft = 10;

    [Header("Win Screen")]
    [SerializeField] private GameObject winScreen;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private Button replay;

    private OutcomeManager outcomeManager;

    private void Start()
    {
        outcomeManager = FindObjectOfType<OutcomeManager>();
        UpdateRoundsLeftText();
        StartCoroutine(PlayRounds());

        winScreen.SetActive(false);
        replay.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
    }

    private IEnumerator PlayRounds()
    {
        while (roundsLeft > 0)
        {
            Debug.Log("************ Round " + roundsLeft + " ************ ");
            OutcomeType outcomeType = outcomeManager.DetermineRandomOutcome();
            Debug.Log("Player 1 Outcome : " + outcomeType);
            ResetPlayersText();
            player1Text.text = outcomeType + "";
            yield return new WaitForSeconds(1.0f);

            outcomeManager.PlayOutcome(outcomeType, player1Controller, player2Controller);
            yield return new WaitForSeconds(1.0f);

            outcomeType = outcomeManager.DetermineRandomOutcome();
            Debug.Log("Player 2 Outcome : " + outcomeType);
            ResetPlayersText();
            player2Text.text = outcomeType + "";
            yield return new WaitForSeconds(1.0f);

            outcomeManager.PlayOutcome(outcomeType, player2Controller, player1Controller);
            roundsLeft--;
            UpdateRoundsLeftText();

            yield return new WaitForSeconds(1.0f);
        }
        DetermineWinner();
    }

    private void ResetPlayersText()
    {
        player1Text.text = "";
        player2Text.text = "";
    }

    private void UpdateRoundsLeftText()
    {
        roundsLeftText.text = roundsLeft.ToString();
    }

    private void DetermineWinner()
    {
        winScreen.SetActive(true);
        if (player1Controller.gridElements.Count > player2Controller.gridElements.Count)
        {
            Debug.Log("!!!!!!!!!!  Player 1 Wins  !!!!!!!!!!");
            winText.text = "Player 1 Wins";
        }
        else if (player2Controller.gridElements.Count > player1Controller.gridElements.Count)
        {
            Debug.Log("!!!!!!!!!!  Player 2 Wins  !!!!!!!!!!");
            winText.text = "Player 2 Wins";
        }
        else
        {
            if (player1Controller.score > player2Controller.score)
            {
                Debug.Log("!!!!!!!!!!  Player 1 Wins  !!!!!!!!!!");
                winText.text = "Player 1 Wins";
            }
            else if (player2Controller.score > player1Controller.score)
            {
                Debug.Log("!!!!!!!!!!  Player 2 Wins  !!!!!!!!!!");
                winText.text = "Player 2 Wins";
            }
            else
            {
                Debug.Log("!!!!!!!!!!  Draw  !!!!!!!!!!");
                winText.text = "Draw";
            }
        }
        winText.text += "\n\nPlayer 1 zones : " + player1Controller.gridElements.Count;
        winText.text += "\nPlayer 1 score : " + player1Controller.score;
        winText.text += "\nPlayer 2 zones : " + player2Controller.gridElements.Count;
        winText.text += "\nPlayer 2 score : " + player2Controller.score;
    }
}
