using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkMatchState : NetworkBehaviour
{
    // =========================================================
    // NETWORKED MATCH PHASE
    //
    // Everyone may READ.
    // Only the SERVER may WRITE.
    // =========================================================

    private NetworkVariable<MatchPhase> matchPhase =
        new NetworkVariable<MatchPhase>(
            MatchPhase.Lobby,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    // =========================================================
    // EVENT FOR LOCAL UI
    // =========================================================

    public event Action<
        MatchPhase,
        MatchPhase
    > OnMatchPhaseChanged;

    public MatchPhase CurrentPhase =>
        matchPhase.Value;

    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        matchPhase.OnValueChanged +=
            HandlePhaseChanged;

        Debug.Log(
            $"Network match state spawned. " +
            $"Current Phase = {matchPhase.Value}"
        );
    }

    public override void OnNetworkDespawn()
    {
        matchPhase.OnValueChanged -=
            HandlePhaseChanged;

        base.OnNetworkDespawn();
    }

    // =========================================================
    // SERVER ONLY
    // =========================================================

    public void SetPhase(
        MatchPhase newPhase)
    {
        if (!IsServer)
        {
            Debug.LogWarning(
                "Only the server may change MatchPhase."
            );

            return;
        }

        if (matchPhase.Value == newPhase)
            return;

        Debug.Log(
            $"SERVER changing MatchPhase: " +
            $"{matchPhase.Value} -> {newPhase}"
        );

        matchPhase.Value =
            newPhase;
    }

    // =========================================================
    // LOCAL NOTIFICATION
    // =========================================================

    private void HandlePhaseChanged(
        MatchPhase previousPhase,
        MatchPhase newPhase)
    {
        Debug.Log(
            $"MatchPhase synchronized: " +
            $"{previousPhase} -> {newPhase}"
        );

        OnMatchPhaseChanged?.Invoke(
            previousPhase,
            newPhase
        );
    }
}