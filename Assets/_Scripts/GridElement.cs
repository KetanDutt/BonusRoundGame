using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GridElement : MonoBehaviour
{
    [SerializeField] private Color player1Color;
    [SerializeField] private Color player2Color;
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private int value = 2;
    [SerializeField] private bool isShielded = false;
    [SerializeField] private GameObject shieldedGO;
    [SerializeField] private GameObject selectedGO;
    [SerializeField] private int controllingPlayer = 0;

    void Start()
    {
        UpdateColor();
        UpdateText();
        selectedGO.SetActive(false);
    }

    private void SetSelected(bool select)
    {
        selectedGO.SetActive(select);
        if (select)
            StartCoroutine(offSelected());
    }

    private IEnumerator offSelected()
    {
        yield return new WaitForSeconds(1.0f);
        SetSelected(false);
    }

    public void ChangeControl(int newPlayer)
    {
        controllingPlayer = newPlayer;
        UpdateColor();
        SetSelected(true);
    }

    public int GetControllingPlayer()
    {
        return controllingPlayer;
    }

    public int GetValue()
    {
        return value;
    }

    public bool GetShielded()
    {
        return isShielded;
    }

    private void UpdateColor()
    {
        if (controllingPlayer == 1)
        {
            image.color = player1Color;
        }
        else if (controllingPlayer == 2)
        {
            image.color = player2Color;
        }
        else
        {
            image.color = Color.white;
        }

        shieldedGO.SetActive(isShielded);
    }

    private void UpdateText()
    {
        text.text = "x" + value;
    }

    public void ModifyValue(int amount)
    {
        value *= amount;
        UpdateText();
        SetSelected(true);
    }

    public void ModifyShielded(bool shielded)
    {
        isShielded = shielded;
        UpdateColor();
        SetSelected(true);
    }
}
