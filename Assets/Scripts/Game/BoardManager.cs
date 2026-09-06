using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board UI")]
    [SerializeField] private Transform boardContainer;

    [SerializeField]
    private BoardCellView boardCellPrefab;

    private Board board;

    // Temporary local multiplayer testing
    private int currentPlayerId = 1;

    public Board Board => board;

    private void Start()
    {
        CreateBoard();
    }

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
            }
        }
    }

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

        SwitchPlayer();
    }

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
    
    private void ClearExistingBoard()
    {
        foreach (Transform child in boardContainer)
        {
            Destroy(child.gameObject);
        }
    }
}