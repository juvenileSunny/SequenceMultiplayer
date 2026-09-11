using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Dropdown seatDropdown;
    [SerializeField] private TMP_Text teamText;
    [SerializeField] private TMP_Text readyStatusText;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;

    private LobbyManager lobbyManager;

    // NEW:
    // UI sends requests through the authority instead of
    // changing LobbyManager state directly.
    private LocalLobbyAuthority lobbyAuthority;

    private int playerId = -1;

    private bool initialized = false;
    private bool suppressCallbacks = false;

    // Maps dropdown option index to real SeatIndex.
    private readonly List<int> seatOptions =
        new List<int>();

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (seatDropdown != null)
        {
            seatDropdown.onValueChanged.AddListener(
                HandleSeatChanged
            );
        }

        if (readyButton != null)
        {
            readyButton.onClick.AddListener(
                HandleReadyClicked
            );
        }
    }

    private void OnDestroy()
    {
        if (seatDropdown != null)
        {
            seatDropdown.onValueChanged.RemoveListener(
                HandleSeatChanged
            );
        }

        if (readyButton != null)
        {
            readyButton.onClick.RemoveListener(
                HandleReadyClicked
            );
        }

        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyChanged -=
                HandleLobbyChanged;
        }
    }

    // =========================================================
    // INITIALIZE
    // =========================================================

    public void Initialize(
        LobbyManager manager,
        LocalLobbyAuthority authority,
        int newPlayerId)
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyChanged -=
                HandleLobbyChanged;
        }

        lobbyManager = manager;
        lobbyAuthority = authority;
        playerId = newPlayerId;

        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyChanged +=
                HandleLobbyChanged;
        }

        initialized = true;

        Refresh();
    }

    // =========================================================
    // LOBBY CHANGED
    // =========================================================

    private void HandleLobbyChanged()
    {
        Refresh();
    }

    // =========================================================
    // BUILD SEAT DROPDOWN
    // =========================================================

    private void BuildSeatDropdown(
        LobbyPlayerData currentPlayer)
    {
        if (seatDropdown == null ||
            lobbyManager == null)
        {
            return;
        }

        suppressCallbacks = true;

        seatDropdown.ClearOptions();
        seatOptions.Clear();

        List<string> labels =
            new List<string>();

        // -----------------------------------------------------
        // NO SEAT
        // -----------------------------------------------------

        labels.Add("NO SEAT");
        seatOptions.Add(-1);

        // -----------------------------------------------------
        // AVAILABLE SEATS
        // -----------------------------------------------------

        for (int seatIndex = 1;
             seatIndex <= lobbyManager.PlayerCount;
             seatIndex++)
        {
            LobbyPlayerData occupant =
                lobbyManager.GetPlayerInSeat(
                    seatIndex
                );

            bool seatIsFree =
                occupant == null;

            bool seatBelongsToThisPlayer =
                occupant != null &&
                occupant.PlayerId == playerId;

            // Do not show seats occupied by another player.
            if (!seatIsFree &&
                !seatBelongsToThisPlayer)
            {
                continue;
            }

            labels.Add(
                $"SEAT {seatIndex}"
            );

            seatOptions.Add(
                seatIndex
            );
        }

        seatDropdown.AddOptions(
            labels
        );

        // -----------------------------------------------------
        // CURRENT SELECTION
        // -----------------------------------------------------

        int selectedDropdownIndex = 0;

        if (currentPlayer != null &&
            currentPlayer.SeatIndex > 0)
        {
            int foundIndex =
                seatOptions.IndexOf(
                    currentPlayer.SeatIndex
                );

            if (foundIndex >= 0)
            {
                selectedDropdownIndex =
                    foundIndex;
            }
        }

        seatDropdown.SetValueWithoutNotify(
            selectedDropdownIndex
        );

        seatDropdown.RefreshShownValue();

        suppressCallbacks = false;
    }

    // =========================================================
    // REFRESH
    // =========================================================

    public void Refresh()
    {
        if (!initialized ||
            lobbyManager == null)
        {
            return;
        }

        LobbyPlayerData player =
            lobbyManager.GetPlayer(
                playerId
            );

        if (player == null)
            return;

        BuildSeatDropdown(
            player
        );

        // -----------------------------------------------------
        // PLAYER NAME
        // -----------------------------------------------------

        if (playerNameText != null)
        {
            playerNameText.text =
                !string.IsNullOrWhiteSpace(
                    player.DisplayName)
                    ? player.DisplayName.ToUpper()
                    : $"PLAYER {player.PlayerId}";
        }

        // -----------------------------------------------------
        // TEAM
        // -----------------------------------------------------

        if (teamText != null)
        {
            teamText.text =
                GetTeamDisplayName(
                    player.TeamId
                );
        }

        // -----------------------------------------------------
        // READY STATUS
        // -----------------------------------------------------

        if (readyStatusText != null)
        {
            readyStatusText.text =
                player.IsReady
                    ? "READY"
                    : "NOT READY";
        }

        // -----------------------------------------------------
        // READY BUTTON
        // -----------------------------------------------------

        bool hasValidSeat =
            player.SeatIndex > 0 &&
            player.TeamId > 0;

        if (readyButton != null)
        {
            readyButton.interactable =
                hasValidSeat;
        }

        if (readyButtonText != null)
        {
            readyButtonText.text =
                player.IsReady
                    ? "UNREADY"
                    : "READY";
        }
    }

    // =========================================================
    // SEAT REQUEST
    // =========================================================

    private void HandleSeatChanged(
        int dropdownIndex)
    {
        if (suppressCallbacks)
            return;

        if (!initialized)
            return;

        if (lobbyAuthority == null)
        {
            Debug.LogError(
                "LobbyPlayerSlotUI: " +
                "LocalLobbyAuthority is missing."
            );

            Refresh();
            return;
        }

        if (dropdownIndex < 0 ||
            dropdownIndex >= seatOptions.Count)
        {
            return;
        }

        int requestedSeat =
            seatOptions[
                dropdownIndex
            ];

        // -----------------------------------------------------
        // CREATE REQUEST
        // -----------------------------------------------------

        RequestSeatRequest request =
            new RequestSeatRequest(
                requestedSeat
            );

        // -----------------------------------------------------
        // SEND REQUEST TO AUTHORITY
        // -----------------------------------------------------

        AuthorityResult result =
            lobbyAuthority.HandleSeatRequest(
                playerId,
                request
            );

        // -----------------------------------------------------
        // RESULT
        // -----------------------------------------------------

        if (!result.Success)
        {
            Debug.LogWarning(
                $"Seat request rejected: " +
                $"{result.Code} - {result.Message}"
            );

            // Return dropdown to actual authoritative state.
            Refresh();

            return;
        }

        Debug.Log(
            $"Seat request accepted: " +
            $"{result.Message}"
        );
    }

    // =========================================================
    // READY REQUEST
    // =========================================================

    private void HandleReadyClicked()
    {
        if (!initialized)
            return;

        if (lobbyManager == null ||
            lobbyAuthority == null)
        {
            Debug.LogError(
                "Lobby authority references are missing."
            );

            return;
        }

        LobbyPlayerData player =
            lobbyManager.GetPlayer(
                playerId
            );

        if (player == null)
            return;

        // -----------------------------------------------------
        // CREATE REQUEST
        // -----------------------------------------------------

        SetReadyRequest request =
            new SetReadyRequest(
                !player.IsReady
            );

        // -----------------------------------------------------
        // SEND TO AUTHORITY
        // -----------------------------------------------------

        AuthorityResult result =
            lobbyAuthority.HandleReadyRequest(
                playerId,
                request
            );

        // -----------------------------------------------------
        // RESULT
        // -----------------------------------------------------

        if (!result.Success)
        {
            Debug.LogWarning(
                $"Ready request rejected: " +
                $"{result.Code} - {result.Message}"
            );

            Refresh();

            return;
        }

        Debug.Log(
            $"Ready request accepted: " +
            $"{result.Message}"
        );
    }

    // =========================================================
    // TEAM DISPLAY
    // =========================================================

    private string GetTeamDisplayName(
        int teamId)
    {
        switch (teamId)
        {
            case 1:
                return "TEAM RED";

            case 2:
                return "TEAM BLUE";

            case 3:
                return "TEAM GREEN";

            default:
                return "NO TEAM";
        }
    }
}