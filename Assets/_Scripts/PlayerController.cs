using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] public List<GridElement> gridElements;

    public int score;
    public int number;

    private void Start()
    {
        score = 0;
        UpdateScore();
    }

    public void UpdateScore()
    {
        CalculateScore();
        scoreText.text = score.ToString();
    }

    private void CalculateScore()
    {
        score = 0;
        foreach (GridElement element in gridElements)
        {
            score += element.GetValue();
        }
    }
}
