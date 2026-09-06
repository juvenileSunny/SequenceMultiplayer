using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board UI")]
    [SerializeField] private Transform boardContainer;
    [SerializeField] private BoardCellView boardCellPrefab;

    [Header("Features")]
    [SerializeField] private bool showLegalMoveHighlights = true;

    private Board board;

    // Stores the visual object corresponding to every board position.
    private BoardCellView[,] cellViews =
        new BoardCellView[Board.Rows, Board.Columns];

    // Temporary local multiplayer testing
    private int currentPlayerId = 1;

    public Board Board => board;

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

        Debug.Log(
            $"Board created. Player {currentPlayerId}'s turn."
        );
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

                // IMPORTANT:
                // Store the visual so we can highlight it later.
                cellViews[row, column] = cellView;
            }
        }
    }

    // =========================================================
    // CELL CLICK
    // =========================================================

    private void OnCellClicked(
        BoardCellView cellView)
    {
        BoardCell cell =
            cellView.Cell;

        if (cell == null)
            return;

        if (cell.IsCorner)
        {
            Debug.Log(
                "Sequence corner cannot receive a chip."
            );

            return;
        }

        if (cell.IsOccupied)
        {
            Debug.Log(
                $"Cell [{cell.Row},{cell.Column}] is already occupied."
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
        BoardCell cell =
            cellView.Cell;

        cell.SetOwner(currentPlayerId);

        Debug.Log(
            $"Player {currentPlayerId} placed chip on " +
            $"{cell.Card.GetCode()} " +
            $"at [{cell.Row},{cell.Column}]"
        );

        cellView.UpdateVisual();

        // Remove any legal-move highlights after placing.
        ClearHighlights();

        SwitchPlayer();
    }

    // =========================================================
    // PLAYER TURN
    // =========================================================

    private void SwitchPlayer()
    {
        currentPlayerId++;

        if (currentPlayerId > 3)
        {
            currentPlayerId = 1;
        }

        Debug.Log(
            $"Player {currentPlayerId}'s turn."
        );
    }

    // =========================================================
    // LEGAL MOVE HIGHLIGHTING
    // =========================================================

    public void HighlightMatchingCard(
        Card selectedCard)
    {
        // Remove old highlights first.
        ClearHighlights();

        if (!showLegalMoveHighlights)
            return;

        if (selectedCard == null)
            return;

        // Jack behavior will be implemented later.
        if (selectedCard.IsJack())
        {
            Debug.Log(
                $"Jack selected: {selectedCard.GetCode()}. " +
                "Special Jack highlighting will be added later."
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

                // Sequence corners aren't playable.
                if (cell.IsCorner)
                    continue;

                // Already-owned positions are not legal.
                if (cell.IsOccupied)
                    continue;

                if (cell.Card == null)
                    continue;

                // Does this board card match the selected hand card?
                bool matches =
                    cell.Card.Rank == selectedCard.Rank &&
                    cell.Card.Suit == selectedCard.Suit;

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
}