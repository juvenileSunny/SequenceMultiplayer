using System.Collections.Generic;

public class Player
{
    // =========================================================
    // IDENTITY
    // =========================================================

    // Permanent identity of this player within the game.
    public int PlayerId { get; }

    // =========================================================
    // GAME SETUP
    // =========================================================

    // Which team this player belongs to.
    // -1 means not assigned yet.
    public int TeamId { get; private set; } = -1;

    // Position in the turn order / virtual table.
    // We will use 1-based seats:
    // Seat 1, Seat 2, Seat 3, etc.
    //
    // -1 means not assigned yet.
    public int SeatIndex { get; private set; } = -1;

    public bool HasTeam => TeamId > 0;

    public bool HasSeat => SeatIndex > 0;

    // =========================================================
    // HAND
    // =========================================================

    public List<Card> Hand { get; } =
        new List<Card>();

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public Player(int playerId)
    {
        PlayerId = playerId;
    }

    // =========================================================
    // TEAM / SEAT ASSIGNMENT
    // =========================================================

    public void AssignTeam(int teamId)
    {
        if (teamId <= 0)
            return;

        TeamId = teamId;
    }

    public void AssignSeat(int seatIndex)
    {
        if (seatIndex <= 0)
            return;

        SeatIndex = seatIndex;
    }

    public void ClearGameSetup()
    {
        TeamId = -1;
        SeatIndex = -1;
    }

    // =========================================================
    // HAND MANAGEMENT
    // =========================================================

    public void AddCard(Card card)
    {
        if (card != null)
        {
            Hand.Add(card);
        }
    }

    public void RemoveCard(Card card)
    {
        if (card != null)
        {
            Hand.Remove(card);
        }
    }

    public void ClearHand()
    {
        Hand.Clear();
    }
}