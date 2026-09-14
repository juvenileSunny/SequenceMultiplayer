using TMPro;
using UnityEngine;

public class GameStatusUI : MonoBehaviour
{
    [Header("Network")]
    [SerializeField]
    private NetworkGameState networkGameState;

    [Header("Turn")]
    [SerializeField]
    private TMP_Text currentTurnText;

    [Header("Sequence Counts")]
    [SerializeField]
    private TMP_Text team1SequenceText;

    [SerializeField]
    private TMP_Text team2SequenceText;

    [SerializeField]
    private TMP_Text team3SequenceText;

    // =========================================================
    // NETWORK EVENTS
    // =========================================================

    private void OnEnable()
    {
        if (networkGameState != null)
        {
            networkGameState.OnPublicGameStateChanged +=
                RefreshFromNetworkState;
        }

        RefreshFromNetworkState();
    }

    private void OnDisable()
    {
        if (networkGameState != null)
        {
            networkGameState.OnPublicGameStateChanged -=
                RefreshFromNetworkState;
        }
    }

    // =========================================================
    // NETWORK STATUS
    // =========================================================

    private void RefreshFromNetworkState()
    {
        if (networkGameState == null)
            return;

        UpdateSequenceText(
            networkGameState.TeamCount,
            networkGameState.Team1Sequences,
            networkGameState.Team2Sequences,
            networkGameState.Team3Sequences,
            networkGameState.SequencesNeededToWin
        );

        if (networkGameState.WinnerTeamId > 0)
        {
            ShowWinner(
                networkGameState.WinnerTeamId
            );

            return;
        }

        if (networkGameState.CurrentPlayerId <= 0)
            return;

        UpdateTurnText(
            networkGameState.CurrentPlayerId,
            networkGameState.CurrentTeamId
        );
    }

    // =========================================================
    // LOCAL / LEGACY UPDATE
    //
    // Keep this because GameManager currently uses it too.
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
            currentPlayer.PlayerId,
            currentPlayer.TeamId
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
        int playerId,
        int teamId)
    {
        if (currentTurnText == null)
            return;

        string teamName =
            GetTeamName(
                teamId
            );

        currentTurnText.text =
            $"Player {playerId} • " +
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
        if (sequencesNeededToWin <= 0)
            return;

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
    // WINNER
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