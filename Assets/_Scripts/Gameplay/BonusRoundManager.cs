using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Choreographs the whole bonus-round show:
///
///  - runs the fixed number of autoplay rounds (Player 1 first, then Player 2);
///  - shows an outcome announcement card above the acting player's board and
///    keeps the per-player status line updated;
///  - resolves each outcome through the OutcomeManager;
///  - animates the score cards, the rounds-left counter and finally the win
///    banner (most zones wins; equal zones break the tie by score).
///
/// Serialized field names are part of the scene wiring and must stay stable.
/// </summary>
public class BonusRoundManager : MonoBehaviour
{
    [Header("Scene wiring (do not rename — serialized references)")]
    [SerializeField] private TextMeshProUGUI roundsLeftText;
    [SerializeField] private PlayerController player1Controller;
    [SerializeField] private TextMeshProUGUI player1Text;
    [SerializeField] private PlayerController player2Controller;
    [SerializeField] private TextMeshProUGUI player2Text;
    [SerializeField] private int roundsLeft = 10;

    [Header("Win screen")]
    [SerializeField] private GameObject winScreen;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private Button replay;

    [Header("Pacing (seconds)")]
    [SerializeField] private float settleDelay = 0.5f;
    [SerializeField] private float turnGap = 0.14f;
    [SerializeField] private float roundGap = 0.55f;

    private OutcomeManager outcomeManager;
    private int _initialRounds;
    private RectTransform _board1;
    private RectTransform _board2;
    private CanvasGroup _winGroup;
    private bool _replayWired;

    private void Awake()
    {
        outcomeManager = FindObjectOfType<OutcomeManager>();

        // Make sure the runtime FX/UI layer exists under the canvas.
        RuntimeUi.EnsureCreated();

        _initialRounds = Mathf.Max(0, roundsLeft);

        // Boards are the parents of each player's first zone.
        _board1 = FindBoard(player1Controller);
        _board2 = FindBoard(player2Controller);

        ClearStatusText();
        UpdateRoundsLeftLabel(false);
    }

    private void Start()
    {
        WireReplayButton();
        StartCoroutine(RunBonusRounds());
    }

    // ------------------------------------------------------------------ helpers

    private static RectTransform FindBoard(PlayerController player)
    {
        if (player == null || player.gridElements == null || player.gridElements.Count == 0)
        {
            return null;
        }
        GridElement zone = player.gridElements[0];
        if (zone == null) return null;
        return zone.transform.parent as RectTransform;
    }

    private void ClearStatusText()
    {
        SetStatus(player1Text, string.Empty, Color.white);
        SetStatus(player2Text, string.Empty, Color.white);
    }

    private static void SetStatus(TextMeshProUGUI label, string text, Color color)
    {
        if (label == null) return;
        label.text = text;
        label.color = color;
        label.rectTransform.localScale = Vector3.one;
    }

    private void UpdateRoundsLeftLabel(bool animate)
    {
        if (roundsLeftText == null) return;
        roundsLeftText.text = Mathf.Max(0, roundsLeft).ToString();
        if (animate)
        {
            Tweens.PunchScale(roundsLeftText.rectTransform, 1.35f, 0.4f);
            Sfx.Play(SfxId.Tick, 0.05f);
        }
    }

    private void WireReplayButton()
    {
        if (replay == null || _replayWired) return;
        _replayWired = true;
        replay.onClick.AddListener(RestartGame);
    }

