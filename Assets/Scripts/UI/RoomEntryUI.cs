using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomEntryUI : MonoBehaviour
{
    // =========================================================
    // SESSION
    // =========================================================

    [Header("Session")]
    [SerializeField]
    private RoomSessionContext roomSessionContext;

    // =========================================================
    // NETWORKING
    // =========================================================

    [Header("Networking")]
    [SerializeField]
    private SequenceNetworkBootstrap networkBootstrap;

    [SerializeField]
    private NetworkLobbyBridge networkLobbyBridge;

    // =========================================================
    // PANELS
    // =========================================================

    [Header("Panels")]
    [SerializeField] private GameObject roomEntryPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject boardPanel;
    [SerializeField] private GameObject handPanel;
    [SerializeField] private GameObject gameStatusPanel;

    // =========================================================
    // ROOM CONTROLS
    // =========================================================

    [Header("Room Controls")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button joinRoomButton;

    // =========================================================
    // STATUS
    // =========================================================

    [Header("Status")]
    [SerializeField] private TMP_Text roomMessageText;

    // =========================================================
    // TEMPORARY LOCAL TEST IDENTITY
    //
    // These IDs are temporary.
    // The next rejoin step will make the SERVER restore
    // PlayerId from the persistent rejoin token.
    // =========================================================

    [Header("Temporary Local Testing")]
    [SerializeField]
    private int temporaryHostPlayerId = 1;


    // =========================================================
    // PUBLIC DATA
    // =========================================================

    public bool IsHost
    {
        get
        {
            return
                roomSessionContext != null &&
                roomSessionContext.IsLocalPlayerHost;
        }
    }

    public string CurrentRoomCode
    {
        get
        {
            if (roomSessionContext == null)
                return "";

            return roomSessionContext.RoomCode;
        }
    }

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        ShowRoomEntry();
    }

    private void OnEnable()
    {
        if (createRoomButton != null)
        {
            createRoomButton.onClick.AddListener(
                HandleCreateRoomClicked
            );
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.onClick.AddListener(
                HandleJoinRoomClicked
            );
        }

        if (networkBootstrap != null)
        {
            networkBootstrap.OnLocalClientDisconnected +=
                HandleLocalClientDisconnected;
        }
    }

    private void OnDisable()
    {
        if (createRoomButton != null)
        {
            createRoomButton.onClick.RemoveListener(
                HandleCreateRoomClicked
            );
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.onClick.RemoveListener(
                HandleJoinRoomClicked
            );
        }

        if (networkBootstrap != null)
        {
            networkBootstrap.OnLocalClientDisconnected -=
                HandleLocalClientDisconnected;
        }
    }

    // =========================================================
    // CREATE ROOM
    // =========================================================

    private void HandleCreateRoomClicked()
    {
        if (!TryGetPlayerName(
                out string displayName))
        {
            return;
        }

        if (networkBootstrap == null)
        {
            Debug.LogError(
                "RoomEntryUI: SequenceNetworkBootstrap is missing."
            );

            SetMessage(
                "Networking is unavailable."
            );

            return;
        }

        if (roomSessionContext == null)
        {
            Debug.LogError(
                "RoomEntryUI: RoomSessionContext is missing."
            );

            SetMessage(
                "Room session is unavailable."
            );

            return;
        }

        bool hostStarted =
            networkBootstrap.StartHost();

        if (!hostStarted)
        {
            Debug.LogError(
                "RoomEntryUI: Failed to start network host."
            );

            SetMessage(
                "Could not create room."
            );

            return;
        }

        string roomCode =
            GenerateTemporaryRoomCode();

        roomSessionContext.ConfigureAsHost(
            roomCode,
            temporaryHostPlayerId
        );

        if (networkLobbyBridge != null)
        {
            networkLobbyBridge.SetHostDisplayName(
                displayName
            );
        }

        Debug.Log(
            $"ROOM CREATED: " +
            $"{roomSessionContext.RoomCode}"
        );

        Debug.Log(
            $"Local Player: " +
            $"{roomSessionContext.LocalPlayerId}"
        );

        Debug.Log(
            $"Host Player: " +
            $"{roomSessionContext.HostPlayerId}"
        );

        OpenLobby();
    }

    // =========================================================
    // JOIN / REJOIN ROOM
    // =========================================================

    private void HandleJoinRoomClicked()
    {
        if (!TryGetPlayerName(
                out string displayName))
        {
            return;
        }

        if (networkBootstrap == null)
        {
            Debug.LogError(
                "RoomEntryUI: SequenceNetworkBootstrap is missing."
            );

            SetMessage(
                "Networking is unavailable."
            );

            return;
        }

        if (roomSessionContext == null)
        {
            Debug.LogError(
                "RoomEntryUI: RoomSessionContext is missing."
            );

            SetMessage(
                "Room session is unavailable."
            );

            return;
        }

        if (networkLobbyBridge == null)
        {
            Debug.LogError(
                "RoomEntryUI: NetworkLobbyBridge is missing."
            );

            SetMessage(
                "Network session registration is unavailable."
            );

            return;
        }

        if (roomCodeInput == null)
            return;

        string enteredCode =
            roomCodeInput.text
                .Trim()
                .ToUpper();

        if (string.IsNullOrWhiteSpace(
                enteredCode))
        {
            SetMessage(
                "Enter a room code before joining."
            );

            return;
        }

        // -----------------------------------------------------
        // REJOIN IDENTITY
        //
        // RejoinToken identifies the PLAYER.
        // DisplayName is allowed to change on rejoin.
        //
        // Same token + different name:
        // PlayerId / seat / team / hand stay the same,
        // while the public display name can use the new name.
        // -----------------------------------------------------

        bool hasExistingRejoinToken =
            roomSessionContext.TryLoadRejoinToken(
                enteredCode,
                out string rejoinToken
            );

        // No saved identity for this room yet:
        // create a brand-new token.
        if (!hasExistingRejoinToken)
        {
            rejoinToken =
                roomSessionContext.GetOrCreateRejoinToken(
                    enteredCode
                );
        }

        if (string.IsNullOrWhiteSpace(
                rejoinToken))
        {
            SetMessage(
                "Could not create a player identity."
            );

            return;
        }

        // Local hint only. The server decides whether this
        // token is actually a known rejoin identity.
        SetMessage(
            hasExistingRejoinToken
                ? $"Rejoining as {displayName}..."
                : $"Joining as {displayName}..."
        );
        
        bool connectionStarted =
            networkBootstrap.StartClient(

                () =>
                {
                    Debug.Log(
                        "Network connection succeeded. " +
                        "Waiting for session registration..."
                    );

                    StartCoroutine(
                        RegisterConnectedClient(
                            enteredCode,
                            rejoinToken,
                            displayName
                        )
                    );
                },

                (errorMessage) =>
                {
                    Debug.LogWarning(
                        $"Join failed: {errorMessage}"
                    );

                    SetMessage(
                        errorMessage
                    );
                }
            );

        if (!connectionStarted)
        {
            Debug.LogWarning(
                "Client connection attempt could not start."
            );
        }
    }

    // =========================================================
    // SERVER SESSION REGISTRATION
    //
    // Transport connection is not enough to identify the player.
    //
    // ClientId
    //    + persistent RejoinToken
    //          ↓
    // SERVER
    //          ↓
    // permanent PlayerId
    // =========================================================

    private IEnumerator RegisterConnectedClient(
        string enteredCode,
        string rejoinToken,
        string displayName)
    {
        float timeoutAt =
            Time.realtimeSinceStartup +
            5f;

        // The transport can report "connected" slightly before
        // this scene NetworkBehaviour has completed its network
        // spawn. Wait briefly instead of racing the first RPC.
        while (networkLobbyBridge != null &&
               !networkLobbyBridge.IsSpawned &&
               Time.realtimeSinceStartup <
                   timeoutAt)
        {
            yield return null;
        }

        if (networkLobbyBridge == null ||
            !networkLobbyBridge.IsSpawned)
        {
            SetMessage(
                "Connected, but session registration " +
                "did not become ready."
            );

            Debug.LogWarning(
                "NetworkLobbyBridge did not spawn in time."
            );

            yield break;
        }

        SetMessage(
            "Registering player identity..."
        );

        networkLobbyBridge.RequestSessionRegistration(
            enteredCode,
            rejoinToken,
            displayName,

            (
                success,
                assignedPlayerId,
                isRejoin,
                isMatchInProgress,
                message
            ) =>
            {
                if (!success)
                {
                    Debug.LogWarning(
                        $"Session registration failed: " +
                        $"{message}"
                    );

                    SetMessage(
                        message
                    );

                    networkBootstrap.Shutdown();

                    return;
                }

                // IMPORTANT:
                // The SERVER is now the source of truth for
                // PlayerId. We no longer assume the remote
                // client is Player 2.
                roomSessionContext.ConfigureAsClient(
                    enteredCode,
                    assignedPlayerId,
                    temporaryHostPlayerId,
                    rejoinToken
                );

                Debug.LogWarning(
                    isRejoin
                        ? $"REJOIN IDENTITY RESTORED: " +
                          $"Player {assignedPlayerId}."
                        : $"SERVER ASSIGNED IDENTITY: " +
                          $"Player {assignedPlayerId}."
                );

                // The server has now confirmed whether this
                // connection is a new join or a real rejoin.
                SetMessage(
                    isRejoin
                        ? $"Rejoining as {displayName}..."
                        : $"Joined as {displayName}."
                );

                if (!isMatchInProgress)
                {
                    OpenLobby();

                    return;
                }

                // Existing match:
                // now that LocalPlayerId is configured,
                // request the private hand and full board.
                SetMessage(
                    $"Rejoining as {displayName}..."
                );

                networkLobbyBridge.RequestCurrentMatchStateSync(
                    (syncSuccess, syncMessage) =>
                    {
                        if (!syncSuccess)
                        {
                            Debug.LogWarning(
                                $"Rejoin state sync failed: " +
                                $"{syncMessage}"
                            );

                            SetMessage(
                                syncMessage
                            );

                            return;
                        }

                        Debug.LogWarning(
                            $"REJOIN COMPLETE: " +
                            $"{syncMessage}"
                        );

                        SetMessage(
                            $"{displayName} rejoined the game."
                        );

                        OpenGame();
                    }
                );
            }
        );
    }

    // =========================================================
    // ACCIDENTAL CLIENT DISCONNECT
    // =========================================================

    private void HandleLocalClientDisconnected()
    {
        if (roomSessionContext == null)
            return;

        // Host recovery is a separate feature.
        if (roomSessionContext.IsLocalPlayerHost)
            return;

        string previousRoomCode =
            roomSessionContext.RoomCode;

        Debug.LogWarning(
            $"Connection lost for Player " +
            $"{roomSessionContext.LocalPlayerId}. " +
            $"Room {previousRoomCode} remains available for rejoin."
        );

        // IMPORTANT:
        // Do NOT ClearSession().
        // Do NOT delete the rejoin token.
        //
        // The server should continue holding the
        // authoritative Player object and hand.
        if (roomEntryPanel != null)
            roomEntryPanel.SetActive(true);

        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);

        if (boardPanel != null)
            boardPanel.SetActive(false);

        if (handPanel != null)
            handPanel.SetActive(false);

        if (gameStatusPanel != null)
            gameStatusPanel.SetActive(false);

        if (roomCodeInput != null &&
            !string.IsNullOrWhiteSpace(
                previousRoomCode))
        {
            roomCodeInput.text =
                previousRoomCode;
        }

        SetMessage(
            "Connection lost. " +
            "Press Join to rejoin the same room."
        );
    }

    // =========================================================
    // OPEN LOBBY
    // =========================================================

    private void OpenLobby()
    {
        if (roomEntryPanel != null)
            roomEntryPanel.SetActive(false);

        if (lobbyPanel != null)
            lobbyPanel.SetActive(true);

        if (boardPanel != null)
            boardPanel.SetActive(false);

        if (handPanel != null)
            handPanel.SetActive(false);

        if (gameStatusPanel != null)
            gameStatusPanel.SetActive(false);

        if (roomSessionContext == null)
            return;

        if (roomSessionContext.IsLocalPlayerHost)
        {
            Debug.Log(
                $"Entered Room " +
                $"{roomSessionContext.RoomCode} " +
                $"as HOST."
            );
        }
        else
        {
            Debug.Log(
                $"Entered Room " +
                $"{roomSessionContext.RoomCode} " +
                $"as CLIENT."
            );
        }
    }

    // =========================================================
    // OPEN EXISTING GAME AFTER REJOIN
    // =========================================================

    private void OpenGame()
    {
        if (roomEntryPanel != null)
            roomEntryPanel.SetActive(false);

        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);

        if (boardPanel != null)
            boardPanel.SetActive(true);

        if (handPanel != null)
            handPanel.SetActive(true);

        if (gameStatusPanel != null)
            gameStatusPanel.SetActive(true);

        if (roomSessionContext != null)
        {
            Debug.LogWarning(
                $"Opened existing match for " +
                $"Player {roomSessionContext.LocalPlayerId}."
            );
        }
    }

    // =========================================================
    // INITIAL SCREEN
    // =========================================================

    private void ShowRoomEntry()
    {
        if (roomEntryPanel != null)
            roomEntryPanel.SetActive(true);

        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);

        if (boardPanel != null)
            boardPanel.SetActive(false);

        if (handPanel != null)
            handPanel.SetActive(false);

        if (gameStatusPanel != null)
            gameStatusPanel.SetActive(false);

        SetMessage(
            "Create a new room or join an existing room."
        );
    }

    // =========================================================
    // TEMPORARY ROOM CODE
    // =========================================================

    private string GenerateTemporaryRoomCode()
    {
        const string characters =
            "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        string code = "";

        for (int i = 0; i < 6; i++)
        {
            int index =
                Random.Range(
                    0,
                    characters.Length
                );

            code +=
                characters[index];
        }

        return code;
    }

    // =========================================================
    // PLAYER DISPLAY NAME
    // =========================================================

    private bool TryGetPlayerName(
        out string displayName)
    {
        displayName = "";

        if (playerNameInput == null)
        {
            SetMessage(
                "Player name input is missing."
            );

            return false;
        }

        displayName =
            playerNameInput.text
                .Trim();

        if (displayName.Length < 2)
        {
            SetMessage(
                "Enter a player name with at least 2 characters."
            );

            return false;
        }

        if (displayName.Length > 16)
        {
            displayName =
                displayName.Substring(
                    0,
                    16
                );

            playerNameInput.text =
                displayName;
        }

        return true;
    }

    // =========================================================
    // MESSAGE
    // =========================================================

    private void SetMessage(
        string message)
    {
        if (roomMessageText != null)
        {
            roomMessageText.text =
                message;
        }
    }
}
