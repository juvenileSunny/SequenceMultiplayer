using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board UI")]
    [SerializeField] private Transform boardContainer;
    [SerializeField] private BoardCellView boardCellPrefab;

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
    }

    // =========================================================
    // CURRENT PLAYER / TEAM
    // =========================================================

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

    public int DebugCreateNextSequenceForTeam(
        int teamId)
    {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD

        if (board == null)
            return 0;

        if (teamId <= 0)
            return 0;

        int existingCount =
            GetSequenceCount(teamId);

        // =====================================================
        // FIRST DEBUG SEQUENCE
        //
        // Uses top-left corner:
        //
        // [0,0] = wild corner
        // [0,1]
        // [0,2]
        // [0,3]
        // [0,4]
        // =====================================================

        if (existingCount == 0)
        {
            for (int column = 1;
                column <= 4;
                column++)
            {
                DebugSetCellForTeam(
                    0,
                    column,
                    teamId
                );
            }

            BoardCell lastCell =
                board.GetCell(
                    0,
                    4
                );

            int created =
                RegisterNewSequences(
                    teamId,
                    lastCell
                );

            Debug.LogWarning(
                $"DEBUG: Created first Sequence " +
                $"for Team {teamId}."
            );

            return created;
        }

        // =====================================================
        // SECOND DEBUG SEQUENCE
        //
        // Uses same top-left wild corner,
        // but goes vertically:
        //
        // [0,0] = wild corner
        // [1,0]
        // [2,0]
        // [3,0]
        // [4,0]
        //
        // This overlaps the first Sequence by
        // only one cell: the corner.
        // =====================================================

        if (existingCount == 1)
        {
            for (int row = 1;
                row <= 4;
                row++)
            {
                DebugSetCellForTeam(
                    row,
                    0,
                    teamId
                );
            }

            BoardCell lastCell =
                board.GetCell(
                    4,
                    0
                );

            int created =
                RegisterNewSequences(
                    teamId,
                    lastCell
                );

            Debug.LogWarning(
                $"DEBUG: Created second Sequence " +
                $"for Team {teamId}."
            );

            return created;
        }

        Debug.LogWarning(
            $"DEBUG: Team {teamId} already has " +
            $"{existingCount} Sequence(s)."
        );

        return 0;

    #else

        return 0;

    #endif
    }

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