using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkLobbyBridge : NetworkBehaviour
{
    [Header("Host Authority")]
    [SerializeField]
    private LocalLobbyAuthority lobbyAuthority;

    [Header("Lobby State")]
    [SerializeField]
    private LobbyManager lobbyManager;

    [Header("Match State")]
    [SerializeField]
    private NetworkMatchState networkMatchState;

    // =========================================================
    // TEMPORARY CONNECTION -> PLAYER MAPPING
    //
    // For our first networking test:
    //
    // Host connection -> Player 1
    // First remote connection -> Player 2
    // Second remote connection -> Player 3
    // etc.
    //
    // Later we will replace this with a proper
    // player-registration/session system.
    // =========================================================

    private readonly Dictionary<ulong, int>
        clientToPlayerId =
            new Dictionary<ulong, int>();

    // =========================================================
    // REQUEST CALLBACKS
    // =========================================================

    private int nextRequestId = 1;

    private readonly Dictionary<int, Action<AuthorityResult>>
        pendingSeatRequests =
            new Dictionary<int, Action<AuthorityResult>>();

    private readonly Dictionary<int, Action<AuthorityResult>>
        pendingReadyRequests =
            new Dictionary<int, Action<AuthorityResult>>();
    private readonly Dictionary<int, Action<AuthorityResult>>
        pendingStartMatchRequests =
            new Dictionary<int, Action<AuthorityResult>>();

    [ClientRpc]
    private void StartMatchResultClientRpc(
        int requestId,
        bool success,
        int resultCode,
        string message,
        ClientRpcParams clientRpcParams = default)
    {
        if (IsServer)
            return;

        if (!pendingStartMatchRequests.TryGetValue(
                requestId,
                out Action<AuthorityResult> callback))
        {
            return;
        }

        pendingStartMatchRequests.Remove(
            requestId
        );

        AuthorityResult result =
            success
                ? AuthorityResult.Accepted(
                    message
                )
                : AuthorityResult.Rejected(
                    (AuthorityResultCode)resultCode,
                    message
                );

        callback?.Invoke(
            result
        );
    }
    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // Host/server local client becomes Player 1.
            clientToPlayerId[
                NetworkManager.ServerClientId
            ] = 1;

            NetworkManager.OnClientConnectedCallback +=
                HandleServerClientConnected;

            NetworkManager.OnClientDisconnectCallback +=
                HandleServerClientDisconnected;

            Debug.Log(
                "NetworkLobbyBridge spawned on SERVER."
            );
        }
        else
        {
            Debug.Log(
                "NetworkLobbyBridge spawned on CLIENT."
            );
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null &&
            IsServer)
        {
            NetworkManager.OnClientConnectedCallback -=
                HandleServerClientConnected;

            NetworkManager.OnClientDisconnectCallback -=
                HandleServerClientDisconnected;
        }

        base.OnNetworkDespawn();
    }

    // =========================================================
    // SERVER CONNECTION MAPPING
    // =========================================================

    private void HandleServerClientConnected(
        ulong clientId)
    {
        if (!IsServer)
            return;

        if (clientToPlayerId.ContainsKey(
                clientId))
        {
            return;
        }

        int assignedPlayerId =
            FindNextAvailablePlayerId();

        if (assignedPlayerId <= 0)
        {
            Debug.LogWarning(
                $"No available PlayerId for NGO ClientId {clientId}."
            );

            return;
        }

        clientToPlayerId[
            clientId
        ] = assignedPlayerId;

        Debug.Log(
            $"NGO ClientId {clientId} " +
            $"mapped to Player {assignedPlayerId}."
        );
    }

    private void HandleServerClientDisconnected(
        ulong clientId)
    {
        if (!IsServer)
            return;

        if (clientToPlayerId.TryGetValue(
                clientId,
                out int playerId))
        {
            Debug.Log(
                $"NGO ClientId {clientId} " +
                $"disconnected from Player {playerId}."
            );

            clientToPlayerId.Remove(
                clientId
            );
        }
    }

    private int FindNextAvailablePlayerId()
    {
        if (lobbyManager == null)
            return -1;

        for (int playerId = 2;
             playerId <= lobbyManager.PlayerCount;
             playerId++)
        {
            bool alreadyAssigned = false;

            foreach (
                KeyValuePair<ulong, int> pair
                in clientToPlayerId)
            {
                if (pair.Value == playerId)
                {
                    alreadyAssigned = true;
                    break;
                }
            }

            if (!alreadyAssigned)
                return playerId;
        }

        return -1;
    }

    private bool TryGetPlayerIdForClient(
        ulong clientId,
        out int playerId)
    {
        return clientToPlayerId.TryGetValue(
            clientId,
            out playerId
        );
    }

    // =========================================================
    // PUBLIC SEAT REQUEST
    // =========================================================

    public void RequestSeat(
        int seatIndex,
        Action<AuthorityResult> onCompleted)
    {
        if (!IsSpawned)
        {
            onCompleted?.Invoke(
                AuthorityResult.Rejected(
                    AuthorityResultCode.LobbyNotAvailable,
                    "Network lobby bridge is not ready."
                )
            );

            return;
        }

        // -----------------------------------------------------
        // HOST
        //
        // The host does not need to send a packet to itself.
        // It can call the authority directly.
        // -----------------------------------------------------

        if (IsServer)
        {
            HandleHostSeatRequest(
                seatIndex,
                onCompleted
            );

            return;
        }

        // -----------------------------------------------------
        // REMOTE CLIENT
        // -----------------------------------------------------

        int requestId =
            nextRequestId++;

        pendingSeatRequests[
            requestId
        ] = onCompleted;

        RequestSeatServerRpc(
            requestId,
            seatIndex
        );
    }

    private void HandleHostSeatRequest(
        int seatIndex,
        Action<AuthorityResult> onCompleted)
    {
        if (lobbyAuthority == null)
        {
            onCompleted?.Invoke(
                AuthorityResult.Rejected(
                    AuthorityResultCode.LobbyNotAvailable,
                    "Lobby authority is unavailable."
                )
            );

            return;
        }

        RequestSeatRequest request =
            new RequestSeatRequest(
                seatIndex
            );

        AuthorityResult result =
            lobbyAuthority.HandleSeatRequest(
                1,
                request
            );

        if (result.Success)
        {
            BroadcastLobbyState();
        }

        onCompleted?.Invoke(
            result
        );
    }

    // =========================================================
    // SEAT SERVER RPC
    // =========================================================

    [ServerRpc(RequireOwnership = false)]
    private void RequestSeatServerRpc(
        int requestId,
        int seatIndex,
        ServerRpcParams rpcParams = default)
    {
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        if (!TryGetPlayerIdForClient(
                senderClientId,
                out int playerId))
        {
            SendSeatResult(
                senderClientId,
                requestId,
                AuthorityResult.Rejected(
                    AuthorityResultCode.InvalidPlayer,
                    "No player is assigned to this connection."
                )
            );

            return;
        }

        RequestSeatRequest request =
            new RequestSeatRequest(
                seatIndex
            );

        AuthorityResult result =
            lobbyAuthority.HandleSeatRequest(
                playerId,
                request
            );

        Debug.Log(
            $"NETWORK seat request: " +
            $"ClientId={senderClientId}, " +
            $"Player={playerId}, " +
            $"Seat={seatIndex}, " +
            $"Accepted={result.Success}"
        );

        if (result.Success)
        {
            BroadcastLobbyState();
        }

        SendSeatResult(
            senderClientId,
            requestId,
            result
        );
    }

    // =========================================================
    // SEAT RESULT
    // =========================================================

    private void SendSeatResult(
        ulong targetClientId,
        int requestId,
        AuthorityResult result)
    {
        ClientRpcParams target =
            CreateTargetClientRpcParams(
                targetClientId
            );

        SeatResultClientRpc(
            requestId,
            result.Success,
            (int)result.Code,
            result.Message,
            target
        );
    }

    [ClientRpc]
    private void SeatResultClientRpc(
        int requestId,
        bool success,
        int resultCode,
        string message,
        ClientRpcParams clientRpcParams = default)
    {
        if (IsServer)
            return;

        if (!pendingSeatRequests.TryGetValue(
                requestId,
                out Action<AuthorityResult> callback))
        {
            return;
        }

        pendingSeatRequests.Remove(
            requestId
        );

        AuthorityResult result =
            success
                ? AuthorityResult.Accepted(
                    message
                )
                : AuthorityResult.Rejected(
                    (AuthorityResultCode)resultCode,
                    message
                );

        callback?.Invoke(
            result
        );
    }

    // =========================================================
    // PUBLIC READY REQUEST
    // =========================================================

    public void RequestReadyState(
        bool isReady,
        Action<AuthorityResult> onCompleted)
    {
        if (!IsSpawned)
        {
            onCompleted?.Invoke(
                AuthorityResult.Rejected(
                    AuthorityResultCode.LobbyNotAvailable,
                    "Network lobby bridge is not ready."
                )
            );

            return;
        }

        if (IsServer)
        {
            HandleHostReadyRequest(
                isReady,
                onCompleted
            );

            return;
        }

        int requestId =
            nextRequestId++;

        pendingReadyRequests[
            requestId
        ] = onCompleted;

        RequestReadyServerRpc(
            requestId,
            isReady
        );
    }

    private void HandleHostReadyRequest(
        bool isReady,
        Action<AuthorityResult> onCompleted)
    {
        if (lobbyAuthority == null)
        {
            onCompleted?.Invoke(
                AuthorityResult.Rejected(
                    AuthorityResultCode.LobbyNotAvailable,
                    "Lobby authority is unavailable."
                )
            );

            return;
        }

        SetReadyRequest request =
            new SetReadyRequest(
                isReady
            );

        AuthorityResult result =
            lobbyAuthority.HandleReadyRequest(
                1,
                request
            );

        if (result.Success)
        {
            BroadcastLobbyState();
        }

        onCompleted?.Invoke(
            result
        );
    }

    // =========================================================
    // READY SERVER RPC
    // =========================================================

    [ServerRpc(RequireOwnership = false)]
    private void RequestReadyServerRpc(
        int requestId,
        bool isReady,
        ServerRpcParams rpcParams = default)
    {
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        if (!TryGetPlayerIdForClient(
                senderClientId,
                out int playerId))
        {
            SendReadyResult(
                senderClientId,
                requestId,
                AuthorityResult.Rejected(
                    AuthorityResultCode.InvalidPlayer,
                    "No player is assigned to this connection."
                )
            );

            return;
        }

        SetReadyRequest request =
            new SetReadyRequest(
                isReady
            );

        AuthorityResult result =
            lobbyAuthority.HandleReadyRequest(
                playerId,
                request
            );

        Debug.Log(
            $"NETWORK ready request: " +
            $"ClientId={senderClientId}, " +
            $"Player={playerId}, " +
            $"Ready={isReady}, " +
            $"Accepted={result.Success}"
        );

        if (result.Success)
        {
            BroadcastLobbyState();
        }

        SendReadyResult(
            senderClientId,
            requestId,
            result
        );
    }

    // =========================================================
    // READY RESULT
    // =========================================================

    private void SendReadyResult(
        ulong targetClientId,
        int requestId,
        AuthorityResult result)
    {
        ClientRpcParams target =
            CreateTargetClientRpcParams(
                targetClientId
            );

        ReadyResultClientRpc(
            requestId,
            result.Success,
            (int)result.Code,
            result.Message,
            target
        );
    }

    [ClientRpc]
    private void ReadyResultClientRpc(
        int requestId,
        bool success,
        int resultCode,
        string message,
        ClientRpcParams clientRpcParams = default)
    {
        if (IsServer)
            return;

        if (!pendingReadyRequests.TryGetValue(
                requestId,
                out Action<AuthorityResult> callback))
        {
            return;
        }

        pendingReadyRequests.Remove(
            requestId
        );

        AuthorityResult result =
            success
                ? AuthorityResult.Accepted(
                    message
                )
                : AuthorityResult.Rejected(
                    (AuthorityResultCode)resultCode,
                    message
                );

        callback?.Invoke(
            result
        );
    }

    // =========================================================
    // SYNCHRONIZE PUBLIC LOBBY STATE
    // =========================================================

    private void BroadcastLobbyState()
    {
        if (!IsServer ||
            lobbyManager == null)
        {
            return;
        }

        for (int playerId = 1;
             playerId <= lobbyManager.PlayerCount;
             playerId++)
        {
            LobbyPlayerData player =
                lobbyManager.GetPlayer(
                    playerId
                );

            if (player == null)
                continue;

            SyncPlayerStateClientRpc(
                player.PlayerId,
                player.SeatIndex,
                player.TeamId,
                player.IsReady
            );
        }
    }

    [ClientRpc]
    private void SyncPlayerStateClientRpc(
        int playerId,
        int seatIndex,
        int teamId,
        bool isReady)
    {
        // Host already owns the authoritative state.
        // Do not write it back into itself.
        if (IsServer)
            return;

        if (lobbyManager == null)
            return;

        LobbyPlayerData player =
            lobbyManager.GetPlayer(
                playerId
            );

        if (player == null)
            return;

        // -----------------------------------------------------
        // Synchronize seat.
        // Team is derived from the seat by LobbyManager.
        // -----------------------------------------------------

        if (seatIndex > 0)
        {
            lobbyManager.TryAssignSeat(
                playerId,
                seatIndex
            );
        }
        else
        {
            lobbyManager.ClearPlayerSeat(
                playerId
            );
        }

        // -----------------------------------------------------
        // Synchronize ready state.
        // -----------------------------------------------------

        lobbyManager.SetPlayerReady(
            playerId,
            isReady
        );

        Debug.Log(
            $"SYNC lobby state: " +
            $"Player={playerId}, " +
            $"Seat={seatIndex}, " +
            $"Team={teamId}, " +
            $"Ready={isReady}"
        );
    }

    // =========================================================
    // TARGETED CLIENT RPC
    // =========================================================

    private ClientRpcParams CreateTargetClientRpcParams(
        ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds =
                    new ulong[]
                    {
                        clientId
                    }
            }
        };
    }


