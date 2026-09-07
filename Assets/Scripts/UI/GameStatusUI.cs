using TMPro;
using UnityEngine;

public class GameStatusUI : MonoBehaviour
{
    [Header("Turn")]
    [SerializeField] private TMP_Text currentTurnText;

    [Header("Sequence Counts")]
    [SerializeField] private TMP_Text team1SequenceText;
    [SerializeField] private TMP_Text team2SequenceText;
    [SerializeField] private TMP_Text team3SequenceText;

    // =========================================================
    // MAIN UPDATE
    // =========================================================

    public void UpdateStatus(
        Player currentPlayer,
        int teamCount,
        int team1Sequences,
        int team2Sequences,
        int team3Sequences,
        int sequencesNeededToWin)
    {
        if (currentPlayer == null)
            return;

        UpdateTurnText(
            currentPlayer
        );

        UpdateSequenceText(
            teamCount,
            team1Sequences,
            team2Sequences,
            team3Sequences,
            sequencesNeededToWin
        );
    }

    // =========================================================
    // TURN DISPLAY
    // =========================================================

    private void UpdateTurnText(
        Player player)
    {
        if (currentTurnText == null)
            return;

        string teamName =
            GetTeamName(
                player.TeamId
            );

        currentTurnText.text =
            $"Player {player.PlayerId} • " +
            $"{teamName} • Turn";
    }

    // =========================================================
    // SEQUENCE DISPLAY
    // =========================================================

    private void UpdateSequenceText(
        int teamCount,
        int team1Sequences,
        int team2Sequences,
        int team3Sequences,
        int sequencesNeededToWin)
    {
        if (team1SequenceText != null)
        {
            team1SequenceText.text =
                $"Red: {team1Sequences} / " +
                $"{sequencesNeededToWin} Sequences";
        }

        if (team2SequenceText != null)
        {
            team2SequenceText.text =
                $"Blue: {team2Sequences} / " +
                $"{sequencesNeededToWin} Sequences";
        }

        if (team3SequenceText != null)
        {
            bool showTeam3 =
                teamCount == 3;

            team3SequenceText.gameObject.SetActive(
                showTeam3
            );

            if (showTeam3)
            {
                team3SequenceText.text =
                    $"Green: {team3Sequences} / " +
                    $"{sequencesNeededToWin} Sequence" +
                    $"{(sequencesNeededToWin == 1 ? "" : "s")}";
            }
        }
    }

    // =========================================================
    // TEAM NAME
    // =========================================================

    private string GetTeamName(
        int teamId)
    {
        switch (teamId)
        {
            case 1:
                return "Team Red";

            case 2:
                return "Team Blue";

            case 3:
                return "Team Green";

            default:
                return $"Team {teamId}";
        }
    }

    // =========================================================
    // GAME OVER DISPLAY
    // =========================================================

    public void ShowWinner(
        int winnerTeamId)
    {
        if (currentTurnText == null)
            return;

        string teamName =
            GetTeamName(
                winnerTeamId
            );

        currentTurnText.text =
            $"{teamName} WINS!";
    }
}