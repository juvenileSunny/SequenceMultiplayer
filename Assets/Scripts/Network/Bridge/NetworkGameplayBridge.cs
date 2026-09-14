using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkGameplayBridge : NetworkBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Authority")]
    [SerializeField]
    private GameManager gameManager;

    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private NetworkLobbyBridge networkLobbyBridge;

    // =========================================================
    // CLIENT REQUEST CALLBACKS
    // =========================================================

    private int nextPlayRequestId = 1;

    private readonly Dictionary<
        int,
        Action<bool, string>
    > pendingPlayRequests =
        new Dictionary<
            int,
            Action<bool, string>
        >();

    private int nextDeadCardRequestId = 1;

    private readonly Dictionary<
        int,
        Action<bool, string>
    > pendingDeadCardRequests =
        new Dictionary<
            int,
            Action<bool, string>
        >();

    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer &&
            boardManager != null)
        {
            boardManager.OnBoardCellChanged +=
                HandleServerBoardCellChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer &&
            boardManager != null)
        {
            boardManager.OnBoardCellChanged -=
                HandleServerBoardCellChanged;
        }

        base.OnNetworkDespawn();
    }

    // =========================================================
    // CLIENT -> REQUEST NORMAL BOARD PLAY
    // =========================================================

    public void RequestPlayCard(
        string cardCode,
        int row,
        int column,
        Action<bool, string> onCompleted)
    {
        if (!IsSpawned)
        {
            onCompleted?.Invoke(
                false,
                "Gameplay network bridge is not ready."
            );

            return;
        }

        if (IsServer)
        {
            onCompleted?.Invoke(
                false,
                "Host does not need a remote play request."
            );

            return;
        }

        int requestId =
            nextPlayRequestId++;

        pendingPlayRequests[
            requestId
        ] = onCompleted;

        RequestPlayCardServerRpc(
            requestId,
            cardCode,
            row,
            column
        );
    }

    // =========================================================
    // PLAY SERVER RPC
    // =========================================================

    [ServerRpc(RequireOwnership = false)]
    private void RequestPlayCardServerRpc(
        int requestId,
        string cardCode,
        int row,
        int column,
        ServerRpcParams rpcParams = default)
    {
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        if (networkLobbyBridge == null ||
            !networkLobbyBridge.TryGetPlayerIdForClient(
                senderClientId,
                out int playerId))
        {
            SendPlayResult(
                senderClientId,
                requestId,
                false,
                "No player is assigned to this connection."
            );

            return;
        }

        if (gameManager == null)
        {
            SendPlayResult(
                senderClientId,
                requestId,
                false,
                "Server GameManager is unavailable."
            );

            return;
        }

        bool success =
            gameManager.TryHandleNetworkPlayRequest(
                playerId,
                cardCode,
                row,
                column,
                out string error
            );

        Debug.Log(
            $"NETWORK PLAY: " +
            $"ClientId={senderClientId}, " +
            $"Player={playerId}, " +
            $"Card={cardCode}, " +
            $"Cell=[{row},{column}], " +
            $"Accepted={success}"
        );

        SendPlayResult(
            senderClientId,
            requestId,
            success,
            success
                ? "Move accepted."
                : error
        );
    }

    // =========================================================
    // PLAY RESULT
    // =========================================================

    private void SendPlayResult(
        ulong targetClientId,
        int requestId,
        bool success,
        string message)
    {
        ClientRpcParams target =
            CreateTargetClientRpcParams(
                targetClientId
            );

        PlayResultClientRpc(
            requestId,
            success,
            message,
            target
        );
    }

    [ClientRpc]
    private void PlayResultClientRpc(
        int requestId,
        bool success,
        string message,
        ClientRpcParams clientRpcParams = default)
    {
        if (IsServer)
            return;

        if (!pendingPlayRequests.TryGetValue(
                requestId,
                out Action<bool, string> callback))
        {
            return;
        }

        pendingPlayRequests.Remove(
            requestId
        );

        callback?.Invoke(
            success,
            message
        );
    }

    // =========================================================
    // CLIENT -> REQUEST DEAD CARD REPLACEMENT
    // =========================================================

    public void RequestDeadCardReplacement(
        string cardCode,
        Action<bool, string> onCompleted)
    {
        if (!IsSpawned)
        {
            onCompleted?.Invoke(
                false,
                "Gameplay network bridge is not ready."
            );

            return;
        }

        if (IsServer)
        {
            onCompleted?.Invoke(
                false,
                "Host does not need a remote dead-card request."
            );

            return;
        }

        int requestId =
            nextDeadCardRequestId++;

        pendingDeadCardRequests[
            requestId
        ] = onCompleted;

        RequestDeadCardReplacementServerRpc(
            requestId,
            cardCode
        );
    }

    // =========================================================
    // DEAD CARD SERVER RPC
    // =========================================================

    [ServerRpc(RequireOwnership = false)]
    private void RequestDeadCardReplacementServerRpc(
        int requestId,
        string cardCode,
        ServerRpcParams rpcParams = default)
    {
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        if (networkLobbyBridge == null ||
            !networkLobbyBridge.TryGetPlayerIdForClient(
                senderClientId,
                out int playerId))
        {
            SendDeadCardResult(
                senderClientId,
                requestId,
                false,
                "No player is assigned to this connection."
            );

            return;
        }

        if (gameManager == null)
        {
            SendDeadCardResult(
                senderClientId,
                requestId,
                false,
                "Server GameManager is unavailable."
            );

            return;
        }

        bool success =
            gameManager.TryHandleNetworkDeadCardReplacement(
                playerId,
                cardCode,
                out string error
            );

        Debug.Log(
            $"NETWORK DEAD CARD: " +
            $"ClientId={senderClientId}, " +
            $"Player={playerId}, " +
            $"Card={cardCode}, " +
            $"Accepted={success}"
        );

        SendDeadCardResult(
            senderClientId,
            requestId,
            success,
            success
                ? "Dead card replaced."
                : error
        );
    }

    // =========================================================
    // DEAD CARD RESULT
    // =========================================================

    private void SendDeadCardResult(
        ulong targetClientId,
        int requestId,
        bool success,
        string message)
    {
        ClientRpcParams target =
            CreateTargetClientRpcParams(
                targetClientId
            );

        DeadCardResultClientRpc(
            requestId,
            success,
            message,
            target
        );
    }

    [ClientRpc]
    private void DeadCardResultClientRpc(
        int requestId,
        bool success,
        string message,
        ClientRpcParams clientRpcParams = default)
    {
        if (IsServer)
            return;

        if (!pendingDeadCardRequests.TryGetValue(
                requestId,
                out Action<bool, string> callback))
        {
            return;
        }

        pendingDeadCardRequests.Remove(
            requestId
        );

        callback?.Invoke(
            success,
            message
        );
    }

    // =========================================================
    // SERVER BOARD CHANGE
    //
    // Normal connected clients receive authoritative deltas
    // as moves happen.
    // =========================================================

    private void HandleServerBoardCellChanged(
        BoardCell cell)
    {
        if (!IsServer ||
            cell == null)
        {
            return;
        }

        SyncBoardCellClientRpc(
            cell.Row,
            cell.Column,
            cell.OwnerTeamId,
            cell.IsPartOfCompletedSequence
        );
    }

    // =========================================================
    // SERVER -> CLIENT BOARD DELTA
    // =========================================================

    [ClientRpc]
    private void SyncBoardCellClientRpc(
        int row,
        int column,
        int ownerTeamId,
        bool isPartOfCompletedSequence)
    {
        if (IsServer)
            return;

        if (boardManager == null)
            return;

        boardManager.ApplyNetworkCellState(
            row,
            column,
            ownerTeamId,
            isPartOfCompletedSequence
        );

        Debug.Log(
            $"BOARD SYNC: " +
            $"[{row},{column}] -> " +
            $"Team {ownerTeamId}"
        );
    }

    // =========================================================
    // FULL BOARD SNAPSHOT FOR REJOIN
    //
    // A reconnecting player may have missed many deltas while
    // disconnected, so the server sends all 100 board cells.
    // =========================================================

    public void SendFullBoardSnapshotToPlayer(
        int playerId)
    {
        if (!IsServer)
        {
            Debug.LogWarning(
                "Only the server may send a board snapshot."
            );

            return;
        }

        if (boardManager == null ||
            boardManager.Board == null)
        {
            Debug.LogWarning(
                "Cannot send board snapshot. " +
                "BoardManager/Board is unavailable."
            );

            return;
        }

        if (networkLobbyBridge == null)
        {
            Debug.LogWarning(
                "Cannot send board snapshot. " +
                "NetworkLobbyBridge is unavailable."
            );

            return;
        }

        if (!networkLobbyBridge.TryGetClientIdForPlayerId(
                playerId,
                out ulong targetClientId))
        {
            Debug.LogWarning(
                $"Cannot send board snapshot. " +
                $"Player {playerId} is not currently connected."
            );

            return;
        }

        int cellCount =
            Board.Rows *
            Board.Columns;

        int[] ownerTeamIds =
            new int[
                cellCount
            ];

        bool[] completedSequenceFlags =
            new bool[
                cellCount
            ];

        int index = 0;

        for (int row = 0;
             row < Board.Rows;
             row++)
        {
            for (int column = 0;
                 column < Board.Columns;
                 column++)
            {
                BoardCell cell =
                    boardManager.Board.GetCell(
                        row,
                        column
                    );

                if (cell != null)
                {
                    ownerTeamIds[index] =
                        cell.OwnerTeamId;

                    completedSequenceFlags[index] =
                        cell.IsPartOfCompletedSequence;
                }

                index++;
            }
        }

        ClientRpcParams target =
            CreateTargetClientRpcParams(
                targetClientId
            );

        FullBoardSnapshotClientRpc(
            ownerTeamIds,
            completedSequenceFlags,
            target
        );

        Debug.LogWarning(
            $"FULL BOARD SNAPSHOT sent to " +
            $"Player {playerId} / ClientId {targetClientId}."
        );
    }

    [ClientRpc]
    private void FullBoardSnapshotClientRpc(
        int[] ownerTeamIds,
        bool[] completedSequenceFlags,
        ClientRpcParams clientRpcParams = default)
    {
        if (IsServer)
            return;

        if (boardManager == null)
            return;

        int expectedCellCount =
            Board.Rows *
            Board.Columns;

        if (ownerTeamIds == null ||
            completedSequenceFlags == null ||
            ownerTeamIds.Length !=
                expectedCellCount ||
            completedSequenceFlags.Length !=
                expectedCellCount)
        {
            Debug.LogWarning(
                "Received invalid full board snapshot."
            );

            return;
        }

        int index = 0;

        for (int row = 0;
             row < Board.Rows;
             row++)
        {
            for (int column = 0;
                 column < Board.Columns;
                 column++)
            {
                BoardCell localCell =
                    boardManager.Board != null
                        ? boardManager.Board.GetCell(
                            row,
                            column
                        )
                        : null;

                // Corners are permanent free spaces and do not
                // need owner/protection state written onto them.
                if (localCell != null &&
                    !localCell.IsCorner)
                {
                    boardManager.ApplyNetworkCellState(
                        row,
                        column,
                        ownerTeamIds[index],
                        completedSequenceFlags[index]
                    );
                }

                index++;
            }
        }

        Debug.LogWarning(
            "FULL BOARD SNAPSHOT applied on client."
        );
    }

    // =========================================================
    // RPC HELPER
    // =========================================================

    private ClientRpcParams CreateTargetClientRpcParams(
        ulong clientId)
    {
        return new ClientRpcParams
        {
            Send =
                new ClientRpcSendParams
                {
                    TargetClientIds =
                        new ulong[]
                        {
                            clientId
                        }
                }
        };
    }
}
