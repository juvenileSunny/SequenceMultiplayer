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

    public IReadOnlyList<Player> Players => players;

    private void Start()
    {
        InitializeGame();
    }

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

        ShowPlayerHand(1);

        Debug.Log(
            $"Game initialized with {playerCount} players."
        );
    }

    // --------------------------------------------------
    // PLAYERS
    // --------------------------------------------------

    private void CreatePlayers()
    {
        players.Clear();

        for (int i = 1; i <= playerCount; i++)
        {
            Player player = new Player(i);

            players.Add(player);
        }

        Debug.Log(
            $"Created {players.Count} players."
        );
    }

    // --------------------------------------------------
    // DECK
    // --------------------------------------------------

    private void CreateDeck()
    {
        deck = new Deck();

        deck.Shuffle();

        Debug.Log(
            $"Deck shuffled. Cards: {deck.Count}"
        );
    }

    // --------------------------------------------------
    // DEALING
    // --------------------------------------------------

    private void DealCards()
    {
        int cardsPerPlayer =
            GetCardsPerPlayer(playerCount);

        // Deal round-by-round
        for (int round = 0; round < cardsPerPlayer; round++)
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

    // --------------------------------------------------
    // HAND UI
    // --------------------------------------------------
    private void OnEnable()
    {
        if (handManager != null)
        {
            handManager.OnSelectedCardChanged +=
                HandleSelectedCardChanged;
        }
    }
    private void OnDisable()
    {
        if (handManager != null)
        {
            handManager.OnSelectedCardChanged -=
                HandleSelectedCardChanged;
        }
    }
    private void HandleSelectedCardChanged(Card card)
    {
        if (card == null)
        {
            boardManager.ClearHighlights();
            return;
        }

        boardManager.HighlightMatchingCard(card);
    }

    public void ShowPlayerHand(int playerId)
    {
        Player player =
            GetPlayer(playerId);

        if (player == null)
        {
            Debug.LogError(
                $"Player {playerId} does not exist."
            );

            return;
        }

        handManager.Initialize(player);

        Debug.Log(
            $"Showing Player {playerId}'s hand."
        );
    }

    public Player GetPlayer(int playerId)
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

    // --------------------------------------------------
    // HAND SIZE RULES
    // --------------------------------------------------

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