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

    [Header("Panels")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject boardPanel;
    [SerializeField] private GameObject handPanel;
    [SerializeField] private GameObject gameStatusPanel;

    [Header("Lobby Settings")]
    [SerializeField] private TMP_Dropdown playerCountDropdown;
    [SerializeField] private TMP_Dropdown teamCountDropdown;

    [Header("Player List")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private LobbyPlayerSlotUI playerSlotPrefab;

    [Header("Status")]
    [SerializeField] private TMP_Text lobbyMessageText;

    [Header("Buttons")]
    [SerializeField] private Button startGameButton;

    // =========================================================
    // GENERATED ROWS
    // =========================================================

    private readonly List<LobbyPlayerSlotUI> spawnedSlots =
        new List<LobbyPlayerSlotUI>();

    // =========================================================
    // UNITY
    // =========================================================

    private void OnEnable()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyChanged +=
                RefreshLobbyUI;
        }

        if (playerCountDropdown != null)
        {
            playerCountDropdown.onValueChanged.AddListener(
                HandleLobbySettingsChanged
            );
        }

        if (teamCountDropdown != null)
        {
            teamCountDropdown.onValueChanged.AddListener(
                HandleLobbySettingsChanged
            );
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(
                HandleStartGameClicked
            );
        }
    }

    private void Start()
    {
        ShowLobby();

        ConfigureLobbyFromDropdowns();
    }

    private void OnDisable()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyChanged -=
                RefreshLobbyUI;
        }

        if (playerCountDropdown != null)
        {
            playerCountDropdown.onValueChanged.RemoveListener(
                HandleLobbySettingsChanged
            );
        }

        if (teamCountDropdown != null)
        {
            teamCountDropdown.onValueChanged.RemoveListener(
                HandleLobbySettingsChanged
            );
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(
                HandleStartGameClicked
            );
        }
    }

    // =========================================================
    // DROPDOWN CHANGED
    // =========================================================

    private void HandleLobbySettingsChanged(
        int ignoredValue)
    {
        ConfigureLobbyFromDropdowns();
    }

    // =========================================================
    // CONFIGURE LOBBY
    // =========================================================

    private void ConfigureLobbyFromDropdowns()
    {
        if (lobbyManager == null)
            return;

        int playerCount =
            GetSelectedPlayerCount();

        int teamCount =
            GetSelectedTeamCount();

        bool success =
            lobbyManager.ConfigureLobby(
                playerCount,
                teamCount
            );

        if (!success)
        {
            SetLobbyMessage(
                "Invalid player/team combination."
            );

            ClearPlayerRows();

            return;
        }

        CreateLocalLobbyPlayers();

        RebuildPlayerRows();

        RefreshLobbyUI();
    }

    // =========================================================
    // CREATE TEMPORARY LOCAL LOBBY PLAYERS
    //
    // Later networking will replace this.
    // =========================================================

    private void CreateLocalLobbyPlayers()
    {
        if (lobbyManager == null)
            return;

        for (int playerId = 1;
             playerId <= lobbyManager.PlayerCount;
             playerId++)
        {
            lobbyManager.JoinPlayer(
                playerId,
                $"Player {playerId}"
            );
        }
    }

    // =========================================================
    // REBUILD PLAYER ROWS
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
                player.PlayerId
            );

            spawnedSlots.Add(
                slot
            );
        }
    }

    // =========================================================
    // CLEAR PLAYER ROWS
    // =========================================================

    private void ClearPlayerRows()
    {
        foreach (LobbyPlayerSlotUI slot
                 in spawnedSlots)
        {
            if (slot != null)
            {
                Destroy(
                    slot.gameObject
                );
            }
        }

        spawnedSlots.Clear();

        if (playerListContent == null)
            return;

        // Also clean any leftover generated children.
        for (int i =
                 playerListContent.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                playerListContent.GetChild(i);

            Destroy(
                child.gameObject
            );
        }
    }

    // =========================================================
    // REFRESH
    // =========================================================

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

        string errorMessage;

        bool canStart =
            lobbyManager.CanStartMatch(
                out errorMessage
            );

        if (startGameButton != null)
        {
            startGameButton.interactable =
                canStart;
        }

        if (canStart)
        {
            SetLobbyMessage(
                "All players ready. Match can start."
            );
        }
        else
        {
            SetLobbyMessage(
                errorMessage
            );
        }
    }

    // =========================================================
    // START GAME
    // =========================================================

    private void HandleStartGameClicked()
    {
        if (lobbyManager == null)
            return;

        bool started =
            lobbyManager.StartMatch();

        if (!started)
        {
            RefreshLobbyUI();

            return;
        }

        ShowGame();
    }

    // =========================================================
    // PLAYER COUNT DROPDOWN
    // =========================================================

    private int GetSelectedPlayerCount()
    {
        if (playerCountDropdown == null)
            return 2;

        string selectedText =
            playerCountDropdown.options[
                playerCountDropdown.value
            ].text;

        int result;

        if (int.TryParse(
                selectedText,
                out result))
        {
            return result;
        }

        return 2;
    }

    // =========================================================
    // TEAM COUNT DROPDOWN
    // =========================================================

    private int GetSelectedTeamCount()
    {
        if (teamCountDropdown == null)
            return 2;

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

        if (string.IsNullOrWhiteSpace(
                message))
        {
            lobbyMessageText.text =
                "Waiting for players...";
        }
        else
        {
            lobbyMessageText.text =
                message;
        }
    }

    // =========================================================
    // SHOW LOBBY
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

    // =========================================================
    // SHOW GAME
    // =========================================================

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