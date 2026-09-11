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
    // Later the network/server will assign PlayerId values.
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
        if (roomSessionContext == null)
        {
            SetMessage(
                "Room session is unavailable."
            );

            Debug.LogError(
                "RoomEntryUI: RoomSessionContext is missing."
            );

            return;
        }

        string roomCode =
            GenerateTemporaryRoomCode();

        // -----------------------------------------------------
        // IMPORTANT:
        //
        // RoomEntryUI does NOT store:
        // isHost = true
        //
        // Instead the SESSION owns that information.
        // -----------------------------------------------------

        roomSessionContext.ConfigureAsHost(
            roomCode,
            temporaryHostPlayerId
        );

        Debug.Log(
            $"TEMP ROOM CREATED: " +
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
    // JOIN ROOM
    // =========================================================

    private void HandleJoinRoomClicked()
    {
        if (roomSessionContext == null)
        {
            SetMessage(
                "Room session is unavailable."
            );

            Debug.LogError(
                "RoomEntryUI: RoomSessionContext is missing."
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
        // TEMPORARY LOCAL TESTING
        //
        // We currently pretend:
        //
        // Host   = Player 1
        // Client = Player 2
        //
        // Later the HOST/SERVER will assign the real PlayerId.
        // -----------------------------------------------------

        roomSessionContext.ConfigureAsClient(
            enteredCode,
            temporaryJoiningPlayerId,
            temporaryHostPlayerId
        );

        Debug.Log(
            $"TEMP JOIN ROOM: " +
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