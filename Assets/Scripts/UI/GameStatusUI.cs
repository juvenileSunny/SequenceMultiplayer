using TMPro;
using UnityEngine;

public class GameStatusUI : MonoBehaviour
{
    [Header("Network")]
    [SerializeField]
    private NetworkGameState networkGameState;

    [SerializeField]
    private NetworkLobbyBridge networkLobbyBridge;

    [SerializeField]
    private RoomSessionContext roomSessionContext;

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

        if (networkLobbyBridge != null)
        {
            networkLobbyBridge.OnPublicPlayerStateChanged +=
                RefreshFromNetworkState;
        }

        if (roomSessionContext != null)
        {
            roomSessionContext.OnSessionChanged +=
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

        if (networkLobbyBridge != null)
        {
            networkLobbyBridge.OnPublicPlayerStateChanged -=
                RefreshFromNetworkState;
        }

        if (roomSessionContext != null)
        {
            roomSessionContext.OnSessionChanged -=
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

        string playerName =
            networkLobbyBridge != null
                ? networkLobbyBridge.GetPlayerDisplayName(
                    playerId
                )
                : $"Player {playerId}";

        // =====================================================
        // CURRENT PLAYER DISCONNECTED
        // =====================================================

        bool isConnected =
            networkLobbyBridge == null ||
            networkLobbyBridge.IsPlayerConnected(
                playerId
            );

        if (!isConnected)
        {
            currentTurnText.text =
                $"Waiting for {playerName} to reconnect...";

            return;
        }

        // =====================================================
        // LOCAL PLAYER'S TURN
        // =====================================================

        bool isLocalPlayer =
            roomSessionContext != null &&
            roomSessionContext.HasLocalPlayer &&
            roomSessionContext.LocalPlayerId ==
                playerId;

        if (isLocalPlayer)
        {
            currentTurnText.text =
                $"YOUR TURN • {teamName}";

            return;
        }

        // =====================================================
        // OTHER PLAYER'S TURN
        // =====================================================

        currentTurnText.text =
            $"{playerName}'s Turn • {teamName}";
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
