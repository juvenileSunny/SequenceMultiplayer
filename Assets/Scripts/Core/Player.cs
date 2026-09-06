using System.Collections.Generic;

public class Player
{
    public int PlayerId;
    public List<Card> Hand = new List<Card>();

    public Player(int playerId)
    {
        PlayerId = playerId;
    }

    public void AddCard(Card card)
    {
        if (card != null)
            Hand.Add(card);
    }

    public void RemoveCard(Card card)
    {
        Hand.Remove(card);
    }
}