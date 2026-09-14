using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkGameState : NetworkBehaviour
{
    // =========================================================
    // CURRENT TURN
    //
    // Everyone can read.
    // Only the server can write.
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
    // EVENTS
    // =========================================================

    public event Action<int, int>
        OnCurrentPlayerChanged;

    // =========================================================
    // PUBLIC READ-ONLY STATE
    // =========================================================

    public int CurrentPlayerId =>
        currentPlayerId.Value;

    public int CurrentSeatIndex =>
        currentSeatIndex.Value;

    public int CurrentTeamId =>
        currentTeamId.Value;

    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        currentPlayerId.OnValueChanged +=
            HandleCurrentPlayerChanged;

        Debug.Log(
            $"NetworkGameState spawned. " +
            $"Current Player = {currentPlayerId.Value}"
        );
    }

    public override void OnNetworkDespawn()
    {
        currentPlayerId.OnValueChanged -=
            HandleCurrentPlayerChanged;

        base.OnNetworkDespawn();
    }

    // =========================================================
    // SERVER ONLY — SET CURRENT TURN
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

        int previousPlayerId =
            currentPlayerId.Value;

        currentPlayerId.Value =
            playerId;

        currentSeatIndex.Value =
            seatIndex;

        currentTeamId.Value =
            teamId;

        Debug.Log(
            $"SERVER CURRENT TURN: " +
            $"Player {playerId}, " +
            $"Seat {seatIndex}, " +
            $"Team {teamId}"
        );

        // OnValueChanged handles the normal network notification.
        // This log simply helps us debug the authoritative server.
    }

    // =========================================================
    // NETWORK CHANGE
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
    }
}