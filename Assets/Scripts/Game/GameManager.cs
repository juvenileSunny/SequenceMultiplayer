using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int playerCount = 3;

    [Header("Managers")]
    [SerializeField] private HandManager handManager;
    [SerializeField] private BoardManager boardManager;

    private Deck deck;

    private readonly List<Player> players =
        new List<Player>();

    private int currentPlayerId = 1;

    public IReadOnlyList<Player> Players => players;

    private void OnEnable()
    {
        if (handManager != null)
        {
            handManager.OnSelectedCardChanged +=
                HandleSelectedCardChanged;
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
    // GAME INITIALIZATION
    // =========================================================

    private void InitializeGame()
    {
        if (!IsSupportedPlayerCount(playerCount))
        {
            Debug.LogError(
                $"Unsupported player count: {playerCount}"
            );

            return;
        }

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

        for (int i = 1; i <= playerCount; i++)
        {
            players.Add(
                new Player(i)
            );
        }

        Debug.Log(
            $"Created {players.Count} players."
        );
    }

    public Player GetPlayer(int playerId)
    {
        foreach (Player player in players)
        {
            if (player.PlayerId == playerId)
                return player;
        }

        return null;
    }

    // =========================================================
    // DECK
    // =========================================================

    private void CreateDeck()
    {
        deck = new Deck();

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
            GetCardsPerPlayer(playerCount);

        // Deal one card to each player per round.
        for (int round = 0;
             round < cardsPerPlayer;
             round++)
        {
            foreach (Player player in players)
            {
                Card card = deck.Draw();

                player.AddCard(card);
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
    // HAND UI
    // =========================================================

    public void ShowPlayerHand(int playerId)
    {
        Player player =
            GetPlayer(playerId);

        if (player == null)
            return;

        handManager.Initialize(player);

        Debug.Log(
            $"Showing Player {playerId}'s hand."
        );
    }

    // =========================================================
    // HAND SELECTION
    // =========================================================

    private void HandleSelectedCardChanged(
        Card card)
    {
        if (card == null)
        {
            boardManager.ClearHighlights();
            return;
        }

        boardManager.HighlightMatchingCard(card);

        Debug.Log(
            $"Showing legal positions for {card.GetCode()}."
        );
    }

    // =========================================================
    // SUCCESSFUL MOVE
    // =========================================================

    private void HandleMoveCompleted(
        Card playedCard)
    {
        Player currentPlayer =
            GetPlayer(currentPlayerId);

        if (currentPlayer == null)
            return;

        // Remove played card from hand.
        currentPlayer.RemoveCard(
            playedCard
        );

        Debug.Log(
            $"Removed {playedCard.GetCode()} " +
            $"from Player {currentPlayerId}'s hand."
        );

        // Draw replacement card.
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

        AdvanceTurn();
    }

    // =========================================================
    // TURN MANAGEMENT
    // =========================================================

    private void AdvanceTurn()
    {
        currentPlayerId++;

        if (currentPlayerId > players.Count)
        {
            currentPlayerId = 1;
        }

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
    // HAND SIZE
    // =========================================================

    private int GetCardsPerPlayer(int count)
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

    private bool IsSupportedPlayerCount(int count)
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