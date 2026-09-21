using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour
{
    [Header("Board UI")]
    [SerializeField] private Transform boardContainer;
    [SerializeField] private BoardCellView boardCellPrefab;

    [Header("Network")]
    [SerializeField]
    private NetworkGameState networkGameState;
    [SerializeField]
    private NetworkGameplayBridge networkGameplayBridge;



    [Header("Features")]
    [SerializeField] private bool showLegalMoveHighlights = true;

    private Board board;

    private BoardCellView[,] cellViews =
        new BoardCellView[Board.Rows, Board.Columns];

    // Card currently selected from the player's hand.
    private Card selectedCard;

    // Current turn information.
    private int currentPlayerId = 1;
    private int currentTeamId = 1;

    private bool boardLocked = false;

    public Board Board => board;

    // Fired after any successful play:
    // normal card, two-eyed Jack, or one-eyed Jack.
    public event Action<Card, BoardCell> OnMoveCompleted;
    public event Action<BoardCell> OnBoardCellChanged;

    // Key = TeamId
    // Value = completed Sequences belonging to that team.
    private readonly Dictionary<
        int,
        List<List<BoardCell>>
    > completedSequences =
        new Dictionary<
            int,
            List<List<BoardCell>>
        >();

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        CreateBoard();
    }

    private void OnEnable()
    {
        if (networkGameState != null)
        {
            networkGameState.OnCurrentPlayerChanged +=
                HandleNetworkTurnChanged;
        }
    }

    private void OnDisable()
    {
        if (networkGameState != null)
        {
            networkGameState.OnCurrentPlayerChanged -=
                HandleNetworkTurnChanged;
        }
    }

    private void HandleNetworkTurnChanged(
        int previousPlayerId,
        int newPlayerId)
    {
        RefreshCurrentPlayerFromNetwork();

        Debug.Log(
            $"Board network identity refreshed: " +
            $"Player {currentPlayerId}, Team {currentTeamId}."
        );
    }

    // =========================================================
    // BOARD CREATION
    // =========================================================

    private void CreateBoard()
    {
        board = new Board();

        GenerateBoardVisuals();

        Debug.Log("Board created.");
    }

    private void GenerateBoardVisuals()
    {
        ClearExistingBoard();

        for (int row = 0; row < Board.Rows; row++)
        {
            for (int column = 0;
                 column < Board.Columns;
                 column++)
            {
                BoardCell cell =
                    board.GetCell(row, column);

                BoardCellView cellView =
                    Instantiate(
                        boardCellPrefab,
                        boardContainer
                    );

                cellView.name =
                    $"Cell_{row}_{column}";

                cellView.Initialize(
                    cell,
                    OnCellClicked
                );

                cellViews[row, column] =
                    cellView;
            }
        }
        // Canvas.ForceUpdateCanvases();

        // RectTransform boardRect =
        //     boardContainer as RectTransform;

        // if (boardRect != null)
        // {
        //     LayoutRebuilder.ForceRebuildLayoutImmediate(
        //         boardRect
        //     );
        // }
    }

    // =========================================================
    // CURRENT PLAYER / TEAM
    // =========================================================
    private void RefreshCurrentPlayerFromNetwork()
    {
        if (networkGameState == null)
            return;

        if (!networkGameState.IsSpawned)
            return;

        if (networkGameState.CurrentPlayerId <= 0)
            return;

        if (networkGameState.CurrentTeamId <= 0)
            return;

        currentPlayerId =
            networkGameState.CurrentPlayerId;

        currentTeamId =
            networkGameState.CurrentTeamId;
    }


    public void SetCurrentPlayer(
        int playerId,
        int teamId)
    {
        currentPlayerId = playerId;
        currentTeamId = teamId;

        Debug.Log(
            $"Board ready for Player {currentPlayerId} " +
            $"(Team {currentTeamId})."
        );
    }

    // =========================================================
    // CELL CLICK
    // =========================================================

    private void OnCellClicked(
        BoardCellView cellView)
    {
        RefreshCurrentPlayerFromNetwork();

        NetworkManager networkManager =
            NetworkManager.Singleton;

        if (networkManager != null &&
            networkManager.IsListening &&
            networkManager.IsClient &&
            !networkManager.IsServer)
        {
            if (cellView == null)
                return;

            BoardCell networkTargetCell =
                cellView.Cell;

            if (networkTargetCell == null)
                return;

            if (selectedCard == null)
            {
                Debug.Log(
                    "Select a card from your hand first."
                );

                return;
            }

            if (networkGameplayBridge == null)
            {
                Debug.LogError(
                    "NetworkGameplayBridge is not assigned."
                );

                return;
            }

            string requestedCardCode =
                selectedCard.GetCode();

            networkGameplayBridge.RequestPlayCard(
                requestedCardCode,
                networkTargetCell.Row,
                networkTargetCell.Column,
                (success, message) =>
                {
                    if (success)
                    {
                        Debug.Log(
                            $"SERVER accepted " +
                            $"{requestedCardCode}."
                        );

                        ClearHighlights();
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"SERVER rejected move: " +
                            $"{message}"
                        );
                    }
                }
            );

            return;
        }
        if (boardLocked)
        {
            Debug.Log(
                "Game is over. Board is locked."
            );

            return;
        }

        if (cellView == null)
            return;

        BoardCell cell =
            cellView.Cell;

        if (cell == null)
            return;

        if (selectedCard == null)
        {
            Debug.Log(
                "Select a card from your hand first."
            );

            return;
        }

        // Corners are wild/free Sequence spaces.
        if (cell.IsCorner)
        {
            Debug.Log(
                "Sequence corner cannot be targeted."
            );

            return;
        }

        // Two-eyed Jack
        // JC / JD
        if (selectedCard.IsTwoEyedJack())
        {
            HandleTwoEyedJack(
                cellView
            );

            return;
        }

        // One-eyed Jack
        // JH / JS
        if (selectedCard.IsOneEyedJack())
        {
            HandleOneEyedJack(
                cellView
            );

            return;
        }

        // Normal card
        HandleNormalCard(
            cellView
        );
    }

    // =========================================================
    // SERVER AUTHORITATIVE MOVE
    // =========================================================

    public bool TryExecuteAuthoritativeMove(
        Card card,
        int playerId,
        int teamId,
        int row,
        int column,
        out string error)
    {
        error = "";

        if (card == null)
        {
            error =
                "Card is null.";

            return false;
        }

        if (boardLocked)
        {
            error =
                "The game board is locked.";

            return false;
        }

        if (board == null ||
            !board.IsValidPosition(
                row,
                column))
        {
            error =
                "Invalid board position.";

            return false;
        }

        BoardCell cell =
            board.GetCell(
                row,
                column
            );

        BoardCellView view =
            cellViews[
                row,
                column
            ];

        if (cell == null ||
            view == null)
        {
            error =
                "Board cell is unavailable.";

            return false;
        }

        if (cell.IsCorner)
        {
            error =
                "Sequence corners cannot be targeted.";

            return false;
        }

        // =====================================================
        // VALIDATE BEFORE CHANGING AUTHORITATIVE BOARD STATE
        // =====================================================

        if (card.IsTwoEyedJack())
        {
            if (cell.IsOccupied)
            {
                error =
                    "Two-eyed Jack requires an empty space.";

                return false;
            }
        }
        else if (card.IsOneEyedJack())
        {
            if (!cell.IsOccupied)
            {
                error =
                    "One-eyed Jack requires an opponent chip.";

                return false;
            }

            if (cell.OwnerTeamId ==
                teamId)
            {
                error =
                    "Cannot remove your own team's chip.";

                return false;
            }

            if (cell.IsPartOfCompletedSequence)
            {
                error =
                    "Completed Sequence chips are protected.";

                return false;
            }
        }
        else
        {
            if (cell.IsOccupied)
            {
                error =
                    "That board position is occupied.";

                return false;
            }

            if (cell.Card == null)
            {
                error =
                    "This board position has no card.";

                return false;
            }

            bool matches =
                cell.Card.Rank == card.Rank &&
                cell.Card.Suit == card.Suit;

            if (!matches)
            {
                error =
                    $"Card {card.GetCode()} " +
                    "does not match this position.";

                return false;
            }
        }

        // =====================================================
        // SERVER HAS APPROVED THE MOVE
        //
        // Reuse the existing local board pipeline so Jacks,
        // move completion, sequence detection, draw, and turn
        // advancement remain in one authoritative implementation.
        // =====================================================

        currentPlayerId =
            playerId;

        currentTeamId =
            teamId;

        selectedCard =
            card;

        if (card.IsOneEyedJack())
        {
            RemoveChipWithJack(
                view
            );
        }
        else
        {
            PlaceChip(
                view
            );
        }

        return true;
    }

    // =========================================================
    // APPLY AUTHORITATIVE NETWORK BOARD STATE
    //
    // Remote clients use this only for presentation.
    // They never decide ownership themselves.
    // =========================================================

    public void ApplyNetworkCellState(
        int row,
        int column,
        int ownerTeamId,
        bool isPartOfCompletedSequence)
    {
        if (board == null)
            return;

        if (!board.IsValidPosition(
                row,
                column))
        {
            return;
        }

        BoardCell cell =
            board.GetCell(
                row,
                column
            );

        if (cell == null)
            return;

        if (ownerTeamId > 0)
        {
            cell.SetOwnerTeam(
                ownerTeamId
            );
        }
        else
        {
            cell.ClearOwner();
        }

        if (isPartOfCompletedSequence &&
            !cell.IsCorner)
        {
            cell.MarkAsCompletedSequence();
        }

        BoardCellView view =
            cellViews[
                row,
                column
            ];

        if (view != null)
        {
            view.UpdateVisual();
        }
    }

    // =========================================================
    // BOARD LOCK
    // =========================================================

    public void SetBoardLocked(
        bool locked)
    {
        boardLocked = locked;

        if (locked)
        {
            ClearHighlights();
        }
    }

    // =========================================================
    // NORMAL CARD
    // =========================================================

    private void HandleNormalCard(
        BoardCellView cellView)
    {
        BoardCell cell =
            cellView.Cell;

        if (cell == null)
            return;

        if (cell.IsOccupied)
        {
            Debug.Log(
                $"Cell [{cell.Row},{cell.Column}] " +
                "is already occupied."
            );

            return;
        }

        if (cell.Card == null)
            return;

        bool matches =
            cell.Card.Rank == selectedCard.Rank &&
            cell.Card.Suit == selectedCard.Suit;

        if (!matches)
        {
            Debug.Log(
                $"Invalid move. Selected " +
                $"{selectedCard.GetCode()}, but clicked " +
                $"{cell.Card.GetCode()}."
            );

            return;
        }

        PlaceChip(
            cellView
        );
    }

    // =========================================================
    // TWO-EYED JACK
    // JC / JD
    // =========================================================

    private void HandleTwoEyedJack(
        BoardCellView cellView)
    {
        BoardCell cell =
            cellView.Cell;

        if (cell == null)
            return;

        if (cell.IsOccupied)
        {
            Debug.Log(
                "Two-eyed Jack requires an empty space."
            );

            return;
        }

        Debug.Log(
            $"{selectedCard.GetCode()} used as a " +
            "two-eyed Jack."
        );

        PlaceChip(
            cellView
        );
    }

    // =========================================================
    // ONE-EYED JACK
    // JH / JS
    // =========================================================

    private void HandleOneEyedJack(
        BoardCellView cellView)
    {
        BoardCell cell =
            cellView.Cell;

        if (cell == null)
            return;

        if (!cell.IsOccupied)
        {
            Debug.Log(
                "One-eyed Jack must target an opponent team's chip."
            );

            return;
        }

        // Cannot remove a teammate's chip.
        if (cell.OwnerTeamId == currentTeamId)
        {
            Debug.Log(
                "You cannot remove a chip belonging to your own team."
            );

            return;
        }

        // Completed Sequence chips are protected.
        if (cell.IsPartOfCompletedSequence)
        {
            Debug.Log(
                "This chip belongs to a completed Sequence " +
                "and cannot be removed."
            );

            return;
        }

        RemoveChipWithJack(
            cellView
        );
    }

    // =========================================================
    // PLACE CHIP
    // =========================================================

    private void PlaceChip(
        BoardCellView cellView)
    {
        BoardCell cell =
            cellView.Cell;

        if (cell == null)
            return;

        if (cell.IsOccupied)
        {
            Debug.Log(
                $"Cell [{cell.Row},{cell.Column}] " +
                "is already occupied."
            );

            return;
        }

        // IMPORTANT:
        // Board spaces belong to TEAMS, not individual players.
        cell.SetOwnerTeam(
            currentTeamId
        );

        cellView.UpdateVisual();
        OnBoardCellChanged?.Invoke(
            cell
        );

        Debug.Log(
            $"Player {currentPlayerId} " +
            $"(Team {currentTeamId}) played " +
            $"{selectedCard.GetCode()} at " +
            $"[{cell.Row},{cell.Column}]."
        );

        FinishBoardMove(
            selectedCard,
            cell
        );
    }

    // =========================================================
    // REMOVE CHIP WITH ONE-EYED JACK
    // =========================================================

    private void RemoveChipWithJack(
        BoardCellView cellView)
    {
        BoardCell cell =
            cellView.Cell;

        if (cell == null)
            return;

        Card playedJack =
            selectedCard;

        int removedTeamId =
            cell.OwnerTeamId;

        cell.ClearOwner();

        cellView.UpdateVisual();
        OnBoardCellChanged?.Invoke(
            cell
        );

        Debug.Log(
            $"Player {currentPlayerId} " +
            $"(Team {currentTeamId}) used " +
            $"{playedJack.GetCode()} to remove " +
            $"Team {removedTeamId}'s chip from " +
            $"[{cell.Row},{cell.Column}]."
        );

        // A removal does not create a new Sequence,
        // so placedCell is null.
        FinishBoardMove(
            playedJack,
            null
        );
    }

    // =========================================================
    // FINISH BOARD MOVE
    // =========================================================

    private void FinishBoardMove(
        Card playedCard,
        BoardCell placedCell)
    {
        selectedCard = null;

        ClearHighlightVisuals();

        OnMoveCompleted?.Invoke(
            playedCard,
            placedCell
        );
    }

    // =========================================================
    // LEGAL MOVE HIGHLIGHTING
    // =========================================================

    public void HighlightMatchingCard(
        Card card)
    {
        RefreshCurrentPlayerFromNetwork();

        ClearHighlightVisuals();

        selectedCard = card;

        if (card == null)
            return;

        // Turning visuals off must not disable gameplay.
        if (!showLegalMoveHighlights)
            return;

        // Two-eyed Jack
        if (card.IsTwoEyedJack())
        {
            HighlightTwoEyedJackTargets();

            return;
        }

        // One-eyed Jack
        if (card.IsOneEyedJack())
        {
            HighlightOneEyedJackTargets();

            return;
        }

        // Normal card
        for (int row = 0;
             row < Board.Rows;
             row++)
        {
            for (int column = 0;
                 column < Board.Columns;
                 column++)
            {
                BoardCell cell =
                    board.GetCell(
                        row,
                        column
                    );

                if (cell == null)
                    continue;

                if (cell.IsCorner)
                    continue;

                if (cell.IsOccupied)
                    continue;

                if (cell.Card == null)
                    continue;

                bool matches =
                    cell.Card.Rank == card.Rank &&
                    cell.Card.Suit == card.Suit;

                if (!matches)
                    continue;

                SetCellHighlight(
                    row,
                    column,
                    true
                );
            }
        }
    }

    // =========================================================
    // TWO-EYED JACK HIGHLIGHTS
    // =========================================================

    private void HighlightTwoEyedJackTargets()
    {
        for (int row = 0;
             row < Board.Rows;
             row++)
        {
            for (int column = 0;
                 column < Board.Columns;
                 column++)
            {
                BoardCell cell =
                    board.GetCell(
                        row,
                        column
                    );

                if (cell == null)
                    continue;

                if (cell.IsCorner)
                    continue;

                if (cell.IsOccupied)
                    continue;

                SetCellHighlight(
                    row,
                    column,
                    true
                );
            }
        }

        Debug.Log(
            "Two-eyed Jack: choose any empty highlighted space."
        );
    }

    // =========================================================
    // ONE-EYED JACK HIGHLIGHTS
    // =========================================================

    private void HighlightOneEyedJackTargets()
    {
        for (int row = 0;
             row < Board.Rows;
             row++)
        {
            for (int column = 0;
                 column < Board.Columns;
                 column++)
            {
                BoardCell cell =
                    board.GetCell(
                        row,
                        column
                    );

                if (cell == null)
                    continue;

                if (cell.IsCorner)
                    continue;

                if (!cell.IsOccupied)
                    continue;

                // Cannot remove your own team's chip.
                if (cell.OwnerTeamId == currentTeamId)
                    continue;

                // Completed Sequence chips are protected.
                if (cell.IsPartOfCompletedSequence)
                    continue;

                SetCellHighlight(
                    row,
                    column,
                    true
                );
            }
        }

        Debug.Log(
            "One-eyed Jack: choose an opponent team's chip to remove."
        );
    }

    // =========================================================
    // HIGHLIGHT HELPERS
    // =========================================================

    private void SetCellHighlight(
        int row,
        int column,
        bool highlighted)
    {
        BoardCellView view =
            cellViews[row, column];

        if (view != null)
        {
            view.SetHighlighted(
                highlighted
            );
        }
    }

    public void ClearHighlights()
    {
        selectedCard = null;

        ClearHighlightVisuals();
    }

    private void ClearHighlightVisuals()
    {
        for (int row = 0;
             row < Board.Rows;
             row++)
        {
            for (int column = 0;
                 column < Board.Columns;
                 column++)
            {
                BoardCellView view =
                    cellViews[row, column];

                if (view != null)
                {
                    view.SetHighlighted(
                        false
                    );
                }
            }
        }
    }

    // =========================================================
    // DEAD CARD CHECK
    // =========================================================

    public bool IsDeadCard(
        Card card)
    {
        if (card == null)
            return false;

        // Jacks are never dead cards.
        if (card.IsJack())
            return false;

        int matchingSpaces = 0;
        int openSpaces = 0;

        for (int row = 0;
             row < Board.Rows;
             row++)
        {
            for (int column = 0;
                 column < Board.Columns;
                 column++)
            {
                BoardCell cell =
                    board.GetCell(
                        row,
                        column
                    );

                if (cell == null)
                    continue;

                if (cell.IsCorner)
                    continue;

                if (cell.Card == null)
                    continue;

                bool matches =
                    cell.Card.Rank == card.Rank &&
                    cell.Card.Suit == card.Suit;

                if (!matches)
                    continue;

                matchingSpaces++;

                if (!cell.IsOccupied)
                {
                    openSpaces++;
                }
            }
        }

        return
            matchingSpaces > 0 &&
            openSpaces == 0;
    }

    // =========================================================
    // RESET
    // =========================================================

    public void ResetSequenceData()
    {
        completedSequences.Clear();

        boardLocked = false;

        ClearHighlights();
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void ClearExistingBoard()
    {
        for (int row = 0;
             row < Board.Rows;
             row++)
        {
            for (int column = 0;
                 column < Board.Columns;
                 column++)
            {
                cellViews[row, column] =
                    null;
            }
        }

        foreach (Transform child in boardContainer)
        {
            Destroy(
                child.gameObject
            );
        }
    }

    // =========================================================
    // SEQUENCE DETECTION
    // =========================================================

    public int RegisterNewSequences(
        int teamId,
        BoardCell lastPlacedCell)
    {
        if (lastPlacedCell == null)
            return 0;

        if (lastPlacedCell.IsCorner)
            return 0;

        // The newly placed chip must belong to the team
        // we are checking.
        if (lastPlacedCell.OwnerTeamId != teamId)
            return 0;

        List<List<BoardCell>> teamSequences;

        if (!completedSequences.TryGetValue(
                teamId,
                out teamSequences))
        {
            teamSequences =
                new List<List<BoardCell>>();

            completedSequences.Add(
                teamId,
                teamSequences
            );
        }

        int newSequenceCount = 0;

        // Horizontal
        newSequenceCount +=
            FindAndRegisterSequencesInDirection(
                teamId,
                lastPlacedCell,
                0,
                1,
                teamSequences
            );

        // Vertical
        newSequenceCount +=
            FindAndRegisterSequencesInDirection(
                teamId,
                lastPlacedCell,
                1,
                0,
                teamSequences
            );

        // Diagonal \
        newSequenceCount +=
            FindAndRegisterSequencesInDirection(
                teamId,
                lastPlacedCell,
                1,
                1,
                teamSequences
            );

        // Diagonal /
        newSequenceCount +=
            FindAndRegisterSequencesInDirection(
                teamId,
                lastPlacedCell,
                1,
                -1,
                teamSequences
            );

        return newSequenceCount;
    }

    private int FindAndRegisterSequencesInDirection(
        int teamId,
        BoardCell lastPlacedCell,
        int rowDirection,
        int columnDirection,
        List<List<BoardCell>> existingSequences)
    {
        List<List<BoardCell>> candidates =
            new List<List<BoardCell>>();

        // Any 5-cell Sequence containing the newly placed
        // chip can begin between -4 and 0 relative to it.
        for (int offset = -4;
             offset <= 0;
             offset++)
        {
            int startRow =
                lastPlacedCell.Row +
                offset * rowDirection;

            int startColumn =
                lastPlacedCell.Column +
                offset * columnDirection;

            List<BoardCell> candidate =
                new List<BoardCell>();

            bool valid = true;

            for (int i = 0;
                 i < 5;
                 i++)
            {
                int row =
                    startRow +
                    i * rowDirection;

                int column =
                    startColumn +
                    i * columnDirection;

                if (!board.IsValidPosition(
                        row,
                        column))
                {
                    valid = false;
                    break;
                }

                BoardCell cell =
                    board.GetCell(
                        row,
                        column
                    );

                if (cell == null)
                {
                    valid = false;
                    break;
                }

                // Corners count for every team.
                bool belongsToTeam =
                    cell.IsCorner ||
                    cell.OwnerTeamId == teamId;

                if (!belongsToTeam)
                {
                    valid = false;
                    break;
                }

                candidate.Add(
                    cell
                );
            }

            if (!valid)
                continue;

            if (candidate.Count != 5)
                continue;

            // New Sequence may overlap an existing
            // Sequence by at most one cell.
            if (!IsCompatibleWithExistingSequences(
                    candidate,
                    existingSequences))
            {
                continue;
            }

            candidates.Add(
                candidate
            );
        }

        if (candidates.Count == 0)
            return 0;

        /*
         * Usually only one new Sequence exists in one direction.
         *
         * However, a newly placed chip can connect two groups
         * and legally create two Sequences sharing one cell.
         */

        List<List<BoardCell>> accepted =
            new List<List<BoardCell>>();

        bool foundPair = false;

        for (int i = 0;
             i < candidates.Count &&
             !foundPair;
             i++)
        {
            for (int j = i + 1;
                 j < candidates.Count;
                 j++)
            {
                int overlap =
                    CountOverlap(
                        candidates[i],
                        candidates[j]
                    );

                if (overlap <= 1)
                {
                    accepted.Add(
                        candidates[i]
                    );

                    accepted.Add(
                        candidates[j]
                    );

                    foundPair = true;

                    break;
                }
            }
        }

        if (!foundPair)
        {
            accepted.Add(
                candidates[0]
            );
        }

        int registered = 0;

        foreach (List<BoardCell> sequence
                 in accepted)
        {
            // Re-check because a previously accepted
            // Sequence may now be in existingSequences.
            if (!IsCompatibleWithExistingSequences(
                    sequence,
                    existingSequences))
            {
                continue;
            }

            existingSequences.Add(
                sequence
            );

            foreach (BoardCell cell in sequence)
            {
                if (cell.IsCorner)
                    continue;

                cell.MarkAsCompletedSequence();

                BoardCellView view =
                    cellViews[
                        cell.Row,
                        cell.Column
                    ];

                if (view != null)
                {
                    view.UpdateVisual();
                }

                OnBoardCellChanged?.Invoke(
                    cell
                );
            }

            registered++;

            Debug.Log(
                $"Team {teamId} completed a Sequence."
            );
        }

        return registered;
    }

    // =========================================================
    // SEQUENCE OVERLAP CHECK
    // =========================================================

    private bool IsCompatibleWithExistingSequences(
        List<BoardCell> candidate,
        List<List<BoardCell>> existingSequences)
    {
        foreach (List<BoardCell> existing
                 in existingSequences)
        {
            int overlap =
                CountOverlap(
                    candidate,
                    existing
                );

            if (overlap > 1)
            {
                return false;
            }
        }

        return true;
    }

    private int CountOverlap(
        List<BoardCell> first,
        List<BoardCell> second)
    {
        int overlap = 0;

        foreach (BoardCell cell in first)
        {
            if (second.Contains(cell))
            {
                overlap++;
            }
        }

        return overlap;
    }

    // =========================================================
    // SEQUENCE COUNT
    // =========================================================

    public int GetSequenceCount(
        int teamId)
    {
        List<List<BoardCell>> teamSequences;

        if (!completedSequences.TryGetValue(
                teamId,
                out teamSequences))
        {
            return 0;
        }

        return teamSequences.Count;
    }

    // =========================================================
    // DEBUG / DEVELOPMENT TESTING
    // =========================================================

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void DebugCreateNextSequenceForTeam(int teamId)
    {
        if (board == null)
            return;

        if (teamId < 1 ||
            teamId > 3)
        {
            Debug.LogWarning(
                $"DEBUG: Unsupported Team {teamId}."
            );

            return;
        }

        int currentSequenceCount =
            GetSequenceCount(
                teamId
            );

        if (currentSequenceCount >= 3)
        {
            Debug.Log(
                $"DEBUG: Team {teamId} already has " +
                $"{currentSequenceCount} Sequence(s)."
            );

            return;
        }

        // -----------------------------------------------------
        // TEAM-SPECIFIC DEBUG LANES
        //
        // Each team gets three separate horizontal rows so
        // Team 1 / Team 2 / Team 3 never fight over the same
        // debug cells.
        //
        // Team 1:
        //   Sequence 1 -> row 1, columns 1..5
        //   Sequence 2 -> row 2, columns 1..5
        //   Sequence 3 -> row 3, columns 1..5
        //
        // Team 2:
        //   Sequence 1 -> row 4, columns 1..5
        //   Sequence 2 -> row 5, columns 1..5
        //   Sequence 3 -> row 6, columns 1..5
        //
        // Team 3:
        //   Sequence 1 -> row 7, columns 1..5
        //   Sequence 2 -> row 8, columns 1..5
        //   Sequence 3 -> row 9, columns 1..5
        //
        // None of these cells are corners.
        // -----------------------------------------------------

        int baseRow =
            1 +
            ((teamId - 1) * 3);

        int targetRow =
            baseRow +
            currentSequenceCount;

        const int startColumn = 1;
        const int endColumn = 5;

        // -----------------------------------------------------
        // VALIDATE ALL FIVE CELLS FIRST
        // -----------------------------------------------------

        for (int column = startColumn;
             column <= endColumn;
             column++)
        {
            BoardCell cell =
                board.GetCell(
                    targetRow,
                    column
                );

            if (cell == null)
            {
                Debug.LogWarning(
                    $"DEBUG: Missing cell " +
                    $"[{targetRow},{column}]."
                );

                return;
            }

            if (cell.IsCorner)
            {
                Debug.LogWarning(
                    $"DEBUG: Unexpected corner at " +
                    $"[{targetRow},{column}]."
                );

                return;
            }

            // Never overwrite another team's chip.
            if (cell.IsOccupied &&
                cell.OwnerTeamId != teamId)
            {
                Debug.LogWarning(
                    $"DEBUG: Cannot create Team {teamId} " +
                    $"Sequence {currentSequenceCount + 1}. " +
                    $"Cell [{targetRow},{column}] belongs " +
                    $"to Team {cell.OwnerTeamId}."
                );

                return;
            }
        }

        // -----------------------------------------------------
        // PLACE THE FIVE DEBUG CHIPS
        // -----------------------------------------------------

        for (int column = startColumn;
             column <= endColumn;
             column++)
        {
            BoardCell cell =
                board.GetCell(
                    targetRow,
                    column
                );

            cell.SetOwnerTeam(
                teamId
            );

            BoardCellView view =
                cellViews[
                    targetRow,
                    column
                ];

            if (view != null)
            {
                view.UpdateVisual();
            }

            // Publish the changed owner state immediately.
            OnBoardCellChanged?.Invoke(
                cell
            );
        }

        // -----------------------------------------------------
        // REGISTER THE NEW SEQUENCE
        //
        // The last cell is part of the newly-created line, so
        // the normal Sequence detector can discover/register it.
        // -----------------------------------------------------

        BoardCell lastCell =
            board.GetCell(
                targetRow,
                endColumn
            );

        int created =
            RegisterNewSequences(
                teamId,
                lastCell
            );

        int total =
            GetSequenceCount(
                teamId
            );

        Debug.LogWarning(
            $"DEBUG F1: Team {teamId} sequence lane " +
            $"{currentSequenceCount + 1} -> " +
            $"row {targetRow}. " +
            $"Created={created}, Total={total}."
        );
    }
    #endif

    private void DebugSetCellForTeam(
        int row,
        int column,
        int teamId)
    {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD

        BoardCell cell =
            board.GetCell(
                row,
                column
            );

        if (cell == null)
            return;

        if (cell.IsCorner)
            return;

        // Completely clean this position first.
        cell.ResetGameplayState();

        cell.SetOwnerTeam(
            teamId
        );

        BoardCellView view =
            cellViews[
                row,
                column
            ];

        if (view != null)
        {
            view.UpdateVisual();
        }

    #endif
    }
}