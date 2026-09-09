using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // =========================================================
    // LOCAL TEST SETTINGS
    // =========================================================

    [Header("Local Test Settings")]
    [SerializeField] private bool autoStartLocalTestGame = true;

    [SerializeField] private int localTestPlayerCount = 6;
    [SerializeField] private int localTestTeamCount = 2;

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Managers")]
    [SerializeField] private HandManager handManager;
    [SerializeField] private BoardManager boardManager;

    [Header("UI")]
    [SerializeField] private GameStatusUI gameStatusUI;

    // =========================================================
    // GAME DATA
    // =========================================================

    private GameSessionConfig sessionConfig;

    private Deck deck;

    private readonly List<Player> players =
        new List<Player>();

    private int currentPlayerId = -1;

    private bool deadCardReplacedThisTurn = false;
    private bool gameOver = false;

    // =========================================================
    // PUBLIC DATA
    // =========================================================

    public IReadOnlyList<Player> Players =>
        players;

    public GameSessionConfig SessionConfig =>
        sessionConfig;

    // =========================================================
    // EVENTS
    // =========================================================

    private void OnEnable()
    {
        if (handManager != null)
        {
            handManager.OnSelectedCardChanged +=
                HandleSelectedCardChanged;

            handManager.OnDeadCardRequested +=
                HandleDeadCardRequested;
        }

        if (boardManager != null)
        {
            boardManager.OnMoveCompleted +=
                HandleMoveCompleted;
        }
    }

    private void OnDisable()
    {
        if (handManager != null)
        {
            handManager.OnSelectedCardChanged -=
                HandleSelectedCardChanged;

            handManager.OnDeadCardRequested -=
                HandleDeadCardRequested;
        }

        if (boardManager != null)
        {
            boardManager.OnMoveCompleted -=
                HandleMoveCompleted;
        }
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (!autoStartLocalTestGame)
            return;

        GameSessionConfig localConfig =
            CreateLocalTestConfig(
                localTestPlayerCount,
                localTestTeamCount
            );

        StartGame(
            localConfig
        );
    }

    // =========================================================
    // PUBLIC GAME START
    // =========================================================

    public void StartGame(
        GameSessionConfig config)
    {
        // -----------------------------------------------------
        // REFERENCES
        // -----------------------------------------------------

        if (handManager == null ||
            boardManager == null)
        {
            Debug.LogError(
                "GameManager manager references are missing."
            );

            return;
        }

        // -----------------------------------------------------
        // CONFIG
        // -----------------------------------------------------

        if (config == null)
        {
            Debug.LogError(
                "Cannot start game. " +
                "GameSessionConfig is null."
            );

            return;
        }

        string validationError;

        if (!config.IsValid(
                out validationError))
        {
            Debug.LogError(
                "Invalid GameSessionConfig: " +
                validationError
            );

            return;
        }

        // Config becomes the authoritative setup
        // for this local game instance.
        sessionConfig =
            config;

        // -----------------------------------------------------
        // RESET GAME STATE
        // -----------------------------------------------------

        gameOver =
            false;

        deadCardReplacedThisTurn =
            false;

        boardManager.ResetSequenceData();

        boardManager.SetBoardLocked(
            false
        );

        // -----------------------------------------------------
        // CREATE MATCH
        // -----------------------------------------------------

        CreatePlayersFromConfig(
            sessionConfig
        );

        CreateDeck();

        DealCards();

        // -----------------------------------------------------
        // FIRST TURN = SEAT 1
        // -----------------------------------------------------

        Player firstPlayer =
            GetPlayerBySeat(
                1
            );

        if (firstPlayer == null)
        {
            Debug.LogError(
                "Cannot start game. " +
                "No player occupies Seat 1."
            );

            return;
        }

        currentPlayerId =
            firstPlayer.PlayerId;

        SetBoardForCurrentPlayer();

        ShowPlayerHand(
            currentPlayerId
        );

        UpdateGameStatusUI();

        Debug.Log(
            $"Game started: " +
            $"{sessionConfig.PlayerCount} players, " +
            $"{sessionConfig.TeamCount} teams."
        );
    }

    // =========================================================
    // TEMPORARY LOCAL CONFIG
    // =========================================================

    private GameSessionConfig CreateLocalTestConfig(
        int playerCount,
        int teamCount)
    {
        GameSessionConfig config =
            new GameSessionConfig(
                playerCount,
                teamCount
            );

        // Avoid modulo-by-zero if someone puts
        // an invalid value in the Inspector.
        if (teamCount <= 0)
            return config;

        for (int seatIndex = 1;
             seatIndex <= playerCount;
             seatIndex++)
        {
            // Current local testing uses:
            //
            // Player 1 -> Seat 1
            // Player 2 -> Seat 2
            // etc.
            //
            // This is ONLY the local test configuration.
            int playerId =
                seatIndex;

            int teamId =
                ((seatIndex - 1) %
                    teamCount) + 1;

            config.AddPlayer(
                playerId,
                seatIndex,
                teamId
            );
        }

        return config;
    }

    // =========================================================
    // CREATE PLAYERS FROM SESSION CONFIG
    // =========================================================

    private void CreatePlayersFromConfig(
        GameSessionConfig config)
    {
        players.Clear();

        foreach (PlayerSlotData slot
                 in config.PlayerSlots)
        {
            Player player =
                new Player(
                    slot.PlayerId
                );

            player.AssignSeat(
                slot.SeatIndex
            );

            player.AssignTeam(
                slot.TeamId
            );

            players.Add(
                player
            );

            Debug.Log(
                $"Player {player.PlayerId} -> " +
                $"Seat {player.SeatIndex}, " +
                $"Team {player.TeamId}"
            );
        }

        Debug.Log(
            $"Created {players.Count} players " +
            $"from GameSessionConfig."
        );
    }

    // =========================================================
    // PLAYER LOOKUP
    // =========================================================

    public Player GetPlayer(
        int playerId)
    {
        foreach (Player player
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

    private Player GetPlayerBySeat(
        int seatIndex)
    {
        foreach (Player player
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

    private Player GetCurrentPlayer()
    {
        return GetPlayer(
            currentPlayerId
        );
    }

    // =========================================================
    // BOARD CURRENT PLAYER
    // =========================================================

    private void SetBoardForCurrentPlayer()
    {
        Player currentPlayer =
            GetCurrentPlayer();

        if (currentPlayer == null)
        {
            Debug.LogError(
                $"Could not find current Player " +
                $"{currentPlayerId}."
            );

            return;
        }

        boardManager.SetCurrentPlayer(
            currentPlayer.PlayerId,
            currentPlayer.TeamId
        );
    }

    // =========================================================
    // GAME STATUS UI
    // =========================================================

    private void UpdateGameStatusUI()
    {
        if (gameStatusUI == null)
            return;

        if (sessionConfig == null)
            return;

        Player currentPlayer =
            GetCurrentPlayer();

        if (currentPlayer == null)
            return;

        int team1Sequences =
            boardManager.GetSequenceCount(
                1
            );

        int team2Sequences =
            boardManager.GetSequenceCount(
                2
            );

        int team3Sequences =
            boardManager.GetSequenceCount(
                3
            );

        int sequencesNeeded =
            GetSequencesNeededToWin();

        gameStatusUI.UpdateStatus(
            currentPlayer,
            sessionConfig.TeamCount,
            team1Sequences,
            team2Sequences,
            team3Sequences,
            sequencesNeeded
        );
    }

    // =========================================================
    // DECK
    // =========================================================

    private void CreateDeck()
    {
        deck =
            new Deck();

        deck.Shuffle();

        Debug.Log(
            $"Deck shuffled. Cards remaining: " +
            $"{deck.Count}"
        );
    }

    // =========================================================
    // DEALING
    // =========================================================

    private void DealCards()
    {
        if (sessionConfig == null)
            return;

        int cardsPerPlayer =
            GetCardsPerPlayer(
                sessionConfig.PlayerCount
            );

        for (int round = 0;
             round < cardsPerPlayer;
             round++)
        {
            foreach (Player player
                     in players)
            {
                Card card =
                    deck.Draw();

                if (card != null)
                {
                    player.AddCard(
                        card
                    );
                }
            }
        }

        foreach (Player player
                 in players)
        {
            Debug.Log(
                $"Player {player.PlayerId}: " +
                $"{player.Hand.Count} cards"
            );
        }

        Debug.Log(
            $"Cards remaining in deck: " +
            $"{deck.Count}"
        );
    }

    // =========================================================
    // HAND DISPLAY
    // =========================================================

    public void ShowPlayerHand(
        int playerId)
    {
        Player player =
            GetPlayer(
                playerId
            );

        if (player == null)
        {
            Debug.LogWarning(
                $"Could not find Player {playerId}."
            );

            return;
        }

        handManager.Initialize(
            player
        );

        Debug.Log(
            $"Showing Player {player.PlayerId}'s hand " +
            $"(Team {player.TeamId}, " +
            $"Seat {player.SeatIndex})."
        );
    }

    // =========================================================
    // CARD SELECTION
    // =========================================================

    private void HandleSelectedCardChanged(
        Card card)
    {
        if (gameOver)
        {
            boardManager.ClearHighlights();

            handManager.SetDeadCardButtonState(
                false
            );

            return;
        }

        if (card == null)
        {
            boardManager.ClearHighlights();

            handManager.SetDeadCardButtonState(
                false
            );

            return;
        }

        // =====================================================
        // JACK
        // =====================================================

        if (card.IsJack())
        {
            handManager.SetDeadCardButtonState(
                false
            );

            boardManager.HighlightMatchingCard(
                card
            );

            if (card.IsTwoEyedJack())
            {
                Debug.Log(
                    $"{card.GetCode()} selected: " +
                    "place a team chip on any empty space."
                );
            }
            else if (card.IsOneEyedJack())
            {
                Debug.Log(
                    $"{card.GetCode()} selected: " +
                    "remove an opponent team's chip."
                );
            }

            return;
        }

        // =====================================================
        // DEAD CARD
        // =====================================================

        bool isDead =
            boardManager.IsDeadCard(
                card
            );

        if (isDead)
        {
            boardManager.ClearHighlights();

            bool canReplace =
                !deadCardReplacedThisTurn;

            handManager.SetDeadCardButtonState(
                canReplace
            );

            if (canReplace)
            {
                Debug.Log(
                    $"{card.GetCode()} is a dead card. " +
                    "It may be replaced."
                );
            }
            else
            {
                Debug.Log(
                    $"{card.GetCode()} is dead, but a " +
                    "dead card has already been replaced " +
                    "this turn."
                );
            }

            return;
        }

        // =====================================================
        // NORMAL CARD
        // =====================================================

        handManager.SetDeadCardButtonState(
            false
        );

        boardManager.HighlightMatchingCard(
            card
        );

        Debug.Log(
            $"Showing legal positions for " +
            $"{card.GetCode()}."
        );
    }

    // =========================================================
    // DEAD CARD REPLACEMENT
    // =========================================================

    private void HandleDeadCardRequested(
        Card deadCard)
    {
        if (gameOver)
            return;

        if (deadCard == null)
            return;

        if (deadCardReplacedThisTurn)
        {
            Debug.Log(
                "Dead-card replacement already used this turn."
            );

            return;
        }

        if (!boardManager.IsDeadCard(
                deadCard))
        {
            Debug.Log(
                $"{deadCard.GetCode()} is not a dead card."
            );

            handManager.SetDeadCardButtonState(
                false
            );

            return;
        }

        Player currentPlayer =
            GetCurrentPlayer();

        if (currentPlayer == null)
            return;

        // Remove dead card.
        currentPlayer.RemoveCard(
            deadCard
        );

        Debug.Log(
            $"Player {currentPlayer.PlayerId} " +
            $"discarded dead card " +
            $"{deadCard.GetCode()}."
        );

        // Draw replacement.
        Card replacementCard =
            deck.Draw();

        if (replacementCard != null)
        {
            currentPlayer.AddCard(
                replacementCard
            );

            Debug.Log(
                $"Player {currentPlayer.PlayerId} " +
                $"drew replacement " +
                $"{replacementCard.GetCode()}."
            );
        }
        else
        {
            Debug.Log(
                "Deck is empty. " +
                "No replacement card drawn."
            );
        }

        deadCardReplacedThisTurn =
            true;

        boardManager.ClearHighlights();

        // Player continues their normal turn.
        ShowPlayerHand(
            currentPlayerId
        );

        handManager.SetDeadCardButtonState(
            false
        );

        Debug.Log(
            $"Player {currentPlayer.PlayerId} " +
            $"(Team {currentPlayer.TeamId}) " +
            "continues their turn."
        );
    }

    // =========================================================
    // SUCCESSFUL BOARD PLAY
    // =========================================================

    private void HandleMoveCompleted(
        Card playedCard,
        BoardCell placedCell)
    {
        if (gameOver)
            return;

        if (playedCard == null)
            return;

        Player currentPlayer =
            GetCurrentPlayer();

        if (currentPlayer == null)
        {
            Debug.LogError(
                $"Could not find current Player " +
                $"{currentPlayerId}."
            );

            return;
        }

        int currentTeamId =
            currentPlayer.TeamId;

        // =====================================================
        // CONSUME PLAYED CARD
        // =====================================================

        currentPlayer.RemoveCard(
            playedCard
        );

        Debug.Log(
            $"Player {currentPlayer.PlayerId} " +
            $"(Team {currentTeamId}) used " +
            $"{playedCard.GetCode()}."
        );

        // =====================================================
        // SEQUENCE CHECK
        // =====================================================

        if (placedCell != null)
        {
            int newSequences =
                boardManager.RegisterNewSequences(
                    currentTeamId,
                    placedCell
                );

            if (newSequences > 0)
            {
                int totalSequences =
                    boardManager.GetSequenceCount(
                        currentTeamId
                    );

                Debug.Log(
                    $"Team {currentTeamId} completed " +
                    $"{newSequences} new Sequence(s)."
                );

                Debug.Log(
                    $"Team {currentTeamId} now has " +
                    $"{totalSequences} total Sequence(s)."
                );

                UpdateGameStatusUI();

                int sequencesNeeded =
                    GetSequencesNeededToWin();

                if (totalSequences >=
                    sequencesNeeded)
                {
                    EndGame(
                        currentTeamId
                    );

                    return;
                }
            }
        }

        // =====================================================
        // DRAW REPLACEMENT
        // =====================================================

        Card replacementCard =
            deck.Draw();

        if (replacementCard != null)
        {
            currentPlayer.AddCard(
                replacementCard
            );

            Debug.Log(
                $"Player {currentPlayer.PlayerId} drew " +
                $"{replacementCard.GetCode()}."
            );
        }
        else
        {
            Debug.Log(
                "Deck is empty. " +
                "No replacement card drawn."
            );
        }

        AdvanceTurn();
    }

    // =========================================================
    // TURN MANAGEMENT
    // =========================================================

    private void AdvanceTurn()
    {
        if (gameOver)
            return;

        Player currentPlayer =
            GetCurrentPlayer();

        if (currentPlayer == null)
        {
            Debug.LogError(
                "Cannot advance turn because the " +
                "current player does not exist."
            );

            return;
        }

        // Turn order is based ONLY on SeatIndex.
        int nextSeat =
            currentPlayer.SeatIndex + 1;

        if (nextSeat >
            players.Count)
        {
            nextSeat =
                1;
        }

        Player nextPlayer =
            GetPlayerBySeat(
                nextSeat
            );

        if (nextPlayer == null)
        {
            Debug.LogError(
                $"No player found in Seat " +
                $"{nextSeat}."
            );

            return;
        }

        currentPlayerId =
            nextPlayer.PlayerId;

        deadCardReplacedThisTurn =
            false;

        boardManager.ClearHighlights();

        SetBoardForCurrentPlayer();

        ShowPlayerHand(
            currentPlayerId
        );

        UpdateGameStatusUI();

        Debug.Log(
            $"Player {nextPlayer.PlayerId}'s turn " +
            $"(Team {nextPlayer.TeamId}, " +
            $"Seat {nextPlayer.SeatIndex})."
        );
    }

    // =========================================================
    // SEQUENCE WIN REQUIREMENT
    // =========================================================

    private int GetSequencesNeededToWin()
    {
        if (sessionConfig == null)
        {
            Debug.LogWarning(
                "No GameSessionConfig found. Defaulting to 2 sequences."
            );

            return 2;
        }

        int players = sessionConfig.PlayerCount;
        int teams = sessionConfig.TeamCount;

        // =========================================================
        // 2 TEAM GAMES
        // =========================================================

        if (teams == 2)
        {
            switch (players)
            {
                case 2:
                case 4:
                case 6:
                    return 3;

                case 8:
                case 10:
                case 12:
                    return 2;
            }
        }

        // =========================================================
        // 3 TEAM GAMES
        // =========================================================

        if (teams == 3)
        {
            switch (players)
            {
                case 3:
                case 6:
                    return 3;

                case 9:
                    return 2;

                case 12:
                    return 1;
            }
        }

        Debug.LogError(
            $"Unsupported win-condition configuration: " +
            $"{players} players / {teams} teams."
        );

        return int.MaxValue;
    }
    // =========================================================
    // GAME OVER
    // =========================================================

    private void EndGame(
        int winnerTeamId)
    {
        if (gameOver)
            return;

        gameOver =
            true;

        boardManager.ClearHighlights();

        boardManager.SetBoardLocked(
            true
        );

        handManager.SetDeadCardButtonState(
            false
        );

        if (gameStatusUI != null)
        {
            gameStatusUI.ShowWinner(
                winnerTeamId
            );
        }

        Debug.LogWarning(
            $"TEAM {winnerTeamId} WINS THE GAME!"
        );
    }

    // =========================================================
    // HAND SIZE
    // =========================================================

    private int GetCardsPerPlayer(
        int count)
    {
        switch (count)
        {
            case 2:
                return 7;

            case 3:
            case 4:
                return 6;

            case 6:
                return 5;

            case 8:
            case 9:
                return 4;

            case 10:
            case 12:
                return 3;

            default:
                throw new ArgumentException(
                    $"Unsupported player count: {count}"
                );
        }
    }

    // =========================================================
    // DEBUG / DEVELOPMENT TESTING
    // =========================================================

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void DebugCreateSequenceForCurrentTeam()
    {
        if (gameOver)
        {
            Debug.Log(
                "DEBUG: Game is already over."
            );

            return;
        }

        if (boardManager == null)
        {
            Debug.LogWarning(
                "DEBUG: BoardManager is missing."
            );

            return;
        }

        Player currentPlayer =
            GetCurrentPlayer();

        if (currentPlayer == null)
        {
            Debug.LogWarning(
                "DEBUG: There is no current player."
            );

            return;
        }

        int teamId =
            currentPlayer.TeamId;

        int beforeCount =
            boardManager.GetSequenceCount(
                teamId
            );

        // This method returns void.
        boardManager.DebugCreateNextSequenceForTeam(
            teamId
        );

        int sequenceCount =
            boardManager.GetSequenceCount(
                teamId
            );

        int created =
            sequenceCount - beforeCount;

        int sequencesNeeded =
            GetSequencesNeededToWin();

        Debug.Log(
            $"DEBUG: Team {teamId} created " +
            $"{created} Sequence(s). " +
            $"Total = {sequenceCount}/{sequencesNeeded}."
        );

        UpdateGameStatusUI();

        if (sequenceCount >= sequencesNeeded)
        {
            EndGame(
                teamId
            );
        }
    }
    #endif
}