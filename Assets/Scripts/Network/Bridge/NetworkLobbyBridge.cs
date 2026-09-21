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

    [Header("Session / Rejoin")]
    [SerializeField]
    private RoomSessionContext roomSessionContext;

    [SerializeField]
    private NetworkHandState networkHandState;

    [SerializeField]
    private NetworkGameplayBridge networkGameplayBridge;

    private bool matchStarted = false;

    // Public player profile/presence changed.
    // Game HUD and other public UI can refresh from this.
    public event Action OnPublicPlayerStateChanged;

    // Public lobby configuration changed.
    // Any player may REQUEST a change while still in the lobby,
    // but the server is the source of truth and broadcasts the
    // accepted player/team counts to everybody.
    public event Action<int, int> OnLobbyConfigurationChanged;

    private int publicLobbyPlayerCount = -1;
    private int publicLobbyTeamCount = -1;

    public int LobbyPlayerCount
    {
        get
        {
            if (publicLobbyPlayerCount > 0)
                return publicLobbyPlayerCount;

            return lobbyManager != null
                ? lobbyManager.PlayerCount
                : 2;
        }
    }

    public int LobbyTeamCount
    {
        get
        {
            if (publicLobbyTeamCount > 0)
                return publicLobbyTeamCount;

            return lobbyManager != null
                ? lobbyManager.TeamCount
                : 2;
        }
    }

    // PlayerId -> public display name.
    private readonly Dictionary<int, string>
        playerDisplayNames =
            new Dictionary<int, string>();

    // PlayerId -> connection presence.
    private readonly Dictionary<int, bool>
        playerConnectedStates =
            new Dictionary<int, bool>();
    private readonly Dictionary<int, int>
        playerSeatIndexes =
            new Dictionary<int, int>();

    private readonly Dictionary<int, int>
        playerTeamIds =
            new Dictionary<int, int>();

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

    // Rejoin token -> permanent PlayerId for the lifetime
    // of this host/server session.
    private readonly Dictionary<string, int>
        rejoinTokenToPlayerId =
            new Dictionary<string, int>(
                StringComparer.Ordinal
            );

    private readonly Dictionary<int, string>
        playerIdToRejoinToken =
            new Dictionary<int, string>();

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
        pendingLobbyConfigurationRequests =
            new Dictionary<int, Action<AuthorityResult>>();

    private readonly Dictionary<int, Action<AuthorityResult>>
        pendingStartMatchRequests =
            new Dictionary<int, Action<AuthorityResult>>();

    private int nextSessionRegistrationRequestId = 1;

    private readonly Dictionary<
        int,
        Action<bool, int, bool, bool, string>
    > pendingSessionRegistrationRequests =
        new Dictionary<
            int,
            Action<bool, int, bool, bool, string>
        >();

    private int nextStateSyncRequestId = 1;

    private readonly Dictionary<
        int,
        Action<bool, string>
    > pendingStateSyncRequests =
        new Dictionary<
            int,
            Action<bool, string>
        >();

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

            playerConnectedStates[1] =
                true;

            if (!playerDisplayNames.ContainsKey(1))
            {
                playerDisplayNames[1] =
                    "Player 1";
            }

            if (lobbyManager != null)
            {
                // Ensure the authoritative server lobby already
                // contains the configured player rows.
                lobbyManager.ConfigureLobby(
                    lobbyManager.PlayerCount,
                    lobbyManager.TeamCount
                );

                publicLobbyPlayerCount =
                    lobbyManager.PlayerCount;

                publicLobbyTeamCount =
                    lobbyManager.TeamCount;
            }

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

        // Host/server local client is already Player 1.
        if (clientId ==
            NetworkManager.ServerClientId)
        {
            return;
        }

        // Remote clients are NOT assigned a PlayerId merely
        // because a transport connection exists.
        //
        // They must prove identity with their persistent
        // rejoin token first.
        Debug.Log(
            $"NGO ClientId {clientId} connected. " +
            "Awaiting session registration."
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
            clientToPlayerId.Remove(
                clientId
            );

            bool hasReservedIdentity =
                playerIdToRejoinToken.ContainsKey(
                    playerId
                );

            playerConnectedStates[playerId] =
                false;

            Debug.LogWarning(
                $"NGO ClientId {clientId} disconnected " +
                $"from Player {playerId}. " +
                $"ReservedForRejoin={hasReservedIdentity}"
            );

            // IMPORTANT:
            // We intentionally do NOT remove:
            //
            // rejoinTokenToPlayerId
            // playerIdToRejoinToken
            //
            // and GameManager keeps the authoritative
            // Player object / hand / board / turn state.

            BroadcastLobbyState();

            OnPublicPlayerStateChanged?.Invoke();
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
            bool reserved =
                playerIdToRejoinToken.ContainsKey(
                    playerId
                );

            if (reserved)
                continue;

            bool currentlyConnected = false;

            foreach (
                KeyValuePair<ulong, int> pair
                in clientToPlayerId)
            {
                if (pair.Value ==
                    playerId)
                {
                    currentlyConnected =
                        true;

                    break;
                }
            }

            if (!currentlyConnected)
                return playerId;
        }

        return -1;
    }

    public bool TryGetPlayerIdForClient(
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
    // PUBLIC LOBBY CONFIGURATION REQUEST
    //
    // Any registered player may request a different player/team
    // count while the match is still in the lobby.
    //
    // SERVER:
    // validates -> applies -> broadcasts
    //
    // CLIENTS:
    // display the server-approved values
    // =========================================================

    public void RequestLobbyConfiguration(
        int requestedPlayerCount,
        int requestedTeamCount,
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
            AuthorityResult result =
                ApplyLobbyConfigurationOnServer(
                    1,
                    requestedPlayerCount,
                    requestedTeamCount
                );

            onCompleted?.Invoke(
                result
            );

            return;
        }

        int requestId =
            nextRequestId++;

        pendingLobbyConfigurationRequests[
            requestId
        ] = onCompleted;

        RequestLobbyConfigurationServerRpc(
            requestId,
            requestedPlayerCount,
            requestedTeamCount
        );
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestLobbyConfigurationServerRpc(
        int requestId,
        int requestedPlayerCount,
        int requestedTeamCount,
        ServerRpcParams rpcParams = default)
    {
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        if (!TryGetPlayerIdForClient(
                senderClientId,
                out int playerId))
        {
            SendLobbyConfigurationResult(
                senderClientId,
                requestId,
                AuthorityResult.Rejected(
                    AuthorityResultCode.InvalidPlayer,
                    "No player is assigned to this connection."
                )
            );

            return;
        }

        AuthorityResult result =
            ApplyLobbyConfigurationOnServer(
                playerId,
                requestedPlayerCount,
                requestedTeamCount
            );

        Debug.Log(
            $"NETWORK lobby-config request: " +
            $"ClientId={senderClientId}, " +
            $"Player={playerId}, " +
            $"Players={requestedPlayerCount}, " +
            $"Teams={requestedTeamCount}, " +
            $"Accepted={result.Success}"
        );

        SendLobbyConfigurationResult(
            senderClientId,
            requestId,
            result
        );
    }

    private AuthorityResult ApplyLobbyConfigurationOnServer(
        int requestingPlayerId,
        int requestedPlayerCount,
        int requestedTeamCount)
    {
        if (!IsServer ||
            lobbyManager == null)
        {
            return AuthorityResult.Rejected(
                AuthorityResultCode.LobbyNotAvailable,
                "Lobby authority is unavailable."
            );
        }

        if (matchStarted)
        {
            return AuthorityResult.Rejected(
                AuthorityResultCode.LobbyNotAvailable,
                "Lobby settings cannot change after the match has started."
            );
        }

        if (!lobbyManager.IsValidLobbyConfiguration(
                requestedPlayerCount,
                requestedTeamCount,
                out string validationMessage))
        {
            return AuthorityResult.Rejected(
                AuthorityResultCode.LobbyNotAvailable,
                validationMessage
            );
        }

        // -----------------------------------------------------
        // Do not shrink the lobby in a way that removes an
        // actual connected or reserved/rejoin player.
        // -----------------------------------------------------

        foreach (
            KeyValuePair<ulong, int> pair
            in clientToPlayerId)
        {
            if (pair.Value >
                requestedPlayerCount)
            {
                return AuthorityResult.Rejected(
                    AuthorityResultCode.LobbyNotAvailable,
                    $"Cannot reduce the lobby to " +
                    $"{requestedPlayerCount} players while " +
                    $"Player {pair.Value} is connected."
                );
            }
        }

        foreach (int reservedPlayerId
                 in playerIdToRejoinToken.Keys)
        {
            if (reservedPlayerId >
                requestedPlayerCount)
            {
                return AuthorityResult.Rejected(
                    AuthorityResultCode.LobbyNotAvailable,
                    $"Cannot reduce the lobby to " +
                    $"{requestedPlayerCount} players because " +
                    $"Player {reservedPlayerId} is reserved " +
                    $"for rejoin."
                );
            }
        }

        // Never silently delete an occupied seat.
        foreach (LobbyPlayerData player
                 in lobbyManager.Players)
        {
            if (player.HasSeat &&
                player.SeatIndex >
                    requestedPlayerCount)
            {
                return AuthorityResult.Rejected(
                    AuthorityResultCode.LobbyNotAvailable,
                    $"Seat {player.SeatIndex} is occupied. " +
                    $"Choose a player count that keeps all " +
                    $"occupied seats."
                );
            }
        }

        bool success =
            lobbyManager.ConfigureLobby(
                requestedPlayerCount,
                requestedTeamCount
            );

        if (!success)
        {
            return AuthorityResult.Rejected(
                AuthorityResultCode.LobbyNotAvailable,
                "The requested lobby configuration is invalid."
            );
        }

        publicLobbyPlayerCount =
            lobbyManager.PlayerCount;

        publicLobbyTeamCount =
            lobbyManager.TeamCount;

        // First everybody receives the accepted configuration,
        // then everybody receives the current player/seat state.
        BroadcastLobbyConfiguration();
        BroadcastLobbyState();

        Debug.LogWarning(
            $"LOBBY CONFIG UPDATED by Player " +
            $"{requestingPlayerId}: " +
            $"{publicLobbyPlayerCount} players, " +
            $"{publicLobbyTeamCount} teams."
        );

        return AuthorityResult.Accepted(
            $"Lobby changed to " +
            $"{publicLobbyPlayerCount} players / " +
            $"{publicLobbyTeamCount} teams."
        );
    }

    private void BroadcastLobbyConfiguration()
    {
        if (!IsServer ||
            lobbyManager == null)
        {
            return;
        }

        publicLobbyPlayerCount =
            lobbyManager.PlayerCount;

        publicLobbyTeamCount =
            lobbyManager.TeamCount;

        SyncLobbyConfigurationClientRpc(
            publicLobbyPlayerCount,
            publicLobbyTeamCount
        );

        OnLobbyConfigurationChanged?.Invoke(
            publicLobbyPlayerCount,
            publicLobbyTeamCount
        );
    }

    [ClientRpc]
    private void SyncLobbyConfigurationClientRpc(
        int playerCount,
        int teamCount)
    {
        // The host/server already applied the authoritative
        // configuration and BroadcastLobbyConfiguration()
        // refreshes its local UI directly.
        if (IsServer)
            return;

        publicLobbyPlayerCount =
            playerCount;

        publicLobbyTeamCount =
            teamCount;

        if (lobbyManager != null)
        {
            lobbyManager.ConfigureLobby(
                playerCount,
                teamCount
            );
        }

        Debug.Log(
            $"SYNC lobby configuration: " +
            $"Players={playerCount}, " +
            $"Teams={teamCount}"
        );

        OnLobbyConfigurationChanged?.Invoke(
            playerCount,
            teamCount
        );
    }

    private void SendLobbyConfigurationResult(
        ulong targetClientId,
        int requestId,
        AuthorityResult result)
    {
        ClientRpcParams target =
            CreateTargetClientRpcParams(
                targetClientId
            );

        LobbyConfigurationResultClientRpc(
            requestId,
            result.Success,
            (int)result.Code,
            result.Message,
            target
        );
    }

    [ClientRpc]
    private void LobbyConfigurationResultClientRpc(
        int requestId,
        bool success,
        int resultCode,
        string message,
        ClientRpcParams clientRpcParams = default)
    {
        if (IsServer)
            return;

        if (!pendingLobbyConfigurationRequests.TryGetValue(
                requestId,
                out Action<AuthorityResult> callback))
        {
            return;
        }

        pendingLobbyConfigurationRequests.Remove(
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

            string displayName =
                GetPlayerDisplayName(
                    player.PlayerId
                );

            bool isConnected =
                IsPlayerConnected(
                    player.PlayerId
                );

            // Keep the server-side lobby model aligned too.
            player.SetDisplayName(
                displayName
            );

            player.SetConnected(
                isConnected
            );

            SyncPlayerStateClientRpc(
                player.PlayerId,
                displayName,
                player.SeatIndex,
                player.TeamId,
                player.IsReady,
                isConnected
            );
            playerSeatIndexes[player.PlayerId] =
                player.SeatIndex;

            playerTeamIds[player.PlayerId] =
                player.TeamId;
        }

        OnPublicPlayerStateChanged?.Invoke();
    }

    [ClientRpc]
    private void SyncPlayerStateClientRpc(
        int playerId,
        string displayName,
        int seatIndex,
        int teamId,
        bool isReady,
        bool isConnected)
    {
        // Store public profile data even if the local
        // LobbyManager has not created its player objects yet.
        playerDisplayNames[playerId] =
            string.IsNullOrWhiteSpace(
                displayName)
                ? $"Player {playerId}"
                : displayName.Trim();

        playerConnectedStates[playerId] =
            isConnected;
        playerSeatIndexes[playerId] =
            seatIndex;

        playerTeamIds[playerId] =
            teamId;

        // Host already owns the authoritative lobby state.
        if (!IsServer &&
            lobbyManager != null)
        {
            LobbyPlayerData player =
                lobbyManager.GetPlayer(
                    playerId
                );

            if (player != null)
            {
                player.SetDisplayName(
                    playerDisplayNames[playerId]
                );

                player.SetConnected(
                    isConnected
                );

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

                lobbyManager.SetPlayerReady(
                    playerId,
                    isReady
                );
            }
        }

        Debug.Log(
            $"SYNC public player state: " +
            $"Player={playerId}, " +
            $"Name={playerDisplayNames[playerId]}, " +
            $"Seat={seatIndex}, " +
            $"Team={teamId}, " +
            $"Ready={isReady}, " +
            $"Connected={isConnected}"
        );

        OnPublicPlayerStateChanged?.Invoke();
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
        matchStarted =
            true;

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

    if (result.Success)
    {
        matchStarted =
            true;

        if (networkMatchState != null)
        {
            networkMatchState.SetPhase(
                MatchPhase.Playing
            );
        }
    }

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


// =========================================================
// SESSION REGISTRATION / REJOIN
// =========================================================

public void RequestSessionRegistration(
    string roomCode,
    string rejoinToken,
    string displayName,
    Action<bool, int, bool, bool, string> onCompleted)
{
    if (!IsSpawned)
    {
        onCompleted?.Invoke(
            false,
            -1,
            false,
            false,
            "Network lobby bridge is not ready."
        );

        return;
    }

    if (IsServer)
    {
        onCompleted?.Invoke(
            false,
            1,
            false,
            matchStarted,
            "Host does not need remote session registration."
        );

        return;
    }

    if (string.IsNullOrWhiteSpace(
            rejoinToken))
    {
        onCompleted?.Invoke(
            false,
            -1,
            false,
            false,
            "Rejoin identity is missing."
        );

        return;
    }

    int requestId =
        nextSessionRegistrationRequestId++;

    pendingSessionRegistrationRequests[
        requestId
    ] = onCompleted;

    RequestSessionRegistrationServerRpc(
        requestId,
        roomCode,
        rejoinToken,
        displayName
    );
}

[ServerRpc(RequireOwnership = false)]
private void RequestSessionRegistrationServerRpc(
    int requestId,
    string roomCode,
    string rejoinToken,
    string displayName,
    ServerRpcParams rpcParams = default)
{
    ulong senderClientId =
        rpcParams.Receive.SenderClientId;

    string normalizedRoomCode =
        string.IsNullOrWhiteSpace(
            roomCode)
            ? ""
            : roomCode
                .Trim()
                .ToUpperInvariant();

    string normalizedToken =
        string.IsNullOrWhiteSpace(
            rejoinToken)
            ? ""
            : rejoinToken.Trim();

    string normalizedDisplayName =
        NormalizeDisplayName(
            displayName
        );

    if (string.IsNullOrWhiteSpace(
            normalizedToken))
    {
        SendSessionRegistrationResult(
            senderClientId,
            requestId,
            false,
            -1,
            false,
            matchStarted,
            "Rejoin identity is missing."
        );

        return;
    }

    // Validate the cosmetic/local room code against the
    // host's current room context when available.
    if (roomSessionContext != null &&
        roomSessionContext.HasRoom)
    {
        string authoritativeRoomCode =
            roomSessionContext.RoomCode
                .Trim()
                .ToUpperInvariant();

        if (!string.Equals(
                authoritativeRoomCode,
                normalizedRoomCode,
                StringComparison.Ordinal))
        {
            SendSessionRegistrationResult(
                senderClientId,
                requestId,
                false,
                -1,
                false,
                matchStarted,
                "Room code does not match this host."
            );

            return;
        }
    }

    // If this connection was already registered,
    // simply return its current identity.
    if (clientToPlayerId.TryGetValue(
            senderClientId,
            out int existingClientPlayerId))
    {
        bool existingWasRejoin =
            playerIdToRejoinToken.ContainsKey(
                existingClientPlayerId
            );

        ApplyDisplayName(
            existingClientPlayerId,
            normalizedDisplayName
        );

        playerConnectedStates[
            existingClientPlayerId
        ] = true;

        BroadcastLobbyConfiguration();
        BroadcastLobbyState();

        SendSessionRegistrationResult(
            senderClientId,
            requestId,
            true,
            existingClientPlayerId,
            existingWasRejoin,
            matchStarted,
            "Session identity already registered."
        );

        return;
    }

    bool isRejoin = false;
    int playerId = -1;

    // =====================================================
    // REJOIN: TOKEN ALREADY BELONGS TO A PLAYER
    // =====================================================

    if (rejoinTokenToPlayerId.TryGetValue(
            normalizedToken,
            out int reservedPlayerId))
    {
        playerId =
            reservedPlayerId;

        // The same PlayerId may not be actively connected
        // from two clients at once.
        foreach (
            KeyValuePair<ulong, int> pair
            in clientToPlayerId)
        {
            if (pair.Value ==
                playerId)
            {
                SendSessionRegistrationResult(
                    senderClientId,
                    requestId,
                    false,
                    -1,
                    true,
                    matchStarted,
                    $"Player {playerId} is already connected."
                );

                return;
            }
        }

        isRejoin =
            true;
    }

    // =====================================================
    // FIRST JOIN: CREATE A RESERVED PLAYER IDENTITY
    // =====================================================

    else
    {
        // Once the match has started, unknown tokens are not
        // allowed to enter as brand-new players.
        if (matchStarted)
        {
            SendSessionRegistrationResult(
                senderClientId,
                requestId,
                false,
                -1,
                false,
                true,
                "Match is already in progress and this device " +
                "does not have a recognized rejoin identity."
            );

            return;
        }

        playerId =
            FindNextAvailablePlayerId();

        if (playerId <= 0)
        {
            SendSessionRegistrationResult(
                senderClientId,
                requestId,
                false,
                -1,
                false,
                false,
                "No player slot is available."
            );

            return;
        }

        rejoinTokenToPlayerId[
            normalizedToken
        ] = playerId;

        playerIdToRejoinToken[
            playerId
        ] = normalizedToken;
    }

    clientToPlayerId[
        senderClientId
    ] = playerId;

    playerConnectedStates[playerId] =
        true;

    ApplyDisplayName(
        playerId,
        normalizedDisplayName
    );

    BroadcastLobbyConfiguration();
    BroadcastLobbyState();

    Debug.LogWarning(
        isRejoin
            ? $"REJOIN REGISTERED: ClientId={senderClientId} " +
              $"restored as Player {playerId}."
            : $"SESSION REGISTERED: ClientId={senderClientId} " +
              $"assigned to Player {playerId}."
    );

    SendSessionRegistrationResult(
        senderClientId,
        requestId,
        true,
        playerId,
        isRejoin,
        matchStarted,
        isRejoin
            ? $"Rejoined as Player {playerId}."
            : $"Joined as Player {playerId}."
    );
}

private void SendSessionRegistrationResult(
    ulong targetClientId,
    int requestId,
    bool success,
    int playerId,
    bool isRejoin,
    bool isMatchInProgress,
    string message)
{
    ClientRpcParams target =
        CreateTargetClientRpcParams(
            targetClientId
        );

    SessionRegistrationResultClientRpc(
        requestId,
        success,
        playerId,
        isRejoin,
        isMatchInProgress,
        message,
        target
    );
}

[ClientRpc]
private void SessionRegistrationResultClientRpc(
    int requestId,
    bool success,
    int playerId,
    bool isRejoin,
    bool isMatchInProgress,
    string message,
    ClientRpcParams clientRpcParams = default)
{
    if (IsServer)
        return;

    if (!pendingSessionRegistrationRequests.TryGetValue(
            requestId,
            out Action<bool, int, bool, bool, string> callback))
    {
        return;
    }

    pendingSessionRegistrationRequests.Remove(
        requestId
    );

    callback?.Invoke(
        success,
        playerId,
        isRejoin,
        isMatchInProgress,
        message
    );
}

// =========================================================
// REQUEST FULL CURRENT MATCH STATE AFTER REJOIN
// =========================================================

public void RequestCurrentMatchStateSync(
    Action<bool, string> onCompleted)
{
    if (!IsSpawned)
    {
        onCompleted?.Invoke(
            false,
            "Network lobby bridge is not ready."
        );

        return;
    }

    if (IsServer)
    {
        onCompleted?.Invoke(
            false,
            "Host does not need rejoin state synchronization."
        );

        return;
    }

    int requestId =
        nextStateSyncRequestId++;

    pendingStateSyncRequests[
        requestId
    ] = onCompleted;

    RequestCurrentMatchStateSyncServerRpc(
        requestId
    );
}

[ServerRpc(RequireOwnership = false)]
private void RequestCurrentMatchStateSyncServerRpc(
    int requestId,
    ServerRpcParams rpcParams = default)
{
    ulong senderClientId =
        rpcParams.Receive.SenderClientId;

    if (!TryGetPlayerIdForClient(
            senderClientId,
            out int playerId))
    {
        SendStateSyncResult(
            senderClientId,
            requestId,
            false,
            "This connection has no registered PlayerId."
        );

        return;
    }

    if (!matchStarted)
    {
        SendStateSyncResult(
            senderClientId,
            requestId,
            false,
            "The match has not started."
        );

        return;
    }

    if (networkGameplayBridge == null)
    {
        SendStateSyncResult(
            senderClientId,
            requestId,
            false,
            "NetworkGameplayBridge is unavailable."
        );

        return;
    }

    if (networkHandState == null)
    {
        SendStateSyncResult(
            senderClientId,
            requestId,
            false,
            "NetworkHandState is unavailable."
        );

        return;
    }

    // Public NetworkVariables such as phase, current turn,
    // public sequence counts, and winner automatically sync
    // when the client reconnects.
    //
    // These two pieces need explicit refresh:
    //
    // 1. full board snapshot
    // 2. this player's private hand
    networkGameplayBridge.SendFullBoardSnapshotToPlayer(
        playerId
    );

    networkHandState.SendHandToPlayer(
        playerId
    );

    Debug.LogWarning(
        $"REJOIN STATE sent to Player {playerId} " +
        $"(ClientId={senderClientId})."
    );

    SendStateSyncResult(
        senderClientId,
        requestId,
        true,
        $"Player {playerId} match state restored."
    );
}

private void SendStateSyncResult(
    ulong targetClientId,
    int requestId,
    bool success,
    string message)
{
    ClientRpcParams target =
        CreateTargetClientRpcParams(
            targetClientId
        );

    StateSyncResultClientRpc(
        requestId,
        success,
        message,
        target
    );
}

[ClientRpc]
private void StateSyncResultClientRpc(
    int requestId,
    bool success,
    string message,
    ClientRpcParams clientRpcParams = default)
{
    if (IsServer)
        return;

    if (!pendingStateSyncRequests.TryGetValue(
            requestId,
            out Action<bool, string> callback))
    {
        return;
    }

    pendingStateSyncRequests.Remove(
        requestId
    );

    callback?.Invoke(
        success,
        message
    );
}


// =========================================================
// PUBLIC PLAYER PROFILE / PRESENCE
// =========================================================

public void SetHostDisplayName(
    string displayName)
{
    if (!IsServer)
        return;

    string normalized =
        NormalizeDisplayName(
            displayName
        );

    ApplyDisplayName(
        1,
        normalized
    );

    playerConnectedStates[1] =
        true;

    BroadcastLobbyState();

    OnPublicPlayerStateChanged?.Invoke();
}

public string GetPlayerDisplayName(
    int playerId)
{
    if (playerDisplayNames.TryGetValue(
            playerId,
            out string displayName) &&
        !string.IsNullOrWhiteSpace(
            displayName))
    {
        return displayName;
    }

    if (lobbyManager != null)
    {
        LobbyPlayerData player =
            lobbyManager.GetPlayer(
                playerId
            );

        if (player != null &&
            !string.IsNullOrWhiteSpace(
                player.DisplayName))
        {
            return player.DisplayName;
        }
    }

    return $"Player {playerId}";
}

public bool IsPlayerConnected(
    int playerId)
{
    if (playerConnectedStates.TryGetValue(
            playerId,
            out bool connected))
    {
        return connected;
    }

    // The host exists as long as this listen server exists.
    if (IsServer &&
        playerId == 1)
    {
        return true;
    }

    return false;
}

public int GetPlayerSeatIndex(
    int playerId)
{
    if (playerSeatIndexes.TryGetValue(
            playerId,
            out int seatIndex))
    {
        return seatIndex;
    }

    if (lobbyManager != null)
    {
        LobbyPlayerData player =
            lobbyManager.GetPlayer(
                playerId
            );

        if (player != null)
            return player.SeatIndex;
    }

    return -1;
}

public int GetPlayerTeamId(
    int playerId)
{
    if (playerTeamIds.TryGetValue(
            playerId,
            out int teamId))
    {
        return teamId;
    }

    if (lobbyManager != null)
    {
        LobbyPlayerData player =
            lobbyManager.GetPlayer(
                playerId
            );

        if (player != null)
            return player.TeamId;
    }

    return -1;
}

public List<int> GetKnownPlayerIds()
{
    List<int> playerIds =
        new List<int>();

    foreach (int playerId
             in playerDisplayNames.Keys)
    {
        if (!playerIds.Contains(
                playerId))
        {
            playerIds.Add(
                playerId
            );
        }
    }

    foreach (int playerId
             in playerSeatIndexes.Keys)
    {
        if (!playerIds.Contains(
                playerId))
        {
            playerIds.Add(
                playerId
            );
        }
    }

    playerIds.Sort();

    return playerIds;
}

private void ApplyDisplayName(
    int playerId,
    string displayName)
{
    if (playerId <= 0)
        return;

    string finalName =
        string.IsNullOrWhiteSpace(
            displayName)
            ? GetPlayerDisplayName(
                playerId
            )
            : displayName;

    playerDisplayNames[playerId] =
        finalName;

    if (lobbyManager == null)
        return;

    LobbyPlayerData player =
        lobbyManager.GetPlayer(
            playerId
        );

    if (player != null)
    {
        player.SetDisplayName(
            finalName
        );
    }
}

private string NormalizeDisplayName(
    string displayName)
{
    if (string.IsNullOrWhiteSpace(
            displayName))
    {
        return "";
    }

    string value =
        displayName.Trim();

    if (value.Length > 16)
    {
        value =
            value.Substring(
                0,
                16
            );
    }

    return value;
}

// =========================================================
// PLAYER -> NETWORK CLIENT LOOKUP
// =========================================================

public bool TryGetClientIdForPlayerId(
    int playerId,
    out ulong clientId)
{
    foreach (
        KeyValuePair<ulong, int> pair
        in clientToPlayerId)
    {
        if (pair.Value == playerId)
        {
            clientId = pair.Key;
            return true;
        }
    }

    clientId = 0;
    return false;
}

}