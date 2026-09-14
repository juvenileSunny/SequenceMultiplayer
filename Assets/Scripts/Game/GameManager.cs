using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // =========================================================
    // LOCAL TEST SETTINGS
    // =========================================================

    [Header("Local Test Settings")]
    [SerializeField] private bool autoStartLocalTestGame = false;

    [SerializeField] private int localTestPlayerCount = 6;
    [SerializeField] private int localTestTeamCount = 2;

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Managers")]
    [SerializeField] private HandManager handManager;
    [SerializeField] private BoardManager boardManager;

    [Header("Network State")]
    [SerializeField]
    private NetworkGameState networkGameState;
    [SerializeField]
    private NetworkHandState networkHandState;
    [SerializeField]
    private RoomSessionContext roomSessionContext;
    [SerializeField]
    private NetworkGameplayBridge networkGameplayBridge;

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
    // LOCAL PLAYER HAND
    // =========================================================

    private void ShowLocalPlayerHand()
    {
        if (roomSessionContext == null)
        {
            Debug.LogWarning(
                "GameManager: RoomSessionContext is missing."
            );

            return;
        }

        if (!roomSessionContext.HasLocalPlayer)
        {
            Debug.LogWarning(
                "GameManager: Local player identity is unavailable."
            );

            return;
        }

        int localPlayerId =
            roomSessionContext.LocalPlayerId;

        Player localPlayer =
            GetPlayer(localPlayerId);

        if (localPlayer == null)
        {
            // This is normal on a remote client for now,
            // because only the server currently creates the
            // authoritative Player objects.
            Debug.Log(
                $"No authoritative Player object exists locally " +
                $"for Player {localPlayerId}."
            );

            return;
        }

        handManager.Initialize(
            localPlayer
        );

        Debug.Log(
            $"Showing LOCAL Player {localPlayer.PlayerId}'s hand " +
            $"(Team {localPlayer.TeamId}, " +
            $"Seat {localPlayer.SeatIndex})."
        );
    }

    private void RefreshLocalHandIfOwnedBy(
        int playerId)
    {
        if (roomSessionContext == null)
            return;

        if (!roomSessionContext.HasLocalPlayer)
            return;

        if (roomSessionContext.LocalPlayerId !=
            playerId)
        {
            return;
        }

        ShowLocalPlayerHand();
    }
    // =========================================================
    // LOCAL NETWORK INPUT
    //
    // True only when this computer owns the player
    // whose turn it currently is.
    // =========================================================

    private bool localTurnInputEnabled = false;

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
    #if UNITY_EDITOR || DEVELOPMENT_BUILD

    private void Update()
    {
        // During a network game, only the authoritative
        // server is allowed to change player hands.
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening &&
            !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        // F2 = Two-eyed Jack, Clubs
        if (Input.GetKeyDown(KeyCode.F2))
        {
            DebugGiveCurrentPlayerCard("JC");
        }

        // F3 = Two-eyed Jack, Diamonds
        if (Input.GetKeyDown(KeyCode.F3))
        {
            DebugGiveCurrentPlayerCard("JD");
        }

        // F4 = One-eyed Jack, Hearts
        if (Input.GetKeyDown(KeyCode.F4))
        {
            DebugGiveCurrentPlayerCard("JH");
        }

        // F5 = One-eyed Jack, Spades
        if (Input.GetKeyDown(KeyCode.F5))
        {
            DebugGiveCurrentPlayerCard("JS");
        }
    }

    #endif

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

        // Send each remote player only their own
        // server-authoritative private hand.
        if (networkHandState != null)
        {
            networkHandState.SendInitialHands();
        }

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

        ShowLocalPlayerHand();
        // Publish the authoritative first turn.
        SyncCurrentTurnToNetwork();
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
    // LOCAL TURN INPUT
    // =========================================================

    public void SetLocalTurnInputEnabled(
        bool enabled)
    {
        bool wasEnabled =
            localTurnInputEnabled;

        localTurnInputEnabled =
            enabled;

        // A transition from another player's turn
        // back to this local player's turn starts
        // a fresh dead-card replacement allowance.
        if (enabled &&
            !wasEnabled)
        {
            deadCardReplacedThisTurn =
                false;
        }

        if (!enabled)
        {
            if (boardManager != null)
            {
                boardManager.ClearHighlights();
            }

            if (handManager != null)
            {
                handManager.SetDeadCardButtonState(
                    false
                );
            }
        }

        Debug.Log(
            $"Local turn input = {enabled}"
        );
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
    // SERVER AUTHORITATIVE NETWORK PLAY REQUEST
    // =========================================================

    public bool TryHandleNetworkPlayRequest(
        int requestingPlayerId,
        string cardCode,
        int row,
        int column,
        out string error)
    {
        error = "";

        if (sessionConfig == null)
        {
            error =
                "The match has not started.";

            return false;
        }

        if (gameOver)
        {
            error =
                "The game is already over.";

            return false;
        }

        // Never trust the client to decide whose turn it is.
        if (requestingPlayerId !=
            currentPlayerId)
        {
            error =
                $"It is Player {currentPlayerId}'s turn.";

            return false;
        }

        Player player =
            GetPlayer(
                requestingPlayerId
            );

        if (player == null)
        {
            error =
                "Authoritative player does not exist.";

            return false;
        }

        if (string.IsNullOrWhiteSpace(
                cardCode))
        {
            error =
                "Card code is missing.";

            return false;
        }

        // =====================================================
        // VERIFY THE PLAYER REALLY OWNS THIS CARD
        //
        // Use the actual Card instance from the server's hand.
        // The client only supplies a card code.
        // =====================================================

        Card authoritativeCard =
            null;

        foreach (Card card in player.Hand)
        {
            if (card == null)
                continue;

            if (string.Equals(
                    card.GetCode(),
                    cardCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                authoritativeCard =
                    card;

                break;
            }
        }

        if (authoritativeCard == null)
        {
            error =
                $"Player {requestingPlayerId} " +
                $"does not own {cardCode}.";

            return false;
        }

        if (boardManager == null)
        {
            error =
                "Server BoardManager is unavailable.";

            return false;
        }

        return boardManager.TryExecuteAuthoritativeMove(
            authoritativeCard,
            player.PlayerId,
            player.TeamId,
            row,
            column,
            out error
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
        if (sessionConfig == null)
            return;

        if (boardManager == null)
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

        // =====================================================
        // AUTHORITATIVE NETWORK STATUS
        // =====================================================

        if (networkGameState != null &&
            networkGameState.IsSpawned &&
            networkGameState.IsServer)
        {
            networkGameState.SetPublicMatchStatus(
                sessionConfig.TeamCount,
                team1Sequences,
                team2Sequences,
                team3Sequences,
                sequencesNeeded
            );
        }

        // =====================================================
        // LOCAL UI
        // =====================================================

        if (gameStatusUI != null)
        {
            gameStatusUI.UpdateStatus(
                currentPlayer,
                sessionConfig.TeamCount,
                team1Sequences,
                team2Sequences,
                team3Sequences,
                sequencesNeeded
            );
        }
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
        if (!localTurnInputEnabled)
        {
            if (boardManager != null)
            {
                boardManager.ClearHighlights();
            }

            if (handManager != null)
            {
                handManager.SetDeadCardButtonState(
                    false
                );
            }

            Debug.Log(
                "Card selection ignored: " +
                "it is not this local player's turn."
            );

            return;
        }
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
        // =====================================================
        // LOCAL TURN CHECK
        // =====================================================

        if (!localTurnInputEnabled)
        {
            Debug.Log(
                "Dead-card request ignored: " +
                "it is not this local player's turn."
            );

            return;
        }

        if (deadCard == null)
            return;

        // =====================================================
        // REMOTE CLIENT
        //
        // Client does NOT modify its hand or draw a card.
        // It asks the authoritative server.
        // =====================================================

        NetworkManager networkManager =
            NetworkManager.Singleton;

        bool isRemoteClient =
            networkManager != null &&
            networkManager.IsListening &&
            networkManager.IsClient &&
            !networkManager.IsServer;

        if (isRemoteClient)
        {
            if (networkGameplayBridge == null)
            {
                Debug.LogError(
                    "NetworkGameplayBridge is not assigned."
                );

                return;
            }

            string cardCode =
                deadCard.GetCode();

            networkGameplayBridge.RequestDeadCardReplacement(
                cardCode,
                (success, message) =>
                {
                    if (success)
                    {
                        // The authoritative server will send
                        // the updated private hand separately.
                        deadCardReplacedThisTurn =
                            true;

                        boardManager.ClearHighlights();

                        handManager.SetDeadCardButtonState(
                            false
                        );

                        Debug.Log(
                            $"SERVER replaced dead card " +
                            $"{cardCode}."
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"SERVER rejected dead-card " +
                            $"replacement: {message}"
                        );
                    }
                }
            );

            return;
        }

        // =====================================================
        // HOST / LOCAL AUTHORITATIVE PATH
        // =====================================================

        if (gameOver)
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
                "Deck is empty. " +
                "No replacement card drawn."
            );
        }

        deadCardReplacedThisTurn =
            true;

        boardManager.ClearHighlights();

        RefreshLocalHandIfOwnedBy(
            currentPlayer.PlayerId
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
    // SERVER AUTHORITATIVE DEAD CARD REPLACEMENT
    // =========================================================

    public bool TryHandleNetworkDeadCardReplacement(
        int requestingPlayerId,
        string cardCode,
        out string error)
    {
        error = "";

        // =====================================================
        // MATCH VALIDATION
        // =====================================================

        if (sessionConfig == null)
        {
            error =
                "The match has not started.";

            return false;
        }

        if (gameOver)
        {
            error =
                "The game is already over.";

            return false;
        }

        // =====================================================
        // TURN VALIDATION
        // =====================================================

        if (requestingPlayerId !=
            currentPlayerId)
        {
            error =
                $"It is Player {currentPlayerId}'s turn.";

            return false;
        }

        if (deadCardReplacedThisTurn)
        {
            error =
                "Dead-card replacement was already used this turn.";

            return false;
        }

        // =====================================================
        // PLAYER VALIDATION
        // =====================================================

        Player player =
            GetPlayer(
                requestingPlayerId
            );

        if (player == null)
        {
            error =
                "Authoritative player does not exist.";

            return false;
        }

        // =====================================================
        // VERIFY THE CARD REALLY EXISTS IN SERVER HAND
        // =====================================================

        Card authoritativeCard =
            null;

        foreach (Card card in player.Hand)
        {
            if (card == null)
                continue;

            if (card.GetCode() ==
                cardCode)
            {
                authoritativeCard =
                    card;

                break;
            }
        }

        if (authoritativeCard == null)
        {
            error =
                $"Player {requestingPlayerId} " +
                $"does not own {cardCode}.";

            return false;
        }

        // =====================================================
        // SERVER DETERMINES WHETHER IT IS ACTUALLY DEAD
        // =====================================================

        if (boardManager == null)
        {
            error =
                "Server BoardManager is unavailable.";

            return false;
        }

        if (!boardManager.IsDeadCard(
                authoritativeCard))
        {
            error =
                $"{cardCode} is not a dead card.";

            return false;
        }

        // =====================================================
        // AUTHORITATIVE REPLACEMENT
        // =====================================================

        player.RemoveCard(
            authoritativeCard
        );

        Debug.Log(
            $"NETWORK: Player {player.PlayerId} " +
            $"discarded dead card {cardCode}."
        );

        Card replacementCard =
            deck.Draw();

        if (replacementCard != null)
        {
            player.AddCard(
                replacementCard
            );

            Debug.Log(
                $"NETWORK: Player {player.PlayerId} " +
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

        // Host-owned hand, if applicable.
        RefreshLocalHandIfOwnedBy(
            player.PlayerId
        );

        // Remote owner receives only their updated private hand.
        if (networkHandState != null)
        {
            networkHandState.SendHandToPlayer(
                player.PlayerId
            );
        }

        Debug.Log(
            $"NETWORK DEAD CARD ACCEPTED: " +
            $"Player {player.PlayerId} continues turn."
        );

        // IMPORTANT:
        // DO NOT AdvanceTurn().
        //
        // Sequence rule:
        // dead-card replacement does not consume the turn.
        return true;
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
        RefreshLocalHandIfOwnedBy(
            currentPlayer.PlayerId
        );

        if (networkHandState != null)
        {
            networkHandState.SendHandToPlayer(
                currentPlayer.PlayerId
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

        // =====================================================
        // FIND NEXT SEAT
        //
        // Turn order is based ONLY on SeatIndex.
        // =====================================================

        int nextSeat =
            currentPlayer.SeatIndex + 1;

        if (nextSeat > players.Count)
        {
            nextSeat = 1;
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

        // =====================================================
        // CHANGE AUTHORITATIVE CURRENT PLAYER
        // =====================================================

        currentPlayerId =
            nextPlayer.PlayerId;

        deadCardReplacedThisTurn =
            false;

        boardManager.ClearHighlights();

        // =====================================================
        // UPDATE LOCAL SERVER GAME STATE
        // =====================================================

        SetBoardForCurrentPlayer();

        // ShowPlayerHand(
        //     currentPlayerId
        // );

        // =====================================================
        // SYNCHRONIZE TURN OVER NETWORK
        //
        // The SERVER has decided who plays next.
        // NetworkGameState publishes that decision to
        // every connected client.
        // =====================================================

        SyncCurrentTurnToNetwork();

        // =====================================================
        // UPDATE UI
        // =====================================================

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

        if (networkGameState != null &&
            networkGameState.IsSpawned &&
            networkGameState.IsServer)
        {
            networkGameState.SetWinner(
                winnerTeamId
            );
        }

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
    // SYNCHRONIZE AUTHORITATIVE TURN
    // =========================================================

    private void SyncCurrentTurnToNetwork()
    {
        if (networkGameState == null)
        {
            Debug.LogWarning(
                "GameManager: NetworkGameState is not assigned."
            );

            return;
        }

        Player currentPlayer =
            GetCurrentPlayer();

        if (currentPlayer == null)
        {
            Debug.LogWarning(
                "GameManager: Cannot synchronize turn. " +
                "Current player is null."
            );

            return;
        }

        networkGameState.SetCurrentTurn(
            currentPlayer.PlayerId,
            currentPlayer.SeatIndex,
            currentPlayer.TeamId
        );
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
    #if UNITY_EDITOR || DEVELOPMENT_BUILD

    private void DebugGiveCurrentPlayerCard(
        string cardCode)
    {
        if (gameOver)
        {
            Debug.LogWarning(
                "DEBUG CARD: Game is over."
            );

            return;
        }

        Player currentPlayer =
            GetCurrentPlayer();

        if (currentPlayer == null)
        {
            Debug.LogWarning(
                "DEBUG CARD: No current player."
            );

            return;
        }

        if (!Card.TryFromCode(
                cardCode,
                out Card testCard))
        {
            Debug.LogWarning(
                $"DEBUG CARD: Invalid card code {cardCode}."
            );

            return;
        }

        // -----------------------------------------------------
        // REPLACE ONE EXISTING CARD
        //
        // We replace instead of adding so hand size stays
        // correct.
        // -----------------------------------------------------

        if (currentPlayer.Hand.Count > 0)
        {
            Card removedCard =
                currentPlayer.Hand[0];

            currentPlayer.RemoveCard(
                removedCard
            );

            Debug.Log(
                $"DEBUG CARD: Removed " +
                $"{removedCard.GetCode()} from " +
                $"Player {currentPlayer.PlayerId}."
            );
        }

        currentPlayer.AddCard(
            testCard
        );

        Debug.LogWarning(
            $"DEBUG CARD: Gave {cardCode} to " +
            $"Player {currentPlayer.PlayerId} " +
            $"(Team {currentPlayer.TeamId})."
        );

        // -----------------------------------------------------
        // HOST'S OWN HAND
        // -----------------------------------------------------

        RefreshLocalHandIfOwnedBy(
            currentPlayer.PlayerId
        );

        // -----------------------------------------------------
        // REMOTE PLAYER'S PRIVATE HAND
        // -----------------------------------------------------

        if (networkHandState != null)
        {
            networkHandState.SendHandToPlayer(
                currentPlayer.PlayerId
            );
        }
    }

    #endif
}