    private void RestartGame()
    {
        Sfx.Play(SfxId.UiClick);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ------------------------------------------------------------------ main loop

    private IEnumerator RunBonusRounds()
    {
        // Let the scene settle (initial paints, UI layout pass).
        yield return WaitFor.Seconds(settleDelay + 0.15f);

        // Small "lights up" beat at the start of the show.
        Sfx.Play(SfxId.Whoosh, 0.08f);
        RuntimeUi ui = RuntimeUi.Instance;
        if (ui != null) ui.ScreenFlash(new Color(1f, 1f, 1f), 0.05f, 0.6f);
        yield return WaitFor.Seconds(0.2f);

        while (roundsLeft > 0)
        {
            yield return PlayRound();

            roundsLeft--;
            UpdateRoundsLeftLabel(true);
            if (roundsLeft > 0)
            {
                yield return WaitFor.Seconds(roundGap);
            }
        }

        yield return EndGame();
    }

    private IEnumerator PlayRound()
    {
        // Player 1's outcome is evaluated first, then Player 2's (per spec).
        yield return PlayTurn(player1Controller, player2Controller, player1Text, _board1);
        yield return WaitFor.Seconds(turnGap);
        yield return PlayTurn(player2Controller, player1Controller, player2Text, _board2);
    }

    private IEnumerator PlayTurn(PlayerController actor, PlayerController rival,
        TextMeshProUGUI statusText, RectTransform board)
    {
        if (actor == null || rival == null)
        {
            yield break;
        }

        OutcomeType outcome = outcomeManager != null
            ? outcomeManager.DetermineRandomOutcome()
            : OutcomeType.BLANK;

        App.Log("Round " + _initialRounds + " | Player " + actor.number + " outcome: " + outcome);

        // Per-player status line (left column of the HUD).
        if (statusText != null)
        {
            SetStatus(statusText, OutcomeVisuals.GetLabel(outcome), OutcomeVisuals.GetColor(outcome));
            Tweens.PunchScale(statusText.rectTransform, 1.3f, 0.32f);
        }

        int scoreBefore = actor.score;
        int rivalScoreBefore = rival != null ? rival.score : 0;

        actor.PulsePlate();
        if (rival != null && rival.HasZones) rival.PulsePlate();

        RuntimeUi ui = RuntimeUi.Instance;

        // Announcement card above the acting player's board.
        if (ui != null && board != null)
        {
            yield return ui.AnnounceOutcome(outcome, actor, board);
        }
        else
        {
            yield return WaitFor.Seconds(0.7f);
        }

        // Punchy accent flash as the outcome resolves.
        if (ui != null)
        {
            ui.ScreenFlash(OutcomeVisuals.GetColor(outcome), 0.05f, 0.35f);
        }
        Sfx.Play(SfxId.Reveal, 0.06f);

        if (outcomeManager != null)
        {
            yield return outcomeManager.PlayOutcome(outcome, actor, rival);
        }
        else
        {
            yield return WaitFor.Seconds(0.3f);
        }

        // Score cards count up to their new totals.
        if (actor != null) actor.RefreshScore(true);
        if (rival != null) rival.RefreshScore(true);

        ShowScoreDelta(actor, actor != null ? actor.score - scoreBefore : 0, GridElement.GetPlayerColorStatic(actor.number));
        if (rival != null)
        {
            ShowScoreDelta(rival, rival.score - rivalScoreBefore, new Color(0.85f, 0.55f, 0.5f));
        }

        yield return WaitFor.Seconds(0.12f);
    }

    /// <summary>Floats a +N / -N above the player's score card.</summary>
    private static void ShowScoreDelta(PlayerController player, int delta, Color color)
    {
        if (player == null || delta == 0) return;
        RectTransform plate = player.PlateRect;
        if (plate == null) return;

        Vector2 point;
        if (!App.TryGetCanvasCenter(plate, out point)) return;
        point.y += 34f;

        string label = delta > 0 ? "+" + delta : delta.ToString();
        RuntimeUi ui = RuntimeUi.Instance;
        if (ui != null) ui.FloatingText(label, point, color, 26f);
    }

    // ------------------------------------------------------------------ game over

    private IEnumerator EndGame()
    {
        yield return WaitFor.Seconds(0.45f);

        // Last dramatic tick.
        Sfx.Play(SfxId.Tick);
        RuntimeUi ui = RuntimeUi.Instance;
        if (ui != null) ui.ScreenFlash(Color.white, 0.07f, 0.45f);

        yield return WaitFor.Seconds(0.35f);

        int zones1 = player1Controller != null ? player1Controller.ZonesControlled : 0;
        int zones2 = player2Controller != null ? player2Controller.ZonesControlled : 0;
        int score1 = player1Controller != null ? player1Controller.score : 0;
        int score2 = player2Controller != null ? player2Controller.score : 0;

        int winner = ResolveWinner(zones1, zones2, score1, score2);
        App.Log(string.Format("Game over -> zones {0}:{1}  score {2}:{3}  winner={4}", zones1, zones2, score1, score2, winner));

        // Celebrate.
        if (winner != 0)
        {
            Sfx.Play(SfxId.Win);
            if (ui != null) ui.BurstConfetti(52);
        }
        else
        {
            Sfx.Play(SfxId.Draw);
            if (ui != null) ui.BurstConfetti(20);
        }

        yield return ShowWinScreen(winner, zones1, zones2, score1, score2);
    }

    /// <summary>
    /// Zone count decides first; equal zone counts fall back to total score;
    /// still equal means a draw. Returns 0 (draw), 1 or 2.
    /// </summary>
    private static int ResolveWinner(int zones1, int zones2, int score1, int score2)
    {
        if (zones1 > zones2) return 1;
        if (zones2 > zones1) return 2;
        if (score1 > score2) return 1;
        if (score2 > score1) return 2;
        return 0;
    }

    private IEnumerator ShowWinScreen(int winner, int zones1, int zones2, int score1, int score2)
    {
        if (winScreen == null)
        {
            yield break;
        }

        // Fade the overlay in (it ships fully opaque, so we control alpha).
        _winGroup = winScreen.GetComponent<CanvasGroup>();
        if (_winGroup == null) _winGroup = winScreen.AddComponent<CanvasGroup>();
        _winGroup.alpha = 0f;
        _winGroup.interactable = false;
        winScreen.SetActive(true);

        RuntimeUi ui = RuntimeUi.Instance;
        if (ui != null) ui.SetLegendVisible(false);

        // Rich-text headline + stats.
        if (winText != null)
        {
            winText.text = ComposeWinText(winner, zones1, zones2, score1, score2);
            winText.rectTransform.localScale = new Vector3(0.82f, 0.82f, 1f);
        }

        Tweens.Alpha(_winGroup, 1f, 0.4f, Ease.QuadOut, 0f, delegate
        {
            if (_winGroup != null) _winGroup.interactable = true;
        });

        if (winText != null)
        {
            Tweens.Scale(winText.rectTransform, Vector3.one, 0.5f, Ease.BackOut, 0.18f);
        }
        if (replay != null)
        {
            replay.transform.localScale = Vector3.zero;
            Tweens.Scale(replay.transform, Vector3.one, 0.45f, Ease.BackOut, 0.42f);
        }

        yield return WaitFor.Seconds(0.2f);
    }

    private static string ComposeWinText(int winner, int zones1, int zones2, int score1, int score2)
    {
        Color accent1 = GridElement.GetPlayerColorStatic(1);
        Color accent2 = GridElement.GetPlayerColorStatic(2);
        string c1 = "#" + ColorUtility.ToHtmlStringRGB(accent1);
        string c2 = "#" + ColorUtility.ToHtmlStringRGB(accent2);

        string headline;
        if (winner == 0)
        {
            headline = "<color=#FFE082>IT'S A DRAW!</color>";
        }
        else
        {
            string wc = winner == 1 ? c1 : c2;
            headline = "<color=" + wc + ">PLAYER " + winner + "</color> <color=#FFFFFF>WINS!</color>";
        }

        string p1Line = "<color=" + c1 + ">PLAYER 1</color>  <color=#E8ECF0>Zones " + zones1 +
                        "  ·  Score " + score1 + "</color>";
        string p2Line = "<color=" + c2 + ">PLAYER 2</color>  <color=#E8ECF0>Zones " + zones2 +
                        "  ·  Score " + score2 + "</color>";

        return "<size=58>" + headline + "</size>\n\n" +
               "<size=30>" + p1Line + "\n" + p2Line + "</size>";
    }
}
