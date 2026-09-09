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

    private int playerId = -1;
    private bool initialized = false;
    private bool suppressCallbacks = false;

    // Maps dropdown index -> actual SeatIndex.
    //
    // Example:
    // dropdown index 0 = -1 (NO SEAT)
    // dropdown index 1 = Seat 1
    // dropdown index 2 = Seat 4
    //
    // This is required because occupied seats may be missing.
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
        int newPlayerId)
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyChanged -=
                HandleLobbyChanged;
        }

        lobbyManager = manager;
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

            // Only show:
            // 1. Free seats
            // 2. This player's currently occupied seat
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
        // SELECT CURRENT SEAT
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

        // IMPORTANT:
        // Rebuild every time lobby state changes.
        //
        // This causes newly occupied seats to disappear
        // immediately from everyone else's dropdown.
        BuildSeatDropdown(
            player
        );

        // -----------------------------------------------------
        // PLAYER NAME
        // -----------------------------------------------------

        if (playerNameText != null)
        {
            if (!string.IsNullOrWhiteSpace(
                    player.DisplayName))
            {
                playerNameText.text =
                    player.DisplayName.ToUpper();
            }
            else
            {
                playerNameText.text =
                    $"PLAYER {player.PlayerId}";
            }
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
    // SEAT CHANGED
    // =========================================================

    private void HandleSeatChanged(
        int dropdownIndex)
    {
        if (suppressCallbacks)
            return;

        if (!initialized ||
            lobbyManager == null)
        {
            return;
        }

        if (dropdownIndex < 0 ||
            dropdownIndex >= seatOptions.Count)
        {
            return;
        }

        int selectedSeat =
            seatOptions[
                dropdownIndex
            ];

        // -----------------------------------------------------
        // NO SEAT
        // -----------------------------------------------------

        if (selectedSeat < 0)
        {
            lobbyManager.ClearPlayerSeat(
                playerId
            );

            return;
        }

        // -----------------------------------------------------
        // ASSIGN SEAT
        // -----------------------------------------------------

        bool success =
            lobbyManager.TryAssignSeat(
                playerId,
                selectedSeat
            );

        if (!success)
        {
            Debug.LogWarning(
                $"Player {playerId} could not take " +
                $"Seat {selectedSeat}."
            );

            Refresh();
        }
    }

    // =========================================================
    // READY BUTTON
    // =========================================================

    private void HandleReadyClicked()
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

        if (player.SeatIndex <= 0 ||
            player.TeamId <= 0)
        {
            return;
        }

        lobbyManager.SetPlayerReady(
            playerId,
            !player.IsReady
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