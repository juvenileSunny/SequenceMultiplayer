using System;

[Serializable]
public class BoardCell
{
    public int Row { get; }
    public int Column { get; }

    public Card Card { get; private set; }

    public bool IsCorner { get; }

    public int OwnerId { get; private set; } = -1;

    // Once this cell belongs to a completed Sequence,
    // a one-eyed Jack cannot remove it.
    public bool IsPartOfCompletedSequence
    {
        get;
        private set;
    } = false;

    public bool IsOccupied =>
        OwnerId != -1;

    public BoardCell(
        int row,
        int column,
        bool isCorner)
    {
        Row = row;
        Column = column;
        IsCorner = isCorner;
    }

    public void SetCard(Card card)
    {
        Card = card;
    }

    public void SetOwner(int ownerId)
    {
        OwnerId = ownerId;
    }

    public void ClearOwner()
    {
        OwnerId = -1;
    }

    public void MarkAsCompletedSequence()
    {
        IsPartOfCompletedSequence = true;
    }
}