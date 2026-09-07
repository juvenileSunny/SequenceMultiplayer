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

    [Header("Managers")]
    [SerializeField] private HandManager handManager;
    [SerializeField] private BoardManager boardManager;

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
        // Check manager references FIRST.
        if (handManager == null ||
            boardManager == null)
        {
            Debug.LogError(
                "GameManager manager references are missing."
            );

            return;
        }

        if (!IsSupportedPlayerCount(playerCount))
        {
            Debug.LogError(
                $"Unsupported player count: {playerCount}"
            );

            return;
        }

        gameOver = false;

        deadCardReplacedThisTurn = false;
        boardManager.ResetSequenceData();

        boardManager.SetBoardLocked(false);

        CreatePlayers();

        CreateDeck();

        DealCards();

        currentPlayerId = 1;

        boardManager.SetCurrentPlayer(
            currentPlayerId
        );

        ShowPlayerHand(
            currentPlayerId
        );

        Debug.Log(
            $"Game initialized with {playerCount} players."
        );
    }

    // =========================================================
    // PLAYERS
    // =========================================================

    private void CreatePlayers()
    {
        players.Clear();

        for (int i = 1;
             i <= playerCount;
             i++)
        {
            Player player =
                new Player(i);

            players.Add(
                player
            );
        }

        Debug.Log(
            $"Created {players.Count} players."
        );
    }

    public Player GetPlayer(
        int playerId)
    {
        foreach (Player player in players)
        {
            if (player.PlayerId == playerId)
            {
                return player;
            }
        }

        return null;
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

        // Deal one card to every player each round.
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
            $"Showing Player {playerId}'s hand."
        );
    }

    // =========================================================
    // CARD SELECTION
    // =========================================================

    private void HandleSelectedCardChanged(
        Card card)
    {
        // Do not allow interaction after game over.
        if (gameOver)
        {
            boardManager.ClearHighlights();

            handManager.SetDeadCardButtonState(
                false
            );

            return;
        }

        // Nothing selected.
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
                    "place a chip on any empty space."
                );
            }
            else if (card.IsOneEyedJack())
            {
                Debug.Log(
                    $"{card.GetCode()} selected: " +
                    "remove an opponent's chip."
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
            $"Showing legal positions for {card.GetCode()}."
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

        // Only one dead-card replacement
        // is allowed during a turn.
        if (deadCardReplacedThisTurn)
        {
            Debug.Log(
                "Dead-card replacement already used this turn."
            );

            return;
        }

        // Always verify again against current board state.
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
            GetPlayer(
                currentPlayerId
            );

        if (currentPlayer == null)
            return;

        // -----------------------------------------------------
        // REMOVE DEAD CARD
        // -----------------------------------------------------

        currentPlayer.RemoveCard(
            deadCard
        );

        Debug.Log(
            $"Player {currentPlayerId} discarded dead card " +
            $"{deadCard.GetCode()}."
        );

        // -----------------------------------------------------
        // DRAW REPLACEMENT
        // -----------------------------------------------------

        Card replacementCard =
            deck.Draw();

        if (replacementCard != null)
        {
            currentPlayer.AddCard(
                replacementCard
            );

            Debug.Log(
                $"Player {currentPlayerId} drew replacement " +
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

        // IMPORTANT:
        // Same player continues their normal turn.
        ShowPlayerHand(
            currentPlayerId
        );

        handManager.SetDeadCardButtonState(
            false
        );

        Debug.Log(
            $"Player {currentPlayerId} continues their turn."
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
            GetPlayer(
                currentPlayerId
            );

        if (currentPlayer == null)
            return;

        // -----------------------------------------------------
        // CONSUME PLAYED CARD
        // -----------------------------------------------------

        currentPlayer.RemoveCard(
            playedCard
        );

        Debug.Log(
            $"Player {currentPlayerId} used " +
            $"{playedCard.GetCode()}."
        );

        // =====================================================
        // SEQUENCE CHECK
        // =====================================================

        /*
         * placedCell is:
         *
         * Normal card:
         *     board position where chip was placed
         *
         * Two-eyed Jack:
         *     board position where chip was placed
         *
         * One-eyed Jack:
         *     null because no chip was placed
         */

        if (placedCell != null)
        {
            int newSequences =
                boardManager.RegisterNewSequences(
                    currentPlayerId,
                    placedCell
                );

            if (newSequences > 0)
            {
                int totalSequences =
                    boardManager.GetSequenceCount(
                        currentPlayerId
                    );

                Debug.Log(
                    $"Player {currentPlayerId} completed " +
                    $"{newSequences} new Sequence(s)."
                );

                Debug.Log(
                    $"Player {currentPlayerId} now has " +
                    $"{totalSequences} total Sequence(s)."
                );

                int sequencesNeeded =
                    GetSequencesNeededToWin();

                if (totalSequences >=
                    sequencesNeeded)
                {
                    EndGame(
                        currentPlayerId
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
                $"Player {currentPlayerId} drew " +
                $"{replacementCard.GetCode()}."
            );
        }
        else
        {
            Debug.Log(
                "Deck is empty. No replacement card drawn."
            );
        }

        // Successful play ends the player's turn.
        AdvanceTurn();
    }

    // =========================================================
    // TURN MANAGEMENT
    // =========================================================

    private void AdvanceTurn()
    {
        if (gameOver)
            return;

        currentPlayerId++;

        if (currentPlayerId >
            players.Count)
        {
            currentPlayerId =
                1;
        }

        // New player gets their own opportunity
        // to replace one dead card.
        deadCardReplacedThisTurn =
            false;

        boardManager.ClearHighlights();

        boardManager.SetCurrentPlayer(
            currentPlayerId
        );

        ShowPlayerHand(
            currentPlayerId
        );

        Debug.Log(
            $"Player {currentPlayerId}'s turn."
        );
    }

    // =========================================================
    // SEQUENCE WIN REQUIREMENT
    // =========================================================
    private int GetSequencesNeededToWin()
    {
        /*
         * Current LOCAL player implementation:
         *
         * 2 players:
         *     2 completed Sequences required
         *
         * 3 players:
         *     1 completed Sequence required
         *
         * IMPORTANT:
         *
         * When teams are implemented,
         * this should use the number of TEAMS,
         * not simply playerCount.
         */

        if (playerCount == 3)
        {
            return 1;
        }

        return 2;
    }

    // =========================================================
    // GAME OVER
    // =========================================================

    private void EndGame(
        int winnerId)
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

        Debug.LogWarning(
            $"PLAYER {winnerId} WINS THE GAME!"
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
}