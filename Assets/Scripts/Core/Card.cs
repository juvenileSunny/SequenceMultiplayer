using System;


public enum Suit
{
    Hearts,
    Diamonds,
    Clubs,
    Spades
}

public enum Rank
{
    Two = 2,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King,
    Ace
}


[Serializable]
public class Card
{
    public Suit Suit;
    public Rank Rank;

    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }
    
    public bool isJack()
    {
        return Rank == Rank.Jack;
    }

    public bool IsOneEyedJack()
    {
        return (Rank == Rank.Jack && (Suit == Suit.Hearts || Suit == Suit.Spades));
    }

    public bool IsTwoEyedJack()
    {
        return (Rank == Rank.Jack && (Suit == Suit.Diamonds || Suit == Suit.Clubs));
    }

    public override string ToString()
    {
        return $"{Rank} of {Suit}";
    }
}