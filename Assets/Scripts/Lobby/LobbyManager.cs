using System;
using System.Collections.Generic;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private GameManager gameManager;

    // =========================================================
    // LOBBY SETTINGS
    // =========================================================

    [Header("Lobby Settings")]
    [SerializeField] private int playerCount = 6;
    [SerializeField] private int teamCount = 2;

    // =========================================================
    // LOCAL TESTING
    // =========================================================

    [Header("Local Lobby Testing")]
    [SerializeField] private bool autoCreateLocalTestLobby = false;

    [SerializeField] private bool autoReadyLocalPlayers = false;

    [SerializeField] private bool autoStartLocalMatch = false;

    // =========================================================
    // LOBBY DATA
    // =========================================================

    private readonly List<LobbyPlayerData> players =
        new List<LobbyPlayerData>();

    public IReadOnlyList<LobbyPlayerData> Players =>
        players;

    public int PlayerCount =>
        playerCount;

    public int TeamCount =>
        teamCount;

    // =========================================================
    // EVENTS
    //
    // We will use this when we build the Lobby UI.
    // =========================================================

    public event Action OnLobbyChanged;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (!autoCreateLocalTestLobby)
            return;

        CreateLocalTestLobby();
    }

    // =========================================================
    // RESET / CONFIGURE LOBBY
    // =========================================================

    public bool ConfigureLobby(
        int newPlayerCount,
        int newTeamCount)
    {
        if (!IsSupportedPlayerCount(
                newPlayerCount))
        {
            Debug.LogError(
                $"Unsupported lobby player count: " +
                $"{newPlayerCount}"
            );

            return false;
        }

        if (newTeamCount != 2 &&
            newTeamCount != 3)
        {
            Debug.LogError(
                $"Unsupported team count: " +
                $"{newTeamCount}"
            );

            return false;
        }

        if (newPlayerCount %
            newTeamCount != 0)
        {
            Debug.LogError(
                $"{newPlayerCount} players cannot be " +
                $"divided evenly into " +
                $"{newTeamCount} teams."
            );

            return false;
        }

        playerCount =
            newPlayerCount;

        teamCount =
            newTeamCount;

        players.Clear();

        Debug.Log(
            $"Lobby configured for " +
            $"{playerCount} players and " +
            $"{teamCount} teams."
        );

        NotifyLobbyChanged();

        return true;
    }

    // =========================================================
    // JOIN
    // =========================================================

    public bool JoinPlayer(
        int playerId,
        string displayName)
    {
        if (playerId <= 0)
        {
            Debug.LogWarning(
                "PlayerId must be greater than zero."
            );

            return false;
        }

        if (GetPlayer(playerId) != null)
        {
            Debug.LogWarning(
                $"Player {playerId} is already " +
                "in the lobby."
            );

            return false;
        }

        if (players.Count >=
            playerCount)
        {
            Debug.LogWarning(
                "Lobby is full."
            );

            return false;
        }

        LobbyPlayerData player =
            new LobbyPlayerData(
                playerId,
                displayName
            );

        players.Add(
            player
        );

        Debug.Log(
            $"{player.DisplayName} joined the lobby " +
            $"as Player {player.PlayerId}."
        );

        NotifyLobbyChanged();

        return true;
    }

    // =========================================================
    // LEAVE
    // =========================================================

    public bool RemovePlayer(
        int playerId)
    {
        LobbyPlayerData player =
            GetPlayer(
                playerId
            );

        if (player == null)
        {
            Debug.LogWarning(
                $"Player {playerId} is not " +
                "in the lobby."
            );

            return false;
        }

        players.Remove(
            player
        );

        Debug.Log(
            $"{player.DisplayName} left the lobby."
        );

        NotifyLobbyChanged();

        return true;
    }

    // =========================================================
    // SEAT SELECTION
    // =========================================================

    public bool TryAssignSeat(
        int playerId,
        int seatIndex)
    {
        LobbyPlayerData player =
            GetPlayer(
                playerId
            );

        if (player == null)
        {
            Debug.LogWarning(
                $"Cannot assign seat. " +
                $"Player {playerId} was not found."
            );

            return false;
        }

        if (seatIndex < 1 ||
            seatIndex > playerCount)
        {
            Debug.LogWarning(
                $"Seat {seatIndex} is invalid."
            );

            return false;
        }

        LobbyPlayerData seatOccupant =
            GetPlayerInSeat(
                seatIndex
            );

        if (seatOccupant != null &&
            seatOccupant.PlayerId !=
            playerId)
        {
            Debug.LogWarning(
                $"Seat {seatIndex} is already " +
                $"occupied by Player " +
                $"{seatOccupant.PlayerId}."
            );

            return false;
        }

        player.AssignSeat(
            seatIndex,
            teamCount
        );

        Debug.Log(
            $"{player.DisplayName} -> " +
            $"Seat {player.SeatIndex}, " +
            $"Team {player.TeamId}"
        );

        NotifyLobbyChanged();

        return true;
    }

    // =========================================================
    // CLEAR SEAT
    // =========================================================

    public bool ClearPlayerSeat(
        int playerId)
    {
        LobbyPlayerData player =
            GetPlayer(
                playerId
            );

        if (player == null)
            return false;

        player.ClearSeat();

        Debug.Log(
            $"{player.DisplayName} left their seat."
        );

        NotifyLobbyChanged();

        return true;
    }

    // =========================================================
    // READY
    // =========================================================

    public bool SetPlayerReady(
        int playerId,
        bool ready)
    {
        LobbyPlayerData player =
            GetPlayer(
                playerId
            );

        if (player == null)
        {
            Debug.LogWarning(
                $"Cannot change Ready state. " +
                $"Player {playerId} was not found."
            );

            return false;
        }

        if (!player.HasSeat ||
            !player.HasTeam)
        {
            Debug.LogWarning(
                $"{player.DisplayName} must choose " +
                "a seat before becoming Ready."
            );

            return false;
        }

        player.SetReady(
            ready
        );

        Debug.Log(
            $"{player.DisplayName} Ready = " +
            $"{player.IsReady}"
        );

        NotifyLobbyChanged();

        return true;
    }

    // =========================================================
    // PLAYER LOOKUP
    // =========================================================

    public LobbyPlayerData GetPlayer(
        int playerId)
    {
        foreach (LobbyPlayerData player
                 in players)
        {
            if (player.PlayerId ==
                playerId)
            {
                return player;
            }
        }

        return null;
    }

    // =========================================================
    // SEAT LOOKUP
    // =========================================================

    public LobbyPlayerData GetPlayerInSeat(
        int seatIndex)
    {
        foreach (LobbyPlayerData player
                 in players)
        {
            if (player.SeatIndex ==
                seatIndex)
            {
                return player;
            }
        }

        return null;
    }

    // =========================================================
    // ALL READY
    // =========================================================

    public bool AreAllPlayersReady()
    {
        if (players.Count !=
            playerCount)
        {
            return false;
        }

        foreach (LobbyPlayerData player
                 in players)
        {
            if (!player.HasSeat ||
                !player.HasTeam ||
                !player.IsReady)
            {
                return false;
            }
        }

        return true;
    }

    // =========================================================
    // CAN START MATCH
    // =========================================================

    public bool CanStartMatch(
        out string errorMessage)
    {
        errorMessage =
            "";

        // -----------------------------------------------------
        // CORRECT NUMBER OF PLAYERS
        // -----------------------------------------------------

        if (players.Count !=
            playerCount)
        {
            errorMessage =
                $"Lobby requires {playerCount} players, " +
                $"but currently has {players.Count}.";

            return false;
        }

        HashSet<int> occupiedSeats =
            new HashSet<int>();

        // -----------------------------------------------------
        // PLAYER STATE
        // -----------------------------------------------------

        foreach (LobbyPlayerData player
                 in players)
        {
            if (!player.HasSeat)
            {
                errorMessage =
                    $"{player.DisplayName} has not " +
                    "selected a seat.";

                return false;
            }

            if (!player.HasTeam)
            {
                errorMessage =
                    $"{player.DisplayName} has no team.";

                return false;
            }

            if (!occupiedSeats.Add(
                    player.SeatIndex))
            {
                errorMessage =
                    $"Seat {player.SeatIndex} " +
                    "is occupied more than once.";

                return false;
            }

            if (!player.IsReady)
            {
                errorMessage =
                    $"{player.DisplayName} is not Ready.";

                return false;
            }
        }

        // -----------------------------------------------------
        // FINAL SESSION VALIDATION
        // -----------------------------------------------------

        GameSessionConfig config =
            BuildGameSessionConfig();

        if (config == null)
        {
            errorMessage =
                "Could not build GameSessionConfig.";

            return false;
        }

        string configError;

        if (!config.IsValid(
                out configError))
        {
            errorMessage =
                configError;

            return false;
        }

        return true;
    }

    // =========================================================
    // BUILD GAME SESSION CONFIG
    // =========================================================

    public GameSessionConfig BuildGameSessionConfig()
    {
        GameSessionConfig config =
            new GameSessionConfig(
                playerCount,
                teamCount
            );

        foreach (LobbyPlayerData player
                 in players)
        {
            config.AddPlayer(
                player.PlayerId,
                player.SeatIndex,
                player.TeamId
            );
        }

        return config;
    }

    // =========================================================
    // START MATCH
    // =========================================================

    public bool StartMatch()
    {
        if (gameManager == null)
        {
            Debug.LogError(
                "LobbyManager GameManager reference " +
                "is missing."
            );

            return false;
        }

        string errorMessage;

        if (!CanStartMatch(
                out errorMessage))
        {
            Debug.LogWarning(
                "Cannot start match: " +
                errorMessage
            );

            return false;
        }

        GameSessionConfig config =
            BuildGameSessionConfig();

        Debug.LogWarning(
            "Lobby validated. Starting match..."
        );

        PrintLobbyConfiguration();

        gameManager.StartGame(
            config
        );

        return true;
    }

    // =========================================================
    // LOCAL TEST LOBBY
    // =========================================================

    private void CreateLocalTestLobby()
    {
        if (!ConfigureLobby(
                playerCount,
                teamCount))
        {
            return;
        }

        // -----------------------------------------------------
        // CREATE PLAYERS
        // -----------------------------------------------------

        for (int i = 1;
             i <= playerCount;
             i++)
        {
            JoinPlayer(
                i,
                $"Player {i}"
            );

            TryAssignSeat(
                i,
                i
            );

            if (autoReadyLocalPlayers)
            {
                SetPlayerReady(
                    i,
                    true
                );
            }
        }

        Debug.LogWarning(
            "Local test lobby created."
        );

        PrintLobbyConfiguration();

        // -----------------------------------------------------
        // OPTIONAL AUTO START
        // -----------------------------------------------------

        if (autoStartLocalMatch)
        {
            StartMatch();
        }
    }

    // =========================================================
    // PRINT LOBBY
    // =========================================================

    public void PrintLobbyConfiguration()
    {
        Debug.Log(
            "========== LOBBY =========="
        );

        for (int seatIndex = 1;
             seatIndex <= playerCount;
             seatIndex++)
        {
            LobbyPlayerData player =
                GetPlayerInSeat(
                    seatIndex
                );

            if (player == null)
            {
                Debug.Log(
                    $"Seat {seatIndex}: EMPTY"
                );

                continue;
            }

            Debug.Log(
                $"Seat {player.SeatIndex} -> " +
                $"Player {player.PlayerId} " +
                $"({player.DisplayName}) -> " +
                $"Team {player.TeamId} -> " +
                $"Ready: {player.IsReady}"
            );
        }

        Debug.Log(
            "==========================="
        );
    }

    // =========================================================
    // LOBBY CHANGE
    // =========================================================

    private void NotifyLobbyChanged()
    {
        OnLobbyChanged?.Invoke();
    }

    // =========================================================
    // SUPPORTED PLAYER COUNTS
    // =========================================================

    private bool IsSupportedPlayerCount(
        int count)
    {
        return
            count == 2 ||
            count == 3 ||
            count == 4 ||
            count == 6 ||
            count == 8 ||
            count == 9 ||
            count == 10 ||
            count == 12;
    }
}