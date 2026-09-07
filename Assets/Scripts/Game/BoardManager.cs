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

    // GameManager controls whose turn it is.
    private int currentPlayerId = 1;

    public Board Board => board;

    // Fired after ANY successful play:
    // normal card, two-eyed Jack, or one-eyed Jack.
    public event Action<Card, BoardCell> OnMoveCompleted;
    private bool boardLocked = false;

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
    // private void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.F1))
    //     {
    //         TestSequenceVisual();
    //     }
    // }

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
    // CURRENT PLAYER
    // =========================================================

    public void SetCurrentPlayer(int playerId)
    {
        currentPlayerId = playerId;

        ClearHighlights();

        Debug.Log(
            $"Board ready for Player {currentPlayerId}."
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

        // Player must select a card first.
        if (selectedCard == null)
        {
            Debug.Log(
                "Select a card from your hand first."
            );

            return;
        }

        // Corners are free Sequence spaces.
        // Players never place/remove chips there.
        if (cell.IsCorner)
        {
            Debug.Log(
                "Sequence corner cannot be targeted."
            );

            return;
        }

        // =====================================================
        // TWO-EYED JACK
        // JC / JD
        // Place a chip on ANY empty space.
        // =====================================================

        if (selectedCard.IsTwoEyedJack())
        {
            HandleTwoEyedJack(
                cellView
            );

            return;
        }

        // =====================================================
        // ONE-EYED JACK
        // JH / JS
        // Remove an opponent chip.
        // =====================================================

        if (selectedCard.IsOneEyedJack())
        {
            HandleOneEyedJack(
                cellView
            );

            return;
        }

        // =====================================================
        // NORMAL CARD
        // =====================================================

        HandleNormalCard(
            cellView
        );
    }

    public void SetBoardLocked(bool locked)
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
    // =========================================================

    private void HandleTwoEyedJack(
        BoardCellView cellView)
    {
        BoardCell cell =
            cellView.Cell;

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
                "One-eyed Jack must target an opponent's chip."
            );

            return;
        }

        if (cell.OwnerId == currentPlayerId)
        {
            Debug.Log(
                "You cannot remove your own chip."
            );

            return;
        }

        // Completed Sequence chips cannot be removed.
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

        Card playedCard =
            selectedCard;

        cell.SetOwner(
            currentPlayerId
        );

        cellView.UpdateVisual();

        Debug.Log(
            $"Player {currentPlayerId} played " +
            $"{playedCard.GetCode()} at " +
            $"[{cell.Row},{cell.Column}]."
        );

        FinishBoardMove(
            playedCard,
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

        Card playedJack =
            selectedCard;

        int removedPlayerId =
            cell.OwnerId;

        // Remove ownership from board data.
        cell.ClearOwner();

        // Refresh visual overlay.
        cellView.UpdateVisual();

        Debug.Log(
            $"Player {currentPlayerId} used " +
            $"{playedJack.GetCode()} to remove " +
            $"Player {removedPlayerId}'s chip from " +
            $"[{cell.Row},{cell.Column}]."
        );

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

        // Turning visuals off must NOT disable gameplay.
        if (!showLegalMoveHighlights)
            return;

        // =====================================================
        // TWO-EYED JACK
        // Highlight every empty non-corner cell.
        // =====================================================

        if (card.IsTwoEyedJack())
        {
            HighlightTwoEyedJackTargets();

            return;
        }

        // =====================================================
        // ONE-EYED JACK
        // Highlight opponent chips.
        // =====================================================

        if (card.IsOneEyedJack())
        {
            HighlightOneEyedJackTargets();

            return;
        }

        // =====================================================
        // NORMAL CARD
        // =====================================================

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

                // One-eyed Jack needs an occupied cell.
                if (!cell.IsOccupied)
                    continue;

                // Cannot remove your own chip.
                if (cell.OwnerId == currentPlayerId)
                    continue;

                // NEW:
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
            "One-eyed Jack: choose an opponent chip to remove."
        );
    }

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

    // =========================================================
    // CLEAR SELECTION
    // =========================================================

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

        // Jacks can always use their special ability,
        // so they are never considered dead cards.
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
    // CLEANUP
    // =========================================================

    public void ResetSequenceData()
    {
        completedSequences.Clear();
        boardLocked = false;
    }
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
        int ownerId,
        BoardCell lastPlacedCell)
    {
        if (lastPlacedCell == null)
            return 0;

        if (lastPlacedCell.IsCorner)
            return 0;

        if (lastPlacedCell.OwnerId != ownerId)
            return 0;

        List<List<BoardCell>> ownerSequences;

        if (!completedSequences.TryGetValue(
                ownerId,
                out ownerSequences))
        {
            ownerSequences =
                new List<List<BoardCell>>();

            completedSequences.Add(
                ownerId,
                ownerSequences
            );
        }

        int newSequenceCount = 0;

        // Horizontal
        newSequenceCount +=
            FindAndRegisterSequencesInDirection(
                ownerId,
                lastPlacedCell,
                0,
                1,
                ownerSequences
            );

        // Vertical
        newSequenceCount +=
            FindAndRegisterSequencesInDirection(
                ownerId,
                lastPlacedCell,
                1,
                0,
                ownerSequences
            );

        // Diagonal \
        newSequenceCount +=
            FindAndRegisterSequencesInDirection(
                ownerId,
                lastPlacedCell,
                1,
                1,
                ownerSequences
            );

        // Diagonal /
        newSequenceCount +=
            FindAndRegisterSequencesInDirection(
                ownerId,
                lastPlacedCell,
                1,
                -1,
                ownerSequences
            );

        return newSequenceCount;
    }


    private int FindAndRegisterSequencesInDirection(
        int ownerId,
        BoardCell lastPlacedCell,
        int rowDirection,
        int columnDirection,
        List<List<BoardCell>> existingSequences)
    {
        List<List<BoardCell>> candidates =
            new List<List<BoardCell>>();

        // Any 5-cell Sequence containing the newly
        // placed chip can start between -4 and 0
        // relative to that chip.
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

                // Sequence corners belong to everyone.
                bool belongsToOwner =
                    cell.IsCorner ||
                    cell.OwnerId == ownerId;

                if (!belongsToOwner)
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

            // Don't count a candidate that overlaps
            // an existing Sequence by more than one cell.
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
        * Normally only one new Sequence appears in a
        * particular direction.
        *
        * However, a move can connect two groups and
        * create two legal Sequences sharing exactly
        * one cell.
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
            // Re-check because an earlier accepted sequence
            // may now be in existingSequences.
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
                if (!cell.IsCorner)
                {
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
            }

            registered++;

            Debug.Log(
                $"Player {ownerId} completed a Sequence."
            );
        }

        return registered;
    }


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

            // A new Sequence may share at most
            // one position with an existing Sequence.
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

        foreach (BoardCell cell
                in first)
        {
            if (second.Contains(cell))
            {
                overlap++;
            }
        }

        return overlap;
    }


    public int GetSequenceCount(
        int ownerId)
    {
        List<List<BoardCell>> ownerSequences;

        if (!completedSequences.TryGetValue(
                ownerId,
                out ownerSequences))
        {
            return 0;
        }

        return ownerSequences.Count;
    }

    // private void TestSequenceVisual()
    // {
    //     if (board == null)
    //         return;

    //     // Top-left corner is the free/wild Sequence space.
    //     // Put Player 1 chips on the next four cells.

    //     for (int column = 1; column <= 4; column++)
    //     {
    //         BoardCell cell =
    //             board.GetCell(0, column);

    //         if (cell == null)
    //             continue;

    //         cell.SetOwner(1);

    //         BoardCellView view =
    //             cellViews[0, column];

    //         if (view != null)
    //         {
    //             view.UpdateVisual();
    //         }
    //     }

    //     BoardCell lastCell =
    //         board.GetCell(0, 4);

    //     int created =
    //         RegisterNewSequences(
    //             1,
    //             lastCell
    //         );

    //     Debug.Log(
    //         $"VISUAL TEST: Created {created} Sequence(s)."
    //     );
    // }
}