using System;

[Serializable]
public class BoardCell
{
    public int Row;
    public int Column;

    public Card Card;

    public bool IsCorner;

    // -1 means the cell is not occupied by any player/team.
    public int OwnerId = -1;

    public bool IsOccupied => OwnerId != -1;

    public BoardCell(int row, int column, bool isCorner = false)
    {
        Row = row;
        Column = column;
        IsCorner = isCorner;
        OwnerId = -1;
        Card = null;
    }

    public void SetCard(Card card)
    {
        Card = card;
    }

    public void SetOwner(int playerId)
    {
        OwnerId = playerId;
    }

    public void ClearOwner()
    {
        OwnerId = -1;
    }
}