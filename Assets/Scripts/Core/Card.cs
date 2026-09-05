using System;

public enum Suit
{
    Clubs,
    Diamonds,
    Hearts,
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

    public bool IsJack()
    {
        return Rank == Rank.Jack;
    }

    public bool IsOneEyedJack()
    {
        return Rank == Rank.Jack &&
               (Suit == Suit.Hearts ||
                Suit == Suit.Spades);
    }

    public bool IsTwoEyedJack()
    {
        return Rank == Rank.Jack &&
               (Suit == Suit.Clubs ||
                Suit == Suit.Diamonds);
    }

    public string GetCode()
    {
        return GetRankCode() + GetSuitCode();
    }

    private string GetRankCode()
    {
        switch (Rank)
        {
            case Rank.Two:   return "2";
            case Rank.Three: return "3";
            case Rank.Four:  return "4";
            case Rank.Five:  return "5";
            case Rank.Six:   return "6";
            case Rank.Seven: return "7";
            case Rank.Eight: return "8";
            case Rank.Nine:  return "9";
            case Rank.Ten:   return "10";
            case Rank.Jack:  return "J";
            case Rank.Queen: return "Q";
            case Rank.King:  return "K";
            case Rank.Ace:   return "A";

            default:
                return "";
        }
    }

    private string GetSuitCode()
    {
        switch (Suit)
        {
            case Suit.Clubs:    return "C";
            case Suit.Diamonds: return "D";
            case Suit.Hearts:   return "H";
            case Suit.Spades:   return "S";

            default:
                return "";
        }
    }

    public override string ToString()
    {
        return GetCode();
    }
}