using System;
using System.Collections.Generic;


public class Deck
{
    private readonly List<Card> cards = new List<Card>();
    private readonly Random random = new Random();
    public int Count => cards.Count;

    public Deck()
    {
        CreateDeck();
    }

    private void CreateDeck()
    {
        cards.Clear();
         // Sequence uses two normal decks
        for (int deckNumber = 0; deckNumber < 2; deckNumber++)
        {
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    cards.Add(new Card(suit, rank));
                }
            }
        }
    }

    public void Shuffle()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            Card temp = cards[i];
            cards[i] = cards[j];
            cards[j] = temp;
        }
    }
    public Card Draw()
    {
        if (cards.Count == 0)
            return null;

        Card card = cards[0];
        cards.RemoveAt(0);

        return card;
    }


}
