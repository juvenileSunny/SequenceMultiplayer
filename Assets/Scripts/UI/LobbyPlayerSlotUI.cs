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
    private LobbyRequestGateway requestGateway;
    private RoomSessionContext roomSessionContext;

    private int playerId = -1;

    private bool initialized = false;
    private bool suppressCallbacks = false;

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

        if (roomSessionContext != null)
        {
            roomSessionContext.OnSessionChanged -=
                HandleSessionChanged;
        }
    }

    // =========================================================
    // INITIALIZE
    // =========================================================

    public void Initialize(
        LobbyManager manager,
        LobbyRequestGateway gateway,
        RoomSessionContext sessionContext,
        int newPlayerId)
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyChanged -=
                HandleLobbyChanged;
        }

        if (roomSessionContext != null)
        {
            roomSessionContext.OnSessionChanged -=
                HandleSessionChanged;
        }

        lobbyManager =
            manager;

        requestGateway =
            gateway;

        roomSessionContext =
            sessionContext;

        playerId =
            newPlayerId;

        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyChanged +=
                HandleLobbyChanged;
        }

        if (roomSessionContext != null)
        {
            roomSessionContext.OnSessionChanged +=
                HandleSessionChanged;
        }

        initialized =
            true;

        Refresh();
    }

    // =========================================================
    // EVENTS
    // =========================================================

    private void HandleLobbyChanged()
    {
        Refresh();
    }

    private void HandleSessionChanged()
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

        suppressCallbacks =
            true;

        seatDropdown.ClearOptions();
        seatOptions.Clear();

        List<string> labels =
            new List<string>();

        labels.Add(
            "NO SEAT"
        );

        seatOptions.Add(
            -1
        );

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

        int selectedDropdownIndex =
            0;

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

        suppressCallbacks =
            false;
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

        // =====================================================
        // IS THIS THE PLAYER REPRESENTED BY THIS UNITY CLIENT?
        // =====================================================

        bool isLocalPlayerRow =
            roomSessionContext != null &&
            roomSessionContext.HasLocalPlayer &&
            roomSessionContext.LocalPlayerId ==
            playerId;

        // -----------------------------------------------------
        // PLAYER NAME
        // -----------------------------------------------------

        if (playerNameText != null)
        {
            string displayName =
                !string.IsNullOrWhiteSpace(
                    player.DisplayName)
                    ? player.DisplayName.ToUpper()
                    : $"PLAYER {player.PlayerId}";

            bool isHostPlayer =
                roomSessionContext != null &&
                roomSessionContext.HasHost &&
                roomSessionContext.HostPlayerId ==
                player.PlayerId;

            if (isHostPlayer)
            {
                displayName += "  [HOST]";
            }

            playerNameText.text =
                displayName;
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
        // SEAT CONTROL
        // -----------------------------------------------------

        if (seatDropdown != null)
        {
            // Only the local player may manipulate
            // their own seat selector.
            seatDropdown.interactable =
                isLocalPlayerRow;
        }

        // -----------------------------------------------------
        // READY CONTROL
        // -----------------------------------------------------

        bool hasValidSeat =
            player.SeatIndex > 0 &&
            player.TeamId > 0;

        if (readyButton != null)
        {
            readyButton.interactable =
                isLocalPlayerRow &&
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

        if (!IsThisLocalPlayer())
            return;

        if (requestGateway == null)
        {
            Debug.LogError(
                "LobbyPlayerSlotUI: " +
                "LobbyRequestGateway is missing."
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

        requestGateway.RequestSeat(
            requestedSeat,
            HandleSeatRequestCompleted
        );
    }

    // =========================================================
    // SEAT RESULT
    // =========================================================

    private void HandleSeatRequestCompleted(
        AuthorityResult result)
    {
        if (result == null)
        {
            Debug.LogWarning(
                "Seat request returned no result."
            );

            Refresh();

            return;
        }

        if (!result.Success)
        {
            Debug.LogWarning(
                $"Seat request rejected: " +
                $"{result.Code} - " +
                $"{result.Message}"
            );

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

        if (!IsThisLocalPlayer())
            return;

        if (requestGateway == null ||
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

        requestGateway.RequestReadyState(
            !player.IsReady,
            HandleReadyRequestCompleted
        );
    }

    // =========================================================
    // READY RESULT
    // =========================================================

    private void HandleReadyRequestCompleted(
        AuthorityResult result)
    {
        if (result == null)
        {
            Debug.LogWarning(
                "Ready request returned no result."
            );

            Refresh();

            return;
        }

        if (!result.Success)
        {
            Debug.LogWarning(
                $"Ready request rejected: " +
                $"{result.Code} - " +
                $"{result.Message}"
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
    // LOCAL PLAYER CHECK
    // =========================================================

    private bool IsThisLocalPlayer()
    {
        return
            roomSessionContext != null &&
            roomSessionContext.HasLocalPlayer &&
            roomSessionContext.LocalPlayerId ==
            playerId;
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