using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerSlotUI : MonoBehaviour
{
    // =========================================================
    // UI REFERENCES
    // =========================================================

    [Header("Player")]
    [SerializeField] private TMP_Text playerNameText;

    [Header("Seat")]
    [SerializeField] private TMP_Dropdown seatDropdown;

    [Header("Team")]
    [SerializeField] private TMP_Text teamText;

    [Header("Ready")]
    [SerializeField] private TMP_Text readyStatusText;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;

    // =========================================================
    // DATA
    // =========================================================

    private LobbyManager lobbyManager;

    private int playerId = -1;

    private bool initialized = false;

    private bool suppressCallbacks = false;

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
                Refresh;
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
                Refresh;
        }

        lobbyManager =
            manager;

        playerId =
            newPlayerId;

        if (lobbyManager == null)
        {
            Debug.LogError(
                "LobbyPlayerSlotUI received a null LobbyManager."
            );

            return;
        }

        lobbyManager.OnLobbyChanged +=
            Refresh;

        BuildSeatDropdown();

        initialized =
            true;

        Refresh();
    }

    // =========================================================
    // BUILD SEAT DROPDOWN
    // =========================================================

    private void BuildSeatDropdown()
    {
        if (seatDropdown == null ||
            lobbyManager == null)
        {
            return;
        }

        suppressCallbacks =
            true;

        seatDropdown.ClearOptions();

        List<string> options =
            new List<string>();

        options.Add(
            "NO SEAT"
        );

        for (int seat = 1;
             seat <= lobbyManager.PlayerCount;
             seat++)
        {
            options.Add(
                $"SEAT {seat}"
            );
        }

        seatDropdown.AddOptions(
            options
        );

        seatDropdown.value =
            0;

        seatDropdown.RefreshShownValue();

        suppressCallbacks =
            false;
    }

    // =========================================================
    // REFRESH UI
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
        {
            return;
        }

        // -----------------------------------------------------
        // PLAYER NAME
        // -----------------------------------------------------

        if (playerNameText != null)
        {
            playerNameText.text =
                player.DisplayName;
        }

        // -----------------------------------------------------
        // SEAT
        // -----------------------------------------------------

        if (seatDropdown != null)
        {
            suppressCallbacks =
                true;

            seatDropdown.value =
                player.HasSeat
                    ? player.SeatIndex
                    : 0;

            seatDropdown.RefreshShownValue();

            suppressCallbacks =
                false;
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

        if (readyButton != null)
        {
            readyButton.interactable =
                player.HasSeat &&
                player.HasTeam;
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
        int optionIndex)
    {
        if (suppressCallbacks)
            return;

        if (!initialized ||
            lobbyManager == null)
        {
            return;
        }

        // Option 0 = NO SEAT
        if (optionIndex == 0)
        {
            lobbyManager.ClearPlayerSeat(
                playerId
            );

            return;
        }

        // Dropdown option index matches seat number:
        //
        // 0 = NO SEAT
        // 1 = SEAT 1
        // 2 = SEAT 2
        // ...
        int requestedSeat =
            optionIndex;

        bool success =
            lobbyManager.TryAssignSeat(
                playerId,
                requestedSeat
            );

        // If the seat was already occupied,
        // return the dropdown to the actual current seat.
        if (!success)
        {
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