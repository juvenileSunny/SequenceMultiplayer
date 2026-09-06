using System;
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

    // Card currently selected from the player's hand
    private Card selectedCard;

    // GameManager tells BoardManager whose turn it is
    private int currentPlayerId = 1;

    public Board Board => board;

    // Fired after a valid card has been played.
    public event Action<Card> OnMoveCompleted;

    private void Start()
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

                cellViews[row, column] = cellView;
            }
        }
    }

    // =========================================================
    // CURRENT PLAYER
    // =========================================================

    public void SetCurrentPlayer(int playerId)
    {
        currentPlayerId = playerId;

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
        BoardCell cell = cellView.Cell;

        if (cell == null)
            return;

        // Corners cannot receive chips.
        if (cell.IsCorner)
        {
            Debug.Log(
                "Sequence corner cannot receive a chip."
            );

            return;
        }

        // Already occupied.
        if (cell.IsOccupied)
        {
            Debug.Log(
                $"Cell [{cell.Row},{cell.Column}] is already occupied."
            );

            return;
        }

        // Player must select a hand card first.
        if (selectedCard == null)
        {
            Debug.Log(
                "Select a card from your hand first."
            );

            return;
        }

        // Jack rules come later.
        if (selectedCard.IsJack())
        {
            Debug.Log(
                "Jack rules have not been implemented yet."
            );

            return;
        }

        if (cell.Card == null)
            return;

        // The clicked board card must match
        // the selected hand card.
        bool matches =
            cell.Card.Rank == selectedCard.Rank &&
            cell.Card.Suit == selectedCard.Suit;

        if (!matches)
        {
            Debug.Log(
                $"Invalid move. Selected card is " +
                $"{selectedCard.GetCode()}, but clicked " +
                $"{cell.Card.GetCode()}."
            );

            return;
        }

        PlaceChip(cellView);
    }

    // =========================================================
    // CHIP PLACEMENT
    // =========================================================

    private void PlaceChip(
        BoardCellView cellView)
    {
        BoardCell cell = cellView.Cell;

        Card playedCard = selectedCard;

        cell.SetOwner(currentPlayerId);

        cellView.UpdateVisual();

        Debug.Log(
            $"Player {currentPlayerId} played " +
            $"{playedCard.GetCode()} at " +
            $"[{cell.Row},{cell.Column}]."
        );

        // Clear the card selection on the board.
        selectedCard = null;

        ClearHighlightVisuals();

        // GameManager will remove the card,
        // draw another one, and change turns.
        OnMoveCompleted?.Invoke(playedCard);
    }

    // =========================================================
    // LEGAL MOVE HIGHLIGHTING
    // =========================================================

    public void HighlightMatchingCard(
        Card card)
    {
        ClearHighlightVisuals();

        selectedCard = card;

        if (!showLegalMoveHighlights)
            return;

        if (card == null)
            return;

        // Jack rules will come later.
        if (card.IsJack())
        {
            Debug.Log(
                $"Jack selected: {card.GetCode()}. " +
                "Jack move logic will be added later."
            );

            return;
        }

        for (int row = 0; row < Board.Rows; row++)
        {
            for (int column = 0;
                 column < Board.Columns;
                 column++)
            {
                BoardCell cell =
                    board.GetCell(row, column);

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

                if (matches)
                {
                    BoardCellView view =
                        cellViews[row, column];

                    if (view != null)
                    {
                        view.SetHighlighted(true);
                    }
                }
            }
        }
    }

    public void ClearHighlights()
    {
        selectedCard = null;

        ClearHighlightVisuals();
    }

    private void ClearHighlightVisuals()
    {
        for (int row = 0; row < Board.Rows; row++)
        {
            for (int column = 0;
                 column < Board.Columns;
                 column++)
            {
                BoardCellView view =
                    cellViews[row, column];

                if (view != null)
                {
                    view.SetHighlighted(false);
                }
            }
        }
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void ClearExistingBoard()
    {
        for (int row = 0; row < Board.Rows; row++)
        {
            for (int column = 0;
                 column < Board.Columns;
                 column++)
            {
                cellViews[row, column] = null;
            }
        }

        foreach (Transform child in boardContainer)
        {
            Destroy(child.gameObject);
        }
    }


    // =========================================================
    // DEAD CARD CHECK
    // =========================================================

    public bool IsDeadCard(Card card)
    {
        if (card == null)
            return false;

        // Jacks are special cards, not dead cards.
        if (card.IsJack())
            return false;

        bool foundMatchingCard = false;

        for (int row = 0; row < Board.Rows; row++)
        {
            for (int column = 0;
                column < Board.Columns;
                column++)
            {
                BoardCell cell =
                    board.GetCell(row, column);

                if (cell == null ||
                    cell.IsCorner ||
                    cell.Card == null)
                {
                    continue;
                }

                bool matches =
                    cell.Card.Rank == card.Rank &&
                    cell.Card.Suit == card.Suit;

                if (!matches)
                    continue;

                foundMatchingCard = true;

                // At least one playable copy still exists.
                if (!cell.IsOccupied)
                {
                    return false;
                }
            }
        }

        return foundMatchingCard;
    }
}