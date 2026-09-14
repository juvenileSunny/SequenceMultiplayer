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

    // =========================================================
    // CREATE CARD FROM NETWORK CODE
    //
    // Examples:
    // "9C"  -> Nine of Clubs
    // "10D" -> Ten of Diamonds
    // "JH"  -> Jack of Hearts
    // =========================================================

    public static bool TryFromCode(
        string code,
        out Card card)
    {
        card = null;

        if (string.IsNullOrWhiteSpace(code))
            return false;

        code =
            code.Trim()
                .ToUpperInvariant();

        if (code.Length < 2)
            return false;

        // Last character is always the suit.
        char suitCharacter =
            code[code.Length - 1];

        // Everything before the suit is the rank.
        string rankCode =
            code.Substring(
                0,
                code.Length - 1
            );

        Suit suit;

        switch (suitCharacter)
        {
            case 'C':
                suit = Suit.Clubs;
                break;

            case 'D':
                suit = Suit.Diamonds;
                break;

            case 'H':
                suit = Suit.Hearts;
                break;

            case 'S':
                suit = Suit.Spades;
                break;

            default:
                return false;
        }

        Rank rank;

        switch (rankCode)
        {
            case "2":
                rank = Rank.Two;
                break;

            case "3":
                rank = Rank.Three;
                break;

            case "4":
                rank = Rank.Four;
                break;

            case "5":
                rank = Rank.Five;
                break;

            case "6":
                rank = Rank.Six;
                break;

            case "7":
                rank = Rank.Seven;
                break;

            case "8":
                rank = Rank.Eight;
                break;

            case "9":
                rank = Rank.Nine;
                break;

            case "10":
                rank = Rank.Ten;
                break;

            case "J":
                rank = Rank.Jack;
                break;

            case "Q":
                rank = Rank.Queen;
                break;

            case "K":
                rank = Rank.King;
                break;

            case "A":
                rank = Rank.Ace;
                break;

            default:
                return false;
        }

        card =
            new Card(
                suit,
                rank
            );

        return true;
    }
}