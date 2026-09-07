using System;

[Serializable]
public class BoardCell
{
    // =========================================================
    // BOARD POSITION
    // =========================================================

    public int Row { get; }
    public int Column { get; }

    // Card printed on this board position.
    public Card Card { get; private set; }

    // Four corner spaces are wild/free Sequence spaces.
    public bool IsCorner { get; }

    // =========================================================
    // TEAM OWNERSHIP
    // =========================================================

    // The TEAM that owns the chip on this cell.
    //
    // -1 = empty
    //  1 = Team 1
    //  2 = Team 2
    //  3 = Team 3
    //
    // The board does NOT store which individual player
    // placed the chip.
    public int OwnerTeamId { get; private set; } = -1;

    public bool IsOccupied =>
        OwnerTeamId != -1;

    // =========================================================
    // COMPLETED SEQUENCE
    // =========================================================

    // Once a chip becomes part of a completed Sequence,
    // it is protected from one-eyed Jacks.
    public bool IsPartOfCompletedSequence
    {
        get;
        private set;
    } = false;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public BoardCell(
        int row,
        int column,
        bool isCorner)
    {
        Row = row;
        Column = column;
        IsCorner = isCorner;
    }

    // =========================================================
    // CARD
    // =========================================================

    public void SetCard(
        Card card)
    {
        Card = card;
    }

    // =========================================================
    // TEAM OWNERSHIP
    // =========================================================

    public void SetOwnerTeam(
        int teamId)
    {
        if (IsCorner)
            return;

        if (teamId <= 0)
            return;

        OwnerTeamId = teamId;
    }

    public void ClearOwner()
    {
        OwnerTeamId = -1;

        // Normally a completed Sequence chip cannot be
        // removed by a Jack, but clearing this here keeps
        // the cell state safe if it is reset/reused later.
        IsPartOfCompletedSequence = false;
    }

    // =========================================================
    // SEQUENCE PROTECTION
    // =========================================================

    public void MarkAsCompletedSequence()
    {
        if (IsCorner)
            return;

        if (!IsOccupied)
            return;

        IsPartOfCompletedSequence = true;
    }

    public void ClearSequenceProtection()
    {
        IsPartOfCompletedSequence = false;
    }

    // =========================================================
    // FULL RESET
    // =========================================================

    // Useful later for Restart Game / New Game.
    // The printed Card remains unchanged because the
    // Sequence board layout itself does not change.
    public void ResetGameplayState()
    {
        OwnerTeamId = -1;
        IsPartOfCompletedSequence = false;
    }
}