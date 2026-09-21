using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Managers")]
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private LobbyRequestGateway requestGateway;
    [SerializeField] private NetworkLobbyBridge networkLobbyBridge;

    [Header("Session")]
    [SerializeField] private RoomSessionContext roomSessionContext;

    [Header("Network Match State")]
    [SerializeField]
    private NetworkMatchState networkMatchState;

    [Header("Panels")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject boardPanel;
    [SerializeField] private GameObject handPanel;
    [SerializeField] private GameObject gameStatusPanel;

    [Header("Lobby Settings")]
    [SerializeField] private TMP_Dropdown playerCountDropdown;
    [SerializeField] private TMP_Dropdown teamCountDropdown;

    [Header("Player List")]
    [SerializeField] private RectTransform playerListContent;
    [SerializeField] private LobbyPlayerSlotUI playerSlotPrefab;
    [SerializeField] private GridLayoutGroup playerGridLayout;

    [Header("Status")]
    [SerializeField] private TMP_Text lobbyMessageText;

    [Header("Buttons")]
    [SerializeField] private Button startGameButton;

    private readonly List<LobbyPlayerSlotUI> spawnedSlots =
        new List<LobbyPlayerSlotUI>();

    // Prevent a network-driven dropdown refresh from being
    // interpreted as a brand-new local settings request.
    private bool applyingNetworkConfiguration = false;

    // =========================================================
    // UNITY
    // =========================================================

    private void OnEnable()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyChanged += RefreshLobbyUI;
        }

        if (networkLobbyBridge != null)
        {
            networkLobbyBridge.OnLobbyConfigurationChanged +=
                HandleLobbyConfigurationChanged;
        }

        if (playerCountDropdown != null)
        {
            playerCountDropdown.onValueChanged.AddListener(
                HandlePlayerCountChanged
            );
        }

        if (teamCountDropdown != null)
        {
            teamCountDropdown.onValueChanged.AddListener(
                HandleTeamCountChanged
            );
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(
                HandleStartGameClicked
            );
        }
        if (roomSessionContext != null)
        {
            roomSessionContext.OnSessionChanged +=
                HandleSessionChanged;
        }
        if (networkMatchState != null)
        {
            networkMatchState.OnMatchPhaseChanged +=
                HandleMatchPhaseChanged;
        }
    }

    private void Start()
    {
        ShowLobby();

        // The lobby/server state is authoritative.
        // Do not create an independent local configuration
        // from this client's dropdown values.
        ApplyAuthoritativeConfigurationToUI();
    }

    private void OnDisable()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyChanged -= RefreshLobbyUI;
        }

        if (networkLobbyBridge != null)
        {
            networkLobbyBridge.OnLobbyConfigurationChanged -=
                HandleLobbyConfigurationChanged;
        }

        if (playerCountDropdown != null)
        {
            playerCountDropdown.onValueChanged.RemoveListener(
                HandlePlayerCountChanged
            );
        }

        if (teamCountDropdown != null)
        {
            teamCountDropdown.onValueChanged.RemoveListener(
                HandleTeamCountChanged
            );
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(
                HandleStartGameClicked
            );
        }

        if (roomSessionContext != null)
        {
            roomSessionContext.OnSessionChanged -=
                HandleSessionChanged;
        }

        if (networkMatchState != null)
        {
            networkMatchState.OnMatchPhaseChanged -=
                HandleMatchPhaseChanged;
        }
    }
    private void HandleSessionChanged()
    {
        ApplyAuthoritativeConfigurationToUI();
        RefreshLobbyUI();
    }

    // =========================================================
    // PLAYER COUNT CHANGED
    // =========================================================

    private void HandlePlayerCountChanged(int ignoredValue)
    {
        if (applyingNetworkConfiguration)
            return;

        // Player count determines which team counts are valid.
        UpdateValidTeamOptions();

        RequestLobbyConfigurationFromDropdowns();
    }

    // =========================================================
    // TEAM COUNT CHANGED
    // =========================================================

    private void HandleTeamCountChanged(int ignoredValue)
    {
        if (applyingNetworkConfiguration)
            return;

        RequestLobbyConfigurationFromDropdowns();
    }

    // =========================================================
    // VALID TEAM OPTIONS
    // =========================================================

    private void UpdateValidTeamOptions(
        int preferredTeamCount = -1)
    {
        if (playerCountDropdown == null ||
            teamCountDropdown == null)
        {
            return;
        }

        int playerCount =
            GetSelectedPlayerCount();

        // Preserve the current team choice unless the caller
        // supplied the authoritative server choice.
        int previousTeamCount =
            preferredTeamCount > 0
                ? preferredTeamCount
                : GetSelectedTeamCount();

        List<int> validTeamCounts =
            new List<int>();

        // Two-team games require equal team sizes.
        if (playerCount % 2 == 0)
        {
            validTeamCounts.Add(2);
        }

        // Three-team games require equal team sizes.
        if (playerCount % 3 == 0)
        {
            validTeamCounts.Add(3);
        }

        List<string> options =
            new List<string>();

        foreach (int teamCount in validTeamCounts)
        {
            options.Add($"{teamCount} TEAMS");
        }

        teamCountDropdown.ClearOptions();
        teamCountDropdown.AddOptions(options);

        // Try to preserve the previous choice.
        int selectedIndex = 0;

        for (int i = 0;
             i < validTeamCounts.Count;
             i++)
        {
            if (validTeamCounts[i] ==
                previousTeamCount)
            {
                selectedIndex = i;
                break;
            }
        }

        teamCountDropdown.SetValueWithoutNotify(
            selectedIndex
        );

        teamCountDropdown.RefreshShownValue();
    }

    // =========================================================
    // REQUEST LOBBY CONFIGURATION
    // =========================================================

    private void RequestLobbyConfigurationFromDropdowns()
    {
        if (networkLobbyBridge == null ||
            !networkLobbyBridge.IsSpawned)
        {
            SetLobbyMessage(
                "Lobby networking is not ready."
            );

            ApplyAuthoritativeConfigurationToUI();

            return;
        }

        int playerCount =
            GetSelectedPlayerCount();

        int teamCount =
            GetSelectedTeamCount();

        networkLobbyBridge.RequestLobbyConfiguration(
            playerCount,
            teamCount,
            HandleLobbyConfigurationRequestCompleted
        );
    }

    private void HandleLobbyConfigurationRequestCompleted(
        AuthorityResult result)
    {
        if (result == null)
        {
            SetLobbyMessage(
                "Lobby settings request returned no result."
            );

            ApplyAuthoritativeConfigurationToUI();

            return;
        }

        if (!result.Success)
        {
            Debug.LogWarning(
                $"Lobby settings rejected: " +
                $"{result.Code} - {result.Message}"
            );

            // Restore whatever the SERVER currently says.
            ApplyAuthoritativeConfigurationToUI();

            SetLobbyMessage(
                result.Message
            );

            return;
        }

        SetLobbyMessage(
            result.Message
        );
    }

    // =========================================================
    // AUTHORITATIVE LOBBY CONFIGURATION RECEIVED
    // =========================================================

    private void HandleLobbyConfigurationChanged(
        int playerCount,
        int teamCount)
    {
        ApplyLobbyConfigurationToUI(
            playerCount,
            teamCount
        );
    }

    private void ApplyAuthoritativeConfigurationToUI()
    {
        int playerCount =
            networkLobbyBridge != null
                ? networkLobbyBridge.LobbyPlayerCount
                : (lobbyManager != null
                    ? lobbyManager.PlayerCount
                    : 2);

        int teamCount =
            networkLobbyBridge != null
                ? networkLobbyBridge.LobbyTeamCount
                : (lobbyManager != null
                    ? lobbyManager.TeamCount
                    : 2);

        ApplyLobbyConfigurationToUI(
            playerCount,
            teamCount
        );
    }

    private void ApplyLobbyConfigurationToUI(
        int playerCount,
        int teamCount)
    {
        applyingNetworkConfiguration =
            true;

        SetPlayerCountDropdownWithoutNotify(
            playerCount
        );

        UpdateValidTeamOptions(
            teamCount
        );

        applyingNetworkConfiguration =
            false;

        ApplyPlayerGridLayout(
            playerCount
        );

        // NetworkLobbyBridge configures LobbyManager before
        // firing OnLobbyConfigurationChanged. Rebuild the rows
        // now so every client shows the same number of slots.
        RebuildPlayerRows();

        RefreshLobbyUI();
    }

    private void SetPlayerCountDropdownWithoutNotify(
        int playerCount)
    {
        if (playerCountDropdown == null)
            return;

        for (int i = 0;
             i < playerCountDropdown.options.Count;
             i++)
        {
            string optionText =
                playerCountDropdown.options[i]
                    .text
                    .Trim();

            if (!int.TryParse(
                    optionText,
                    out int optionPlayerCount))
            {
                continue;
            }

            if (optionPlayerCount !=
                playerCount)
            {
                continue;
            }

            playerCountDropdown.SetValueWithoutNotify(
                i
            );

            playerCountDropdown.RefreshShownValue();

            return;
        }

        Debug.LogWarning(
            $"LobbyUI has no Player Count dropdown " +
            $"option for {playerCount}."
        );
    }

    // =========================================================
    // PLAYER GRID
    // =========================================================

    private void ApplyPlayerGridLayout(
        int playerCount)
    {
        if (playerGridLayout == null ||
            playerListContent == null)
        {
            return;
        }

        int columnCount =
            playerCount <= 6
                ? 1
                : 2;

        playerGridLayout.constraint =
            GridLayoutGroup.Constraint.FixedColumnCount;

        playerGridLayout.constraintCount =
            columnCount;

        Canvas.ForceUpdateCanvases();

        float availableWidth =
            playerListContent.rect.width;

        if (availableWidth <= 1f &&
            playerListContent.parent
                is RectTransform parentRect)
        {
            availableWidth =
                parentRect.rect.width;
        }

        float horizontalPadding =
            playerGridLayout.padding.left +
            playerGridLayout.padding.right;

        float totalSpacing =
            playerGridLayout.spacing.x *
            (columnCount - 1);

        float usableWidth =
            availableWidth -
            horizontalPadding -
            totalSpacing;

        float cellWidth =
            usableWidth /
            columnCount;

        playerGridLayout.cellSize =
            new Vector2(
                cellWidth,
                65f
            );
    }

    // =========================================================
    // PLAYER ROWS
    // =========================================================

    private void RebuildPlayerRows()
    {
        ClearPlayerRows();

        if (lobbyManager == null ||
            playerSlotPrefab == null ||
            playerListContent == null)
        {
            return;
        }

        foreach (LobbyPlayerData player
                 in lobbyManager.Players)
        {
            LobbyPlayerSlotUI slot =
                Instantiate(
                    playerSlotPrefab,
                    playerListContent
                );

            slot.Initialize(
                lobbyManager,
                requestGateway,
                roomSessionContext,
                player.PlayerId
            );

            spawnedSlots.Add(slot);
        }
    }

    private void ClearPlayerRows()
    {
        foreach (LobbyPlayerSlotUI slot
                 in spawnedSlots)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }

        spawnedSlots.Clear();

        if (playerListContent == null)
            return;

        for (int i =
                 playerListContent.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                playerListContent.GetChild(i);

            Destroy(child.gameObject);
        }
    }

    // =========================================================
    // REFRESH
    // =========================================================
    private void HandleMatchPhaseChanged(
        MatchPhase previousPhase,
        MatchPhase newPhase)
    {
        Debug.Log(
            $"LobbyUI received MatchPhase: " +
            $"{previousPhase} -> {newPhase}"
        );

        if (newPhase == MatchPhase.Playing)
        {
            ShowGame();
        }
    }
    private void RefreshLobbyUI()
    {
        if (lobbyManager == null)
            return;

        foreach (LobbyPlayerSlotUI slot
                 in spawnedSlots)
        {
            if (slot != null)
            {
                slot.Refresh();
            }
        }

        bool canStart =
            lobbyManager.CanStartMatch(
                out string errorMessage
            );

        bool localPlayerIsHost =
            roomSessionContext != null &&
            roomSessionContext.IsLocalPlayerHost;

        if (startGameButton != null)
        {
            startGameButton.interactable =
                canStart &&
                localPlayerIsHost;
        }

        if (!canStart)
        {
            SetLobbyMessage(
                errorMessage
            );
        }
        else if (localPlayerIsHost)
        {
            SetLobbyMessage(
                "All players are ready. You can start the match."
            );
        }
        else
        {
            SetLobbyMessage(
                "All players are ready. Waiting for the host to start the match."
            );
        }
    }

    // =========================================================
    // START GAME
    // =========================================================

    private void HandleStartGameClicked()
    {
        if (requestGateway == null)
        {
            Debug.LogError(
                "LobbyUI: LobbyRequestGateway is missing."
            );

            SetLobbyMessage(
                "Lobby connection is unavailable."
            );

            return;
        }

        // =====================================================
        // SEND START REQUEST
        //
        // LobbyUI does not know:
        //
        // - who the authority implementation is
        // - whether it is local
        // - whether it is remote
        //
        // It simply requests that the match be started.
        // =====================================================

        requestGateway.RequestStartMatch(
            HandleStartMatchCompleted
        );
    }

    private void HandleStartMatchCompleted(
        AuthorityResult result)
    {
        if (result == null)
        {
            Debug.LogWarning(
                "Start-match request returned no result."
            );

            SetLobbyMessage(
                "Unable to start the match."
            );

            return;
        }

        // =====================================================
        // REQUEST REJECTED
        // =====================================================

        if (!result.Success)
        {
            Debug.LogWarning(
                $"Start-match request rejected: " +
                $"{result.Code} - {result.Message}"
            );

            SetLobbyMessage(
                result.Message
            );

            return;
        }

        // =====================================================
        // REQUEST ACCEPTED
        // =====================================================

        Debug.Log(
            $"Start-match request accepted: " +
            $"{result.Message}"
        );

        // ShowGame();
    }

    // =========================================================
    // PLAYER COUNT
    // =========================================================

    private int GetSelectedPlayerCount()
    {
        if (playerCountDropdown == null)
            return 2;

        string selectedText =
            playerCountDropdown.options[
                playerCountDropdown.value
            ].text;

        if (int.TryParse(
                selectedText,
                out int result))
        {
            return result;
        }

        return 2;
    }

    // =========================================================
    // TEAM COUNT
    // =========================================================

    private int GetSelectedTeamCount()
    {
        if (teamCountDropdown == null ||
            teamCountDropdown.options.Count == 0)
        {
            return 2;
        }

        string selectedText =
            teamCountDropdown.options[
                teamCountDropdown.value
            ].text;

        if (selectedText.StartsWith("3"))
            return 3;

        return 2;
    }

    // =========================================================
    // MESSAGE
    // =========================================================

    private void SetLobbyMessage(
        string message)
    {
        if (lobbyMessageText == null)
            return;

        lobbyMessageText.text =
            string.IsNullOrWhiteSpace(message)
                ? "Waiting for players..."
                : message;
    }

    // =========================================================
    // PANELS
    // =========================================================

    private void ShowLobby()
    {
        if (lobbyPanel != null)
            lobbyPanel.SetActive(true);

        if (boardPanel != null)
            boardPanel.SetActive(false);

        if (handPanel != null)
            handPanel.SetActive(false);

        if (gameStatusPanel != null)
            gameStatusPanel.SetActive(false);
    }

    private void ShowGame()
    {
        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);

        if (boardPanel != null)
            boardPanel.SetActive(true);

        if (handPanel != null)
            handPanel.SetActive(true);

        if (gameStatusPanel != null)
            gameStatusPanel.SetActive(true);
    }
}