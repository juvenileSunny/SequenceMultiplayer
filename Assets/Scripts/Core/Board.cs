using System;

[Serializable]
public class Board
{
    public const int Rows = 10;
    public const int Columns = 10;

    private readonly BoardCell[,] cells;

    // Fixed Sequence board layout.
    // S = Spades
    // H = Hearts
    // D = Diamonds
    // C = Clubs
    //
    // Example:
    // 10S = Ten of Spades
    // QH  = Queen of Hearts
    // AD  = Ace of Diamonds

    private readonly string[,] layout = 
    { 
        // Row 0 
        { 
            "SEQUENCE", "2S", "3S", "4S", "5S", 
            "6S", "7S", "8S", "9S", "SEQUENCE" 
        }, 
    
        // Row 1 
        { 
            "6C", "5C", "4C", "3C", "2C", 
            "AH", "KH", "QH", "10H", "10S" 
        }, 
    
        // Row 2 
        { 
            "7C", "AS", "2D", "3D", "4D", 
            "5D", "6D", "7D", "9H", "QS" 
        }, 
    
        // Row 3 
        { 
            "8C", "KS", "6C", "5C", "4C", 
            "3C", "2C", "8D", "8H", "KS" 
        }, 
    
        // Row 4 
        { 
            "9C", "QS", "7C", "6H", "5H", 
            "4H", "AH", "9D", "7H", "AS" 
        }, 
    
        // Row 5 
        { 
            "10C", "10S", "8C", "7H", "2H", 
            "3H", "KH", "10D", "6H", "2D" 
        }, 
    
        // Row 6 
        { 
            "QC", "9S", "9C", "8H", "9H", 
            "10H", "QH", "QD", "5H", "3D" 
        }, 
    
        // Row 7 
        { 
            "KC", "8S", "10C", "QC", "KC", 
            "AC", "AD", "KD", "4H", "4D" 
        }, 
    
        // Row 8 
        { 
            "AC", "7S", "6S", "5S", "4S", 
            "3S", "2S", "2H", "3H", "5D" 
        }, 
    
        // Row 9 
        { 
            "SEQUENCE", "AD", "KD", "QD", "10D", 
            "9D", "8D", "7D", "6D", "SEQUENCE" 
        } 
    };
    public Board()
    {
        cells = new BoardCell[Rows, Columns];

        CreateBoard();
    }

    private void CreateBoard()
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                string code = layout[row, column];

                bool isCorner = code == "SEQUENCE";

                BoardCell cell =
                    new BoardCell(row, column, isCorner);

                if (!isCorner)
                {
                    Card card = ParseCard(code);

                    cell.SetCard(card);
                }

                cells[row, column] = cell;
            }
        }
    }

    private Card ParseCard(string code)
    {
        // Last character is the suit:
        // 10S -> S
        // QH  -> H

        char suitCharacter =
            code[code.Length - 1];

        // Everything before the last character is the rank:
        // 10S -> 10
        // QH  -> Q

        string rankString =
            code.Substring(0, code.Length - 1);

        Rank rank = ParseRank(rankString);
        Suit suit = ParseSuit(suitCharacter);

        return new Card(suit, rank);
    }

    private Rank ParseRank(string value)
    {
        switch (value)
        {
            case "2":
                return Rank.Two;

            case "3":
                return Rank.Three;

            case "4":
                return Rank.Four;

            case "5":
                return Rank.Five;

            case "6":
                return Rank.Six;

            case "7":
                return Rank.Seven;

            case "8":
                return Rank.Eight;

            case "9":
                return Rank.Nine;

            case "10":
                return Rank.Ten;

            case "Q":
                return Rank.Queen;

            case "K":
                return Rank.King;

            case "A":
                return Rank.Ace;

            default:
                throw new Exception(
                    $"Unknown card rank: {value}"
                );
        }
    }

    private Suit ParseSuit(char value)
    {
        switch (value)
        {
            case 'C':
                return Suit.Clubs;

            case 'D':
                return Suit.Diamonds;

            case 'H':
                return Suit.Hearts;

            case 'S':
                return Suit.Spades;

            default:
                throw new Exception(
                    $"Unknown card suit: {value}"
                );
        }
    }

    public BoardCell GetCell(int row, int column)
    {
        if (!IsValidPosition(row, column))
            return null;

        return cells[row, column];
    }

    public bool IsValidPosition(int row, int column)
    {
        return
            row >= 0 &&
            row < Rows &&
            column >= 0 &&
            column < Columns;
    }

    public bool IsCellAvailable(int row, int column)
    {
        BoardCell cell =
            GetCell(row, column);

        if (cell == null)
            return false;

        if (cell.IsCorner)
            return false;

        return !cell.IsOccupied;
    }
}