// =========================================================
// PUBLIC START MATCH REQUEST
// =========================================================

public void RequestStartMatch(
    Action<AuthorityResult> onCompleted)
{
    if (!IsSpawned)
    {
        onCompleted?.Invoke(
            AuthorityResult.Rejected(
                AuthorityResultCode.LobbyNotAvailable,
                "Network lobby bridge is not ready."
            )
        );

        return;
    }

    // =====================================================
    // HOST
    //
    // Host is also the server, so we do not need
    // to send a packet to ourselves.
    // =====================================================

    if (IsServer)
    {
        HandleHostStartMatchRequest(
            onCompleted
        );

        return;
    }

    // =====================================================
    // REMOTE CLIENT
    //
    // Normally a remote client should never have
    // permission to start the match.
    //
    // We still send the request to the SERVER so
    // SERVER AUTHORITY makes that decision.
    // =====================================================

    int requestId =
        nextRequestId++;

    pendingStartMatchRequests[
        requestId
    ] = onCompleted;

    RequestStartMatchServerRpc(
        requestId
    );
}

// =========================================================
// HOST START MATCH
// =========================================================

private void HandleHostStartMatchRequest(
    Action<AuthorityResult> onCompleted)
{
    if (lobbyAuthority == null)
    {
        onCompleted?.Invoke(
            AuthorityResult.Rejected(
                AuthorityResultCode.LobbyNotAvailable,
                "Lobby authority is unavailable."
            )
        );

        return;
    }

    ulong hostClientId =
        NetworkManager.LocalClientId;

    if (!TryGetPlayerIdForClient(
            hostClientId,
            out int playerId))
    {
        onCompleted?.Invoke(
            AuthorityResult.Rejected(
                AuthorityResultCode.InvalidPlayer,
                "Host connection has no assigned player."
            )
        );

        return;
    }

    StartMatchRequest request =
        new StartMatchRequest();

    AuthorityResult result =
        lobbyAuthority.HandleStartMatchRequest(
            playerId,
            request
        );
    if (result.Success)
    {
        if (networkMatchState != null)
        {
            networkMatchState.SetPhase(
                MatchPhase.Playing
            );
        }
    }
    Debug.Log(
        $"NETWORK start-match request: " +
        $"ClientId={hostClientId}, " +
        $"Player={playerId}, " +
        $"Accepted={result.Success}"
    );

    onCompleted?.Invoke(
        result
    );
}

