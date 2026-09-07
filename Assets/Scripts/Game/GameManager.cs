using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Game Settings")]
    [SerializeField] private int playerCount = 2;
    [SerializeField] private int teamCount = 2;

    [Header("Managers")]
    [SerializeField] private HandManager handManager;
    [SerializeField] private BoardManager boardManager;

    [Header("UI")]
    [SerializeField] private GameStatusUI gameStatusUI;

    // =========================================================
    // GAME DATA
    // =========================================================

    private Deck deck;

    private readonly List<Player> players =
        new List<Player>();

    private int currentPlayerId = 1;

    private bool deadCardReplacedThisTurn = false;
    private bool gameOver = false;

    public IReadOnlyList<Player> Players => players;

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

    private void Start()
    {
        InitializeGame();
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void InitializeGame()
    {
        if (handManager == null ||
            boardManager == null)
        {
            Debug.LogError(
                "GameManager manager references are missing."
            );

            return;
        }

        if (gameStatusUI == null)
        {
            Debug.LogWarning(
                "GameStatusUI reference is not assigned."
            );
        }

        if (!IsSupportedPlayerCount(
                playerCount))
        {
            Debug.LogError(
                $"Unsupported player count: {playerCount}"
            );

            return;
        }

        if (!IsSupportedTeamSetup(
                playerCount,
                teamCount))
        {
            Debug.LogError(
                $"Invalid setup: {playerCount} players " +
                $"cannot be divided evenly into " +
                $"{teamCount} teams."
            );

            return;
        }

        gameOver = false;

        deadCardReplacedThisTurn = false;

        boardManager.ResetSequenceData();

        boardManager.SetBoardLocked(
            false
        );

        CreatePlayers();

        CreateDeck();

        DealCards();

        // Start with Seat 1.
        Player firstPlayer =
            GetPlayerBySeat(1);

        if (firstPlayer == null)
        {
            Debug.LogError(
                "No player assigned to Seat 1."
            );

            return;
        }

        currentPlayerId =
            firstPlayer.PlayerId;

        SetBoardForCurrentPlayer();

        ShowPlayerHand(
            currentPlayerId
        );

        // NEW
        UpdateGameStatusUI();

        Debug.Log(
            $"Game initialized with {playerCount} players " +
            $"and {teamCount} teams."
        );
    }

    // =========================================================
    // PLAYERS
    // =========================================================

    private void CreatePlayers()
    {
        players.Clear();

        for (int playerId = 1;
             playerId <= playerCount;
             playerId++)
        {
            Player player =
                new Player(
                    playerId
                );

            players.Add(
                player
            );
        }

        AssignTemporarySeatsAndTeams();

        Debug.Log(
            $"Created {players.Count} players " +
            $"across {teamCount} teams."
        );
    }

    // =========================================================
    // TEMPORARY SEAT / TEAM ASSIGNMENT
    // =========================================================

    private void AssignTemporarySeatsAndTeams()
    {
        for (int i = 0;
             i < players.Count;
             i++)
        {
            Player player =
                players[i];

            int seatIndex =
                i + 1;

            int teamId =
                (i % teamCount) + 1;

            player.AssignSeat(
                seatIndex
            );

            player.AssignTeam(
                teamId
            );

            Debug.Log(
                $"Player {player.PlayerId} -> " +
                $"Seat {player.SeatIndex}, " +
                $"Team {player.TeamId}"
            );
        }
    }

    // =========================================================
    // PLAYER LOOKUP
    // =========================================================

    public Player GetPlayer(
        int playerId)
    {
        foreach (Player player in players)
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
        foreach (Player player in players)
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

        Player currentPlayer =
            GetCurrentPlayer();

        if (currentPlayer == null)
            return;

        int team1Sequences =
            boardManager.GetSequenceCount(1);

        int team2Sequences =
            boardManager.GetSequenceCount(2);

        int team3Sequences =
            boardManager.GetSequenceCount(3);

        int sequencesNeeded =
            GetSequencesNeededToWin();

        gameStatusUI.UpdateStatus(
            currentPlayer,
            teamCount,
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
            $"Deck shuffled. Cards remaining: {deck.Count}"
        );
    }

    // =========================================================
    // DEALING
    // =========================================================

    private void DealCards()
    {
        int cardsPerPlayer =
            GetCardsPerPlayer(
                playerCount
            );

        for (int round = 0;
             round < cardsPerPlayer;
             round++)
        {
            foreach (Player player in players)
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

        foreach (Player player in players)
        {
            Debug.Log(
                $"Player {player.PlayerId}: " +
                $"{player.Hand.Count} cards"
            );
        }

        Debug.Log(
            $"Cards remaining in deck: {deck.Count}"
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

        currentPlayer.RemoveCard(
            deadCard
        );

        Debug.Log(
            $"Player {currentPlayer.PlayerId} " +
            $"discarded dead card " +
            $"{deadCard.GetCode()}."
        );

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
                "Deck is empty. No replacement card drawn."
            );
        }

        deadCardReplacedThisTurn =
            true;

        boardManager.ClearHighlights();

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

                // NEW
                // Immediately update the counters.
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
                "Deck is empty. No replacement card drawn."
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
                $"No player found in Seat {nextSeat}."
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

        // NEW
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
        if (teamCount == 3)
        {
            return 1;
        }

        return 2;
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

        // NEW
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
    // PLAYER COUNT VALIDATION
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

    // =========================================================
    // TEAM SETUP VALIDATION
    // =========================================================

    private bool IsSupportedTeamSetup(
        int players,
        int teams)
    {
        if (teams != 2 &&
            teams != 3)
        {
            return false;
        }

        if (players % teams != 0)
        {
            return false;
        }

        return true;
    }



    // =========================================================
    // DEBUG / DEVELOPMENT TESTING
    // =========================================================

    public void DebugCreateSequenceForCurrentTeam()
    {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD

        if (gameOver)
        {
            Debug.LogWarning(
                "DEBUG: Game is already over."
            );

            return;
        }

        Player currentPlayer =
            GetCurrentPlayer();

        if (currentPlayer == null)
        {
            Debug.LogError(
                "DEBUG: Current player not found."
            );

            return;
        }

        int teamId =
            currentPlayer.TeamId;

        int created =
            boardManager.DebugCreateNextSequenceForTeam(
                teamId
            );

        if (created <= 0)
        {
            Debug.LogWarning(
                $"DEBUG: No new Sequence created " +
                $"for Team {teamId}."
            );

            return;
        }

        // Update on-screen counter.
        UpdateGameStatusUI();

        int totalSequences =
            boardManager.GetSequenceCount(
                teamId
            );

        Debug.LogWarning(
            $"DEBUG: Team {teamId} now has " +
            $"{totalSequences} Sequence(s)."
        );

        int sequencesNeeded =
            GetSequencesNeededToWin();

        if (totalSequences >=
            sequencesNeeded)
        {
            EndGame(
                teamId
            );
        }

    #endif
    }
}