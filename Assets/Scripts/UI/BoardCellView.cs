using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoardCellView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text cardText;
    [SerializeField] private TMP_Text coordinateText;

    private BoardCell boardCell;

    public BoardCell Cell => boardCell;

    public void Initialize(BoardCell cell)
    {
        boardCell = cell;

        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (boardCell == null)
            return;

        coordinateText.text =
            $"{boardCell.Row},{boardCell.Column}";

        if (boardCell.IsCorner)
        {
            cardText.text = "Sequence";
            return;
        }

        if (boardCell.Card != null)
        {
            cardText.text = GetShortCardName(boardCell.Card);
        }
        else
        {
            // Temporary placeholder
            cardText.text = "-";
        }
    }

    private string GetShortCardName(Card card)
    {
        return $"{GetRankString(card.Rank)}{GetSuitSymbol(card.Suit)}";
    }

    private string GetRankString(Rank rank)
    {
        switch (rank)
        {
            case Rank.Ace:
                return "A";

            case Rank.King:
                return "K";

            case Rank.Queen:
                return "Q";

            case Rank.Jack:
                return "J";

            case Rank.Ten:
                return "10";

            default:
                return ((int)rank).ToString();
        }
    }

    private string GetSuitSymbol(Suit suit)
    {
        switch (suit)
        {
            case Suit.Hearts:
                return "♥";

            case Suit.Diamonds:
                return "♦";

            case Suit.Clubs:
                return "♣";

            case Suit.Spades:
                return "♠";

            default:
                return "";
        }
    }
}