// =========================================================
// START MATCH SERVER RPC
// =========================================================

[ServerRpc(RequireOwnership = false)]
private void RequestStartMatchServerRpc(
    int requestId,
    ServerRpcParams rpcParams = default)
{
    ulong senderClientId =
        rpcParams.Receive.SenderClientId;

    if (!TryGetPlayerIdForClient(
            senderClientId,
            out int playerId))
    {
        SendStartMatchResult(
            senderClientId,
            requestId,
            AuthorityResult.Rejected(
                AuthorityResultCode.InvalidPlayer,
                "No player is assigned to this connection."
            )
        );

        return;
    }

    StartMatchRequest request =
        new StartMatchRequest();

    AuthorityResult result =
        lobbyAuthority.HandleStartMatchRequest(
            playerId,
            request
        );

    Debug.Log(
        $"NETWORK start-match request: " +
        $"ClientId={senderClientId}, " +
        $"Player={playerId}, " +
        $"Accepted={result.Success}"
    );

    SendStartMatchResult(
        senderClientId,
        requestId,
        result
    );
}

// =========================================================
// START MATCH RESULT
// =========================================================

private void SendStartMatchResult(
    ulong targetClientId,
    int requestId,
    AuthorityResult result)
{
    ClientRpcParams target =
        CreateTargetClientRpcParams(
            targetClientId
        );

    StartMatchResultClientRpc(
        requestId,
        result.Success,
        (int)result.Code,
        result.Message,
        target
    );
}

}