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
    // Later the server will assign PlayerId values based
    // on actual NGO client connections.
    // =========================================================

    [Header("Temporary Local Testing")]
    [SerializeField]
    private int temporaryHostPlayerId = 1;

    [SerializeField]
    private int temporaryJoiningPlayerId = 2;

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
    }

    // =========================================================
    // CREATE ROOM
    // =========================================================

    private void HandleCreateRoomClicked()
    {
        // -----------------------------------------------------
        // 1. Validate network bootstrap
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // 2. Validate session context BEFORE starting host
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // 3. Start the REAL NGO host
        //
        // HOST =
        // Server + local Client
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // 4. Generate temporary room code
        //
        // IMPORTANT:
        // This room code does NOT perform real discovery yet.
        // It is still temporary/local.
        // -----------------------------------------------------

        string roomCode =
            GenerateTemporaryRoomCode();

        // -----------------------------------------------------
        // 5. Configure our game session
        //
        // temporaryHostPlayerId is currently Player 1.
        //
        // Later:
        //
        // NGO ClientId
        //      ↓
        // PlayerId
        //
        // will replace this temporary assignment.
        // -----------------------------------------------------

        roomSessionContext.ConfigureAsHost(
            roomCode,
            temporaryHostPlayerId
        );

        // -----------------------------------------------------
        // 6. Debug information
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // 7. Enter lobby
        // -----------------------------------------------------

        OpenLobby();
    }

    // =========================================================
    // JOIN ROOM
    // =========================================================

    private void HandleJoinRoomClicked()
    {
        // =========================================================
        // VALIDATE REFERENCES
        // =========================================================

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

        if (roomCodeInput == null)
            return;

        // =========================================================
        // READ ROOM CODE
        // =========================================================

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

        // =========================================================
        // BEGIN REAL NETWORK CONNECTION
        // =========================================================

        SetMessage(
            "Connecting to host..."
        );

        bool connectionStarted =
            networkBootstrap.StartClient(

                // =============================================
                // CONNECTION SUCCESS
                // =============================================

                () =>
                {
                    Debug.Log(
                        "Network connection succeeded."
                    );

                    // -----------------------------------------
                    // TEMPORARY GAME IDENTITY
                    //
                    // We still temporarily pretend:
                    //
                    // Host   = Player 1
                    // Client = Player 2
                    //
                    // Later the SERVER will assign PlayerId.
                    // -----------------------------------------

                    roomSessionContext.ConfigureAsClient(
                        enteredCode,
                        temporaryJoiningPlayerId,
                        temporaryHostPlayerId
                    );

                    Debug.Log(
                        $"Joined network room: " +
                        $"{enteredCode}"
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
                },

                // =============================================
                // CONNECTION FAILURE
                // =============================================

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