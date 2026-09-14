using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkHandState : NetworkBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Game")]
    [SerializeField]
    private GameManager gameManager;

    [Header("Player Mapping")]
    [SerializeField]
    private NetworkLobbyBridge networkLobbyBridge;

    // =========================================================
    // CLIENT EVENT
    //
    // Next step, HandManager will listen to this.
    // =========================================================

    public event Action<int, string[]>
        OnPrivateHandReceived;

    // =========================================================
    // SERVER SENDS ALL PRIVATE HANDS
    // =========================================================

    public void SendInitialHands()
    {
        if (!IsServer)
        {
            Debug.LogWarning(
                "Only the server may send private hands."
            );

            return;
        }

        if (gameManager == null ||
            networkLobbyBridge == null)
        {
            Debug.LogError(
                "NetworkHandState references are missing."
            );

            return;
        }

        foreach (Player player in gameManager.Players)
        {
            SendHandToPlayer(
                player.PlayerId
            );
        }
    }

    // =========================================================
    // SEND ONE PLAYER'S HAND
    // =========================================================

    public void SendHandToPlayer(
        int playerId)
    {
        if (!IsServer)
            return;

        Player player =
            gameManager.GetPlayer(
                playerId
            );

        if (player == null)
        {
            Debug.LogWarning(
                $"Cannot send hand. " +
                $"Player {playerId} does not exist."
            );

            return;
        }

        // -----------------------------------------------------
        // HOST / PLAYER 1
        //
        // Host already owns the authoritative objects locally
        // and currently displays its hand directly.
        // No private network packet is required.
        // -----------------------------------------------------

        if (!networkLobbyBridge.TryGetClientIdForPlayerId(
                playerId,
                out ulong targetClientId))
        {
            Debug.LogWarning(
                $"No network client found for Player {playerId}."
            );

            return;
        }

        if (targetClientId ==
            NetworkManager.ServerClientId)
        {
            Debug.Log(
                $"Player {playerId} is the host. " +
                "Private hand remains local."
            );

            return;
        }

        // -----------------------------------------------------
        // Convert hand to small card codes.
        //
        // Example:
        //
        // AS,5H,JD,7C,2S
        //
        // We send ONE string for now because it is simple
        // and reliable for this first transport test.
        // -----------------------------------------------------

        List<string> cardCodes =
            new List<string>();

        foreach (Card card in player.Hand)
        {
            if (card == null)
                continue;

            cardCodes.Add(
                card.GetCode()
            );
        }

        string payload =
            string.Join(
                ",",
                cardCodes
            );

        ClientRpcParams target =
            new ClientRpcParams
            {
                Send =
                    new ClientRpcSendParams
                    {
                        TargetClientIds =
                            new ulong[]
                            {
                                targetClientId
                            }
                    }
            };

        ReceivePrivateHandClientRpc(
            playerId,
            payload,
            target
        );

        Debug.Log(
            $"PRIVATE HAND sent: " +
            $"Player {playerId} -> " +
            $"ClientId {targetClientId} | " +
            $"{cardCodes.Count} cards"
        );
    }

    // =========================================================
    // PRIVATE CLIENT DELIVERY
    // =========================================================

    [ClientRpc]
    private void ReceivePrivateHandClientRpc(
        int playerId,
        string cardPayload,
        ClientRpcParams clientRpcParams = default)
    {
        // This RPC is targeted.
        // Only the intended client receives it.

        string[] cardCodes =
            string.IsNullOrWhiteSpace(
                cardPayload)
                ? Array.Empty<string>()
                : cardPayload.Split(',');

        Debug.LogWarning(
            $"PRIVATE HAND received for " +
            $"Player {playerId}: " +
            $"{string.Join(", ", cardCodes)}"
        );

        OnPrivateHandReceived?.Invoke(
            playerId,
            cardCodes
        );
    }
}