using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkGameState : NetworkBehaviour
{
    // =========================================================
    // CURRENT TURN
    // =========================================================

    private NetworkVariable<int> currentPlayerId =
        new NetworkVariable<int>(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private NetworkVariable<int> currentSeatIndex =
        new NetworkVariable<int>(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private NetworkVariable<int> currentTeamId =
        new NetworkVariable<int>(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    // =========================================================
    // PUBLIC MATCH STATUS
    // =========================================================

    private NetworkVariable<int> teamCount =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private NetworkVariable<int> team1Sequences =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private NetworkVariable<int> team2Sequences =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private NetworkVariable<int> team3Sequences =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private NetworkVariable<int> sequencesNeededToWin =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private NetworkVariable<int> winnerTeamId =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    // =========================================================
    // EVENTS
    // =========================================================

    public event Action<int, int>
        OnCurrentPlayerChanged;

    public event Action
        OnPublicGameStateChanged;

    // =========================================================
    // PUBLIC READ-ONLY STATE
    // =========================================================

    public int CurrentPlayerId =>
        currentPlayerId.Value;

    public int CurrentSeatIndex =>
        currentSeatIndex.Value;

    public int CurrentTeamId =>
        currentTeamId.Value;

    public int TeamCount =>
        teamCount.Value;

    public int Team1Sequences =>
        team1Sequences.Value;

    public int Team2Sequences =>
        team2Sequences.Value;

    public int Team3Sequences =>
        team3Sequences.Value;

    public int SequencesNeededToWin =>
        sequencesNeededToWin.Value;

    public int WinnerTeamId =>
        winnerTeamId.Value;

    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        currentPlayerId.OnValueChanged +=
            HandleCurrentPlayerChanged;

        currentSeatIndex.OnValueChanged +=
            HandleAnyPublicStateChanged;

        currentTeamId.OnValueChanged +=
            HandleAnyPublicStateChanged;

        teamCount.OnValueChanged +=
            HandleAnyPublicStateChanged;

        team1Sequences.OnValueChanged +=
            HandleAnyPublicStateChanged;

        team2Sequences.OnValueChanged +=
            HandleAnyPublicStateChanged;

        team3Sequences.OnValueChanged +=
            HandleAnyPublicStateChanged;

        sequencesNeededToWin.OnValueChanged +=
            HandleAnyPublicStateChanged;

        winnerTeamId.OnValueChanged +=
            HandleAnyPublicStateChanged;

        Debug.Log(
            $"NetworkGameState spawned. " +
            $"Current Player = {currentPlayerId.Value}"
        );

        OnPublicGameStateChanged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        currentPlayerId.OnValueChanged -=
            HandleCurrentPlayerChanged;

        currentSeatIndex.OnValueChanged -=
            HandleAnyPublicStateChanged;

        currentTeamId.OnValueChanged -=
            HandleAnyPublicStateChanged;

        teamCount.OnValueChanged -=
            HandleAnyPublicStateChanged;

        team1Sequences.OnValueChanged -=
            HandleAnyPublicStateChanged;

        team2Sequences.OnValueChanged -=
            HandleAnyPublicStateChanged;

        team3Sequences.OnValueChanged -=
            HandleAnyPublicStateChanged;

        sequencesNeededToWin.OnValueChanged -=
            HandleAnyPublicStateChanged;

        winnerTeamId.OnValueChanged -=
            HandleAnyPublicStateChanged;

        base.OnNetworkDespawn();
    }

    // =========================================================
    // SERVER — CURRENT TURN
    // =========================================================

    public void SetCurrentTurn(
        int playerId,
        int seatIndex,
        int teamId)
    {
        if (!IsServer)
        {
            Debug.LogWarning(
                "Only the server may change the current turn."
            );

            return;
        }

        // Set supporting information FIRST.
        currentSeatIndex.Value =
            seatIndex;

        currentTeamId.Value =
            teamId;

        // Player is changed last so turn listeners
        // see the correct seat/team values.
        currentPlayerId.Value =
            playerId;

        Debug.Log(
            $"SERVER CURRENT TURN: " +
            $"Player {playerId}, " +
            $"Seat {seatIndex}, " +
            $"Team {teamId}"
        );
    }

    // =========================================================
    // SERVER — PUBLIC MATCH STATUS
    // =========================================================

    public void SetPublicMatchStatus(
        int numberOfTeams,
        int redSequences,
        int blueSequences,
        int greenSequences,
        int requiredSequences)
    {
        if (!IsServer)
        {
            Debug.LogWarning(
                "Only the server may change public match status."
            );

            return;
        }

        teamCount.Value =
            numberOfTeams;

        team1Sequences.Value =
            redSequences;

        team2Sequences.Value =
            blueSequences;

        team3Sequences.Value =
            greenSequences;

        sequencesNeededToWin.Value =
            requiredSequences;

        Debug.Log(
            $"SERVER STATUS: " +
            $"Teams={numberOfTeams}, " +
            $"Red={redSequences}, " +
            $"Blue={blueSequences}, " +
            $"Green={greenSequences}, " +
            $"Need={requiredSequences}"
        );
    }

    // =========================================================
    // SERVER — WINNER
    // =========================================================

    public void SetWinner(
        int teamId)
    {
        if (!IsServer)
            return;

        winnerTeamId.Value =
            teamId;

        Debug.Log(
            $"SERVER WINNER: Team {teamId}"
        );
    }

    // =========================================================
    // NETWORK CHANGE HANDLERS
    // =========================================================

    private void HandleCurrentPlayerChanged(
        int previousPlayerId,
        int newPlayerId)
    {
        Debug.Log(
            $"TURN synchronized: " +
            $"Player {previousPlayerId} -> " +
            $"Player {newPlayerId}"
        );

        OnCurrentPlayerChanged?.Invoke(
            previousPlayerId,
            newPlayerId
        );

        OnPublicGameStateChanged?.Invoke();
    }

    private void HandleAnyPublicStateChanged(
        int previousValue,
        int newValue)
    {
        OnPublicGameStateChanged?.Invoke();
    }
}