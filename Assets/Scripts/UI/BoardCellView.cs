using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoardCellView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image cardImage;
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

        if (coordinateText != null)
        {
            coordinateText.text = $"{boardCell.Row},{boardCell.Column}";
        }

        // CORNER CELL
        if (boardCell.IsCorner)
        {
            Sprite cornerSprite =
                Resources.Load<Sprite>("Cards/SEQUENCE");

            if (cornerSprite != null && cardImage != null)
            {
                cardImage.gameObject.SetActive(true);
                cardImage.sprite = cornerSprite;
                cardImage.preserveAspect = true;
            }
            else
            {
                if (cardImage != null)
                    cardImage.gameObject.SetActive(false);

                if (cardText != null)
                {
                    cardText.gameObject.SetActive(true);
                    cardText.text = "Sequence";
                }
            }

            if (cardText != null && cornerSprite != null)
            {
                cardText.gameObject.SetActive(false);
            }

            return;
        }

        // NORMAL CARD CELL
        if (cardText != null)
        {
            cardText.gameObject.SetActive(false);
        }

        if (cardImage == null || boardCell.Card == null)
            return;

        string cardCode = boardCell.Card.GetCode();

        Sprite cardSprite =
            Resources.Load<Sprite>($"Cards/{cardCode}");

        if (cardSprite == null)
        {
            Debug.LogWarning($"Could not find card image: Cards/{cardCode}");

            if (cardText != null)
            {
                cardText.gameObject.SetActive(true);
                cardText.text = cardCode;
            }

            cardImage.gameObject.SetActive(false);
            return;
        }

        cardImage.gameObject.SetActive(true);
        cardImage.sprite = cardSprite;
        cardImage.preserveAspect = true;
    